using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : NetworkRoomManager
{
    public static RoomManager Instance { get; private set; }
    int roomOwnerConnectionId = -1;
    uint roomOwnerNetId;

    // 这组字段就是“房间当前对外状态”。
    // 后面大厅列表、房间界面、广播发现都可以从这里取值。
    [Header("房间信息")]
    public string RoomId = "000000";
    string roomPassword;
    public string RoomName = "默认房间";

    public string MapName = "Map01";

    public int MaxPlayersCount = 4;

    public bool IsPlaying;

    public double GameEndTime;
    public bool HasPassword => !string.IsNullOrWhiteSpace(roomPassword);

    [Header("游戏流程")]
    [SerializeField]
    [Min(1f)]
    float gameDurationSeconds = 30f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("进入游戏场景后，在此秒数之前不根据「无 GamePlayer」提前结束；否则切场景瞬间尚未 ReplacePlayer，计数为 0 会立刻 ServerChangeScene 拉回房间。")]
    float earlyGameplayEmptyCheckDelaySeconds = 5f;
    [SerializeField] string multiplayerLobbySceneName = "MultiplayerLobby";
    
    [SerializeField] Transform[] gameplaySpawnPoints;
    [SerializeField] int maxChatLength = 120;

    Coroutine gameTimerCoroutine;
    IChatTextSanitizer chatTextSanitizer;
    IChatChannel roomChatChannel;
    IChatChannel gameChatChannel;
    string currentGameplayScene;

    public override void Awake()
    {
        base.Awake();
        // 预制体里常填 Assets/.../Game.unity 全路径；LoadSceneAsync 用短名更稳，且与 OnRoomServerSceneChanged 比较一致。
        RoomScene = NormalizeSceneIdentifier(RoomScene);
        GameplayScene = NormalizeSceneIdentifier(GameplayScene);
        currentGameplayScene = GameplayScene;
        Instance = this;
        // 使用自定义 RoomUI，关闭 Mirror 自带 OnGUI 房间界面，避免与项目 UI 重叠。
        showRoomGUI = false;
        chatTextSanitizer = new ChatTextSanitizer(maxChatLength);
        roomChatChannel = new RoomChatChannel(this, chatTextSanitizer);
        gameChatChannel = new GameChatChannel(chatTextSanitizer);
    }

    public override void OnStartServer()
    {
        // StopHost 后 OnStopServer 会清空 Instance；Awake 不会再次执行，第二次 StartHost 必须在此重新登记单例。
        Instance = this;
        maxConnections = MaxPlayersCount;
        base.OnStartServer();
    }

    public override void OnStopServer()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
            gameTimerCoroutine = null;
        }

        GameEndTime = 0d;
        roomOwnerConnectionId = -1;
        roomOwnerNetId = 0;
        roomPassword = null;

        base.OnStopServer();
    }

    /// <summary>
    /// 默认 NetworkRoomManager 在「非 RoomScene」时会断开所有连接；主机在大厅 StartHost 时本地连接也会被断开，
    /// 进而导致进入房间后 AddPlayer 重复、出现 “There is already a player for this connection”。
    /// 这里允许主机本地连接在大厅阶段保持存活，远程连接仍必须在房间场景内才能加入。
    /// </summary>
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (!Utils.IsSceneActive(RoomScene))
        {
            if (conn != NetworkServer.localConnection)
            {
                Debug.Log($"[RoomManager] 当前不是房间场景，拒绝远程连接：{conn}");
                conn.Disconnect();
                return;
            }

            OnRoomServerConnect(conn);
            return;
        }

        // 房间内按当前最大人数硬限制远程连接，防止房主下调人数后仍可无限加入。
        if (conn != NetworkServer.localConnection && GetPlayers().Count >= Mathf.Max(1, MaxPlayersCount))
        {
            Debug.Log($"[RoomManager] 房间已满({GetPlayers().Count}/{MaxPlayersCount})，拒绝连接：{conn}");
            conn.Disconnect();
            return;
        }

        base.OnServerConnect(conn);
    }

    /// <summary>
    /// 主机在 MultiplayerLobby 等场景 StartHost 时，先不要生成 RoomPlayer；等 ServerChangeScene 进入 Room 后，
    /// 由客户端 OnClientSceneChanged 再次 AddPlayer。远程在非房间场景一律拒绝。
    /// </summary>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (!Utils.IsSceneActive(RoomScene))
        {
            if (conn == NetworkServer.localConnection)
            {
                return;
            }

            Debug.Log($"[RoomManager] 当前不是房间场景，断开远程连接：{conn}");
            conn.Disconnect();
            return;
        }

        base.OnServerAddPlayer(conn);
        ApplyRoomPlayerJoinedServerRules(conn);
    }

    /// <summary>
    /// 非 RoomScene 时不要调用 NetworkManager.OnClientConnect 里的 AddPlayer，否则进入房间后
    /// OnClientSceneChanged 会再发一次 AddPlayer，触发 “There is already a player for this connection”。
    /// </summary>
    public override void OnClientConnect()
    {
        if (Utils.IsSceneActive(RoomScene))
        {
            base.OnClientConnect();
            return;
        }

        OnRoomClientConnect();
        if (!NetworkClient.ready)
        {
            NetworkClient.Ready();
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        string lobbyScene = NormalizeSceneIdentifier(multiplayerLobbySceneName);
        if (string.IsNullOrWhiteSpace(lobbyScene))
        {
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;
        bool inRoomOrGameplay = currentScene == RoomScene || currentScene == GameplayScene;
        if (!inRoomOrGameplay || currentScene == lobbyScene)
        {
            return;
        }

        SceneManager.LoadScene(lobbyScene);
    }

    /// <summary>
    /// Mirror 96 中 NetworkRoomManager 不会在 OnServerAddPlayer 里调用 OnRoomServerAddPlayer，
    /// 房主与房间状态逻辑必须挂在 OnServerAddPlayer 成功添加 RoomPlayer 之后。
    /// </summary>
    [Server]
    void ApplyRoomPlayerJoinedServerRules(NetworkConnectionToClient conn)
    {
        RoomPlayer player = conn.identity != null ? conn.identity.GetComponent<RoomPlayer>() : null;
        if (player == null)
        {
            return;
        }

        // 场景切换或重连后，旧的 roomOwnerNetId 可能仍保留，但对应 NetworkIdentity 已不存在。
        // 若不清理，会导致“无人是房主”或误把非创建者当成房主。
        SanitizeStaleRoomOwnerState();

        // 规则：
        // 1) 已登记创建者时，创建者连接入房后必须成为房主
        // 2) 未登记创建者时，第一个入房玩家成为房主并登记为创建者
        if (roomOwnerConnectionId >= 0 && conn.connectionId == roomOwnerConnectionId)
        {
            PromoteToLeader(player);
        }
        else if (!HasValidRoomOwner())
        {
            roomOwnerConnectionId = conn.connectionId;
            PromoteToLeader(player);
        }
        else
        {
            ApplyOwnerStateToPlayer(player);
        }

        // 若房主 netId 在 AddPlayer 当帧才生成，补上 RoomManager 侧的 roomOwnerNetId，便于发现广播与 IsRoomOwner(netId) 路径。
        if (roomOwnerNetId == 0 && player.netId != 0 &&
            player.connectionToClient != null &&
            roomOwnerConnectionId >= 0 &&
            player.connectionToClient.connectionId == roomOwnerConnectionId)
        {
            roomOwnerNetId = player.netId;
            UpdateAllPlayersOwnerState();
        }

        // 有新玩家进入后，把房间状态往外抛，UI 后面直接监听即可。
        RoomEvents.RaisePlayerListChanged();
        RoomEvents.RaiseRoomInfoChanged();
        ServerBroadcastRoomInfoSnapshot();
    }

    public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
    {
        bool leaderLeft = false;

        if (conn.identity != null && conn.identity.TryGetComponent(out RoomPlayer roomPlayer))
        {
            leaderLeft = roomPlayer.netId == roomOwnerNetId;
        }

        base.OnRoomServerDisconnect(conn);

        if (leaderLeft)
        {
            AssignNextLeader();
        }

        RoomEvents.RaisePlayerListChanged();
        RoomEvents.RaiseRoomInfoChanged();
    }

    public override void ReadyStatusChanged()
    {
        base.ReadyStatusChanged();
        RoomEvents.RaisePlayerListChanged();
    }

    public override void OnRoomServerPlayersReady()
    {
        // 这里故意不自动开局。
        // 我们保留“全员已准备”的状态，但由房主手动点击开始。
        RoomEvents.RaisePlayerListChanged();
    }

    public override void OnRoomServerPlayersNotReady()
    {
        RoomEvents.RaisePlayerListChanged();
    }

    public override void OnRoomServerSceneChanged(string sceneName)
    {
        bool playing = !string.IsNullOrWhiteSpace(currentGameplayScene) && sceneName == currentGameplayScene;
        IsPlaying = playing;

        // 进入游戏场景后启动最小计时器，时间到自动回房间。
        if (playing)
        {
            RestartGameTimer();
        }
        else
        {
            StopGameTimer();
            EnsureLeaderExists();
            currentGameplayScene = ResolveGameplaySceneForCurrentMap();
        }

        RoomEvents.RaiseRoomStateChanged(IsPlaying);
        ServerBroadcastRoomInfoSnapshot();
    }

    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayerObject, GameObject gamePlayerObject)
    {
        if (roomPlayerObject.TryGetComponent(out RoomPlayer roomPlayer) &&
            gamePlayerObject.TryGetComponent(out GamePlayer gamePlayer))
        {
            gamePlayer.ApplyRoomPlayer(roomPlayer);
        }

        return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayerObject, gamePlayerObject);
    }

    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
    {
        Transform spawnPoint = GetRandomGameplaySpawnPoint();
        if (spawnPoint != null && playerPrefab != null)
        {
            return Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        }

        return base.OnRoomServerCreateGamePlayer(conn, roomPlayer);
    }

    [Server]
    public void ServerUpdateRoomInfo(string roomName, string mapName, int maxPlayers)
    {
        int currentPlayers = GetPlayers().Count;
        int requestedMaxPlayers = Mathf.Max(1, maxPlayers);
        if (requestedMaxPlayers < currentPlayers)
        {
            Debug.LogWarning($"[RoomManager] 修改最大人数无效：当前房间人数为 {currentPlayers}，不能下调到 {requestedMaxPlayers}。");
            requestedMaxPlayers = MaxPlayersCount;
        }

        RoomName = string.IsNullOrWhiteSpace(roomName) ? RoomName : roomName.Trim();
        MapName = GameMapCatalog.Normalize(string.IsNullOrWhiteSpace(mapName) ? MapName : mapName.Trim());
        MaxPlayersCount = requestedMaxPlayers;
        maxConnections = MaxPlayersCount;

        RoomEvents.RaiseRoomInfoChanged();
        ServerBroadcastRoomInfoSnapshot();
    }

    [Server]
    public void ServerSetupRoomIdentity(string roomId, string password)
    {
        string normalizedId = NormalizeRoomId(roomId);
        if (!string.IsNullOrWhiteSpace(normalizedId))
        {
            RoomId = normalizedId;
        }

        roomPassword = string.IsNullOrWhiteSpace(password) ? null : password.Trim();
        RoomEvents.RaiseRoomInfoChanged();
        ServerBroadcastRoomInfoSnapshot();
    }

    [Server]
    public void ServerBroadcastRoomInfoSnapshot()
    {
        foreach (RoomPlayer player in GetPlayers())
        {
            if (player == null || player.connectionToClient == null)
            {
                continue;
            }

            player.ServerPushRoomInfoTo(player.connectionToClient, RoomName, MapName, MaxPlayersCount, IsPlaying, GameEndTime);
        }
    }

    [Server]
    public void ServerBroadcastRoomInfoSnapshotTo(NetworkConnectionToClient target)
    {
        if (target == null)
        {
            return;
        }

        RoomPlayer player = GetPlayers().FirstOrDefault(p => p != null && p.connectionToClient == target);
        if (player == null)
        {
            return;
        }

        player.ServerPushRoomInfoTo(target, RoomName, MapName, MaxPlayersCount, IsPlaying, GameEndTime);
    }

    [Server]
    public void ServerStartGame()
    {
        if (IsPlaying || !CanStartGame())
        {
            return;
        }

        string targetGameplayScene = ResolveGameplaySceneForCurrentMap();
        if (string.IsNullOrWhiteSpace(targetGameplayScene))
        {
            Debug.LogError("[RoomManager] 未找到可用的游戏场景。请在 MapDataDefinition.gameplaySceneName 或 RoomManager.GameplayScene 中配置。");
            return;
        }

        // 路径或名称写错时 LoadSceneAsync 可能无效或仍落在房间场景，提前校验。
        if (!string.IsNullOrWhiteSpace(RoomScene) &&
            string.Equals(targetGameplayScene.Trim(), RoomScene.Trim(), StringComparison.Ordinal))
        {
            Debug.LogError($"[RoomManager] 目标游戏场景({targetGameplayScene}) 与 RoomScene 相同，无法进入独立游戏场景，请检查地图配置。");
            return;
        }

        // 先标记为游戏中，再切场景，方便外部状态同步尽快更新。
        currentGameplayScene = targetGameplayScene;
        IsPlaying = true;
        RoomEvents.RaiseRoomStateChanged(IsPlaying);
        ServerChangeScene(targetGameplayScene);
    }

    /// <summary>
    /// 与房间界面一致：房主可以不准备；非房主必须准备。旧逻辑要求全员 readyToBegin，会导致房主从未点准备时永远无法开局。
    /// </summary>
    public bool CanStartGame()
    {
        List<RoomPlayer> players = GetPlayers();
        if (players.Count < minPlayers)
        {
            return false;
        }

        foreach (RoomPlayer player in players)
        {
            if (player == null)
            {
                continue;
            }

            if (IsRoomOwner(player))
            {
                continue;
            }

            if (!player.readyToBegin)
            {
                return false;
            }
        }

        return true;
    }

    public List<RoomPlayer> GetPlayers()
    {
        return roomSlots
            .OfType<RoomPlayer>()
            .Where(player => player != null)
            .OrderBy(player => player.index)
            .ToList();
    }

    public RoomData GetCurrentRoomData()
    {
        return new RoomData
        {
            RoomId = RoomId,
            HasPassword = HasPassword,
            RoomPassword = roomPassword,
            OwnerPlayerNetId = roomOwnerNetId,
            RoomName = RoomName,
            MapName = MapName,
            CurrentPlayers = GetPlayers().Count,
            MaxPlayers = MaxPlayersCount,
            IsPlaying = IsPlaying
        };
    }

    public RoomPlayer GetLocalRoomPlayer()
    {
        foreach (RoomPlayer player in GetPlayers())
        {
            if (player != null && player.isLocalPlayer)
            {
                return player;
            }
        }

        return null;
    }

    public bool IsRoomOwner(RoomPlayer player)
    {
        if (player == null)
        {
            return false;
        }

        if (roomOwnerNetId != 0 && player.netId != 0 && player.netId == roomOwnerNetId)
        {
            return true;
        }

        // netId 尚未分配完成时，用创建主机时登记的 connectionId 作为兜底。
        return roomOwnerConnectionId >= 0 &&
               player.connectionToClient != null &&
               player.connectionToClient.connectionId == roomOwnerConnectionId;
    }

    [Server]
    public bool IsRoomOwnerConnection(NetworkConnectionToClient conn)
    {
        if (conn == null)
        {
            return false;
        }

        if (roomOwnerConnectionId >= 0 && conn.connectionId == roomOwnerConnectionId)
        {
            return true;
        }

        if (roomOwnerNetId != 0 && conn.identity != null && conn.identity.netId == roomOwnerNetId)
        {
            return true;
        }

        return false;
    }

    public double GetRemainingGameSeconds()
    {
        if (!IsPlaying || GameEndTime <= 0d)
        {
            return 0d;
        }

        return Mathf.Max(0f, (float)(GameEndTime - NetworkTime.time));
    }

    [Server]
    public void ServerBroadcastRoomChat(string senderName, string text)
    {
        roomChatChannel?.Broadcast(senderName, text);
    }

    [Server]
    public void ServerBroadcastGameChat(string senderName, string text)
    {
        gameChatChannel?.Broadcast(senderName, text);
    }

    [Server]
    void AssignNextLeader()
    {
        List<RoomPlayer> players = GetPlayers();
        if (players.Count == 0)
        {
            return;
        }

        foreach (RoomPlayer player in players)
        {
            player.IsLeader = false;
        }

        roomOwnerConnectionId = players[0].connectionToClient != null ? players[0].connectionToClient.connectionId : -1;
        PromoteToLeader(players[0]);
    }

    [Server]
    void EnsureLeaderExists()
    {
        SanitizeStaleRoomOwnerState();

        List<RoomPlayer> players = GetPlayers();
        if (players.Count == 0)
        {
            return;
        }

        if (HasValidRoomOwner())
        {
            RoomPlayer leader = players.FirstOrDefault(player => player != null && player.netId == roomOwnerNetId);
            if (leader != null)
            {
                roomOwnerNetId = leader.netId;
                roomOwnerConnectionId = leader.connectionToClient != null ? leader.connectionToClient.connectionId : roomOwnerConnectionId;
                UpdateAllPlayersOwnerState();
            }
            return;
        }

        // roomSlots 使用 HashSet，Recalculate 后的 index 最小者不一定是创建房间的主机，不能盲用 players[0]。
        RoomPlayer prefer = null;
        if (roomOwnerConnectionId >= 0)
        {
            prefer = players.FirstOrDefault(p =>
                p != null && p.connectionToClient != null && p.connectionToClient.connectionId == roomOwnerConnectionId);
        }

        if (prefer != null)
        {
            PromoteToLeader(prefer);
        }
        else
        {
            PromoteToLeader(players[0]);
        }
    }

    /// <summary>
    /// 当记录的房主 NetId 已无任何 RoomPlayer 对应时，清除该记录，便于后续按连接 ID 重新指定房主。
    /// </summary>
    [Server]
    void SanitizeStaleRoomOwnerState()
    {
        if (roomOwnerNetId == 0)
        {
            return;
        }

        if (!GetPlayers().Any(p => p != null && p.netId == roomOwnerNetId))
        {
            roomOwnerNetId = 0;
        }
    }

    [Server]
    public void ServerSetRoomOwnerByConnectionId(int connectionId)
    {
        roomOwnerConnectionId = connectionId;

        RoomPlayer ownerPlayer = GetPlayers()
            .FirstOrDefault(player => player != null &&
                                      player.connectionToClient != null &&
                                      player.connectionToClient.connectionId == connectionId);
        if (ownerPlayer != null)
        {
            PromoteToLeader(ownerPlayer);
        }
    }

    [Server]
    void PromoteToLeader(RoomPlayer targetLeader)
    {
        if (targetLeader == null)
        {
            return;
        }

        roomOwnerConnectionId = targetLeader.connectionToClient != null ? targetLeader.connectionToClient.connectionId : roomOwnerConnectionId;
        roomOwnerNetId = targetLeader.netId;

        // 新 RoomPlayer 往往在 AddPlayer 同一帧尚未执行 Start()，还未加入 roomSlots，
        // 仅遍历 GetPlayers() 会漏掉房主，导致 RoomOwnerNetId / IsLeader 未同步到该对象。
        foreach (RoomPlayer player in GetPlayers())
        {
            ApplyOwnerStateToPlayer(player);
        }

        ApplyOwnerStateToPlayer(targetLeader);
    }

    [Server]
    void UpdateAllPlayersOwnerState()
    {
        foreach (RoomPlayer player in GetPlayers())
        {
            ApplyOwnerStateToPlayer(player);
        }
    }

    [Server]
    void ApplyOwnerStateToPlayer(RoomPlayer player)
    {
        if (player == null)
        {
            return;
        }

        player.RoomOwnerNetId = roomOwnerNetId;

        bool ownerByNetId = roomOwnerNetId != 0 && player.netId != 0 && player.netId == roomOwnerNetId;
        bool ownerByConnection = roomOwnerConnectionId >= 0 &&
                                 player.connectionToClient != null &&
                                 player.connectionToClient.connectionId == roomOwnerConnectionId;
        player.IsLeader = ownerByNetId || ownerByConnection;
    }

    [Server]
    bool HasValidRoomOwner()
    {
        if (roomOwnerNetId == 0)
        {
            return false;
        }

        return GetPlayers().Any(player => player != null && player.netId == roomOwnerNetId);
    }

    [Server]
    void RestartGameTimer()
    {
        StopGameTimer();
        GameEndTime = NetworkTime.time + gameDurationSeconds;
        gameTimerCoroutine = StartCoroutine(GameTimerCoroutine());
    }

    [Server]
    void StopGameTimer()
    {
        if (gameTimerCoroutine == null)
        {
            return;
        }

        StopCoroutine(gameTimerCoroutine);
        gameTimerCoroutine = null;
        GameEndTime = 0d;
    }

    [Server]
    System.Collections.IEnumerator GameTimerCoroutine()
    {
        // 游戏结束条件：
        // 1) 达到倒计时
        // 2) 游戏场景中已没有有效玩家（例如全部退出或返回大厅）
        // 注意：刚进 Game 时连接上往往还是 RoomPlayer，ReplacePlayer 完成前 GetActiveGamePlayerCount 会为 0，
        // 必须延迟「空场检测」，否则会误判并立刻切回 Room。
        double emptyCheckNotBefore = NetworkTime.time + Mathf.Max(0f, earlyGameplayEmptyCheckDelaySeconds);

        while (IsPlaying && NetworkTime.time < GameEndTime)
        {
            if (NetworkTime.time >= emptyCheckNotBefore && GetActiveGamePlayerCount() <= 0)
            {
                break;
            }

            yield return new WaitForSeconds(1f);
        }

        gameTimerCoroutine = null;

        if (!string.IsNullOrWhiteSpace(RoomScene) && NetworkServer.active)
        {
            ServerChangeScene(RoomScene);
        }
    }

    static string NormalizeSceneIdentifier(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene))
        {
            return scene;
        }

        string trimmed = scene.Trim().Replace('\\', '/');
        if (trimmed.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFileNameWithoutExtension(trimmed);
        }

        string fileName = Path.GetFileName(trimmed);
        return string.IsNullOrEmpty(fileName) ? trimmed : Path.GetFileNameWithoutExtension(fileName);
    }

    string ResolveGameplaySceneForCurrentMap()
    {
        MapDataDefinition mapData = MapDataRepository.GetByMapName(MapName);
        string mappedScene = mapData != null ? mapData.gameplaySceneName : null;
        string resolved = NormalizeSceneIdentifier(mappedScene);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return resolved;
        }

        return NormalizeSceneIdentifier(GameplayScene);
    }

    static string NormalizeRoomId(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return null;
        }

        return roomId.Trim();
    }

    Transform GetRandomGameplaySpawnPoint()
    {
        if (gameplaySpawnPoints != null && gameplaySpawnPoints.Length > 0)
        {
            List<Transform> validPoints = gameplaySpawnPoints.Where(point => point != null).ToList();
            if (validPoints.Count > 0)
            {
                return validPoints[UnityEngine.Random.Range(0, validPoints.Count)];
            }
        }

        return GetStartPosition();
    }

    int GetActiveGamePlayerCount()
    {
        int count = 0;
        foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
        {
            if (connection?.identity != null && connection.identity.GetComponent<GamePlayer>() != null)
            {
                count++;
            }
        }

        return count;
    }

}
