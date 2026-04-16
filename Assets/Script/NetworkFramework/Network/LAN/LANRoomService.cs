using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LANRoomService : MonoBehaviour, IRoomService
{
    [SerializeField] bool autoRefreshOnEnable = true;
    [SerializeField] NetworkDiscovery discovery;
    [SerializeField] float refreshDelaySeconds = 2.1f;
    [SerializeField] float waitingRefreshIntervalSeconds = 2f;
    readonly List<RoomData> cachedRooms = new List<RoomData>();
    IRoomJoinPolicy joinPolicy;
    string waitingRoomId;
    Coroutine hostAssignCoroutine;

    public bool IsRefreshing { get; private set; }
    public bool IsWaitingForRoom => !string.IsNullOrWhiteSpace(waitingRoomId);
    public event Action<IReadOnlyList<RoomData>> OnRoomListUpdated;
    public event Action<RoomJoinResult> OnJoinStatusChanged;

    /// <summary>
    /// 主机建房后，已在服务端完成房主登记与房间信息同步，可安全切换至房间场景。
    /// </summary>
    public event Action OnHostRoomAuthorityReady;

    void OnEnable()
    {
        joinPolicy ??= new RoomJoinPolicy();
        if (autoRefreshOnEnable)
        {
            RefreshRooms();
        }
    }

    void OnDisable()
    {
        CancelInvoke(nameof(RefreshRooms));
    }

    public void RefreshRooms()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        if (discovery != null)
        {
            discovery.RefreshDiscovery();
        }

        Invoke(nameof(FinishRefresh), Mathf.Max(0.5f, refreshDelaySeconds));
    }

    public RoomJoinResult CreateRoom(string roomName, string mapName, int maxPlayers, string roomId = null, string roomPassword = null)
    {
        if (NetworkManager.singleton == null)
        {
            RoomJoinResult missingManager = RoomJoinResult.Fail(RoomJoinResultCode.NetworkManagerMissing, "建房失败：未找到 NetworkManager。");
            OnJoinStatusChanged?.Invoke(missingManager);
            return missingManager;
        }

        if (NetworkServer.active || NetworkClient.isConnected)
        {
            RoomJoinResult alreadyInSession = RoomJoinResult.Fail(RoomJoinResultCode.Unsupported, "建房失败：当前已在联机会话中。");
            OnJoinStatusChanged?.Invoke(alreadyInSession);
            return alreadyInSession;
        }

        string normalizedRoomId = NormalizeRoomId(roomId);
        if (string.IsNullOrWhiteSpace(normalizedRoomId))
        {
            normalizedRoomId = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        if (cachedRooms.Exists(room => room != null && string.Equals(room.RoomId, normalizedRoomId, StringComparison.OrdinalIgnoreCase)))
        {
            RoomJoinResult duplicatedRoomId = RoomJoinResult.Fail(RoomJoinResultCode.Unsupported, "建房失败：房间ID已存在，请更换。");
            OnJoinStatusChanged?.Invoke(duplicatedRoomId);
            return duplicatedRoomId;
        }

        if (hostAssignCoroutine != null)
        {
            StopCoroutine(hostAssignCoroutine);
            hostAssignCoroutine = null;
        }

        NetworkManager.singleton.StartHost();

        // StartHost 是异步的：本地连接与 RoomPlayer 可能在下一帧才就绪。
        // 若立刻 ServerSetRoomOwner，常出现 ownerPlayer==null，导致仅写入了 connectionId 却未 PromoteToLeader。
        hostAssignCoroutine = StartCoroutine(HostAssignOwnerAndRoomInfoWhenReady(roomName, mapName, maxPlayers, normalizedRoomId, roomPassword));

        RoomJoinResult success = RoomJoinResult.Ok("房间创建成功，已开始局域网广播。");
        OnJoinStatusChanged?.Invoke(success);
        return success;
    }

    /// <summary>
    /// 在主机连接与房间玩家生成完成后，再登记创建者为房主并同步房间信息。
    /// </summary>
    IEnumerator HostAssignOwnerAndRoomInfoWhenReady(string roomName, string mapName, int maxPlayers, string roomId, string roomPassword)
    {
        const int maxFrames = 30;
        for (int i = 0; i < maxFrames; i++)
        {
            yield return null;

            // 用户已 StopHost（例如从房间返回大厅），不应再继续登记或误触发进房。
            if (!NetworkServer.active)
            {
                hostAssignCoroutine = null;
                yield break;
            }

            RoomManager roomManager = RoomManager.Instance;
            if (roomManager == null)
            {
                continue;
            }

            if (NetworkServer.localConnection == null)
            {
                continue;
            }

            roomManager.ServerSetRoomOwnerByConnectionId(NetworkServer.localConnection.connectionId);
            roomManager.ServerSetupRoomIdentity(roomId, roomPassword);
            roomManager.ServerUpdateRoomInfo(roomName, mapName, maxPlayers);
            OnHostRoomAuthorityReady?.Invoke();
            hostAssignCoroutine = null;
            yield break;
        }

        Debug.LogWarning("[局域网房间服务] 主机已启动但长时间未拿到本地连接，房主登记可能失败，请检查 Transport 与场景。");
        if (NetworkServer.active)
        {
            OnHostRoomAuthorityReady?.Invoke();
        }

        hostAssignCoroutine = null;
    }

    public RoomJoinResult JoinRoom(string roomId, string roomPassword = null)
    {
        string normalizedRoomId = NormalizeRoomId(roomId);
        if (string.IsNullOrWhiteSpace(normalizedRoomId))
        {
            RoomJoinResult invalid = RoomJoinResult.Fail(RoomJoinResultCode.InvalidRoomId, "加入失败：房间ID无效。");
            OnJoinStatusChanged?.Invoke(invalid);
            return invalid;
        }

        if (discovery == null || !discovery.TryGetRoomUri(normalizedRoomId, out Uri targetUri))
        {
            Debug.LogWarning($"[局域网房间服务] 未找到可加入的房间：{normalizedRoomId}");
            RoomJoinResult notFound = RoomJoinResult.Fail(RoomJoinResultCode.RoomNotFound, "加入失败：未找到目标房间。");
            OnJoinStatusChanged?.Invoke(notFound);
            return notFound;
        }

        if (!discovery.TryGetRoomData(normalizedRoomId, out RoomData roomData))
        {
            RoomJoinResult noData = RoomJoinResult.Fail(RoomJoinResultCode.RoomNotFound, "加入失败：未找到目标房间。");
            OnJoinStatusChanged?.Invoke(noData);
            return noData;
        }

        RoomJoinResult policyResult = joinPolicy != null ? joinPolicy.Validate(roomData) : RoomJoinResult.Ok();
        if (policyResult == null)
        {
            policyResult = RoomJoinResult.Fail(RoomJoinResultCode.Unsupported, "加入失败：策略校验失败。");
        }

        if (policyResult.Code == RoomJoinResultCode.RoomInGame)
        {
            StartWaitingJoin(normalizedRoomId);
            RoomJoinResult waiting = RoomJoinResult.Fail(RoomJoinResultCode.Waiting, $"房间正在游戏中，已进入等待：{roomData.RoomName}");
            OnJoinStatusChanged?.Invoke(waiting);
            return waiting;
        }

        if (policyResult.Code != RoomJoinResultCode.Success)
        {
            OnJoinStatusChanged?.Invoke(policyResult);
            return policyResult;
        }

        if (roomData.HasPassword)
        {
            string provided = roomPassword ?? string.Empty;
            string expected = roomData.RoomPassword ?? string.Empty;
            if (!string.Equals(provided, expected, StringComparison.Ordinal))
            {
                RoomJoinResult passwordIncorrect = RoomJoinResult.Fail(RoomJoinResultCode.PasswordIncorrect, "加入失败：房间密码错误。");
                OnJoinStatusChanged?.Invoke(passwordIncorrect);
                return passwordIncorrect;
            }
        }

        waitingRoomId = null;
        CancelInvoke(nameof(RefreshRooms));
        Debug.Log($"[局域网房间服务] 正在连接：{targetUri}");
        NetworkManager.singleton.StartClient(targetUri);
        RoomJoinResult success = RoomJoinResult.Ok("正在连接局域网房间...");
        OnJoinStatusChanged?.Invoke(success);
        return success;
    }

    public void CancelWaitingJoin()
    {
        waitingRoomId = null;
        CancelInvoke(nameof(RefreshRooms));
        RoomJoinResult cancelled = RoomJoinResult.Fail(RoomJoinResultCode.Cancelled, "已取消等待加入。");
        OnJoinStatusChanged?.Invoke(cancelled);
    }

    void FinishRefresh()
    {
        cachedRooms.Clear();
        if (discovery != null)
        {
            cachedRooms.AddRange(discovery.GetLatestRooms());
        }

        TryAutoJoinWaitingRoom();
        IsRefreshing = false;
        OnRoomListUpdated?.Invoke(cachedRooms);
    }

    void StartWaitingJoin(string roomId)
    {
        waitingRoomId = roomId;
        CancelInvoke(nameof(RefreshRooms));
        InvokeRepeating(nameof(RefreshRooms), Mathf.Max(0.5f, waitingRefreshIntervalSeconds), Mathf.Max(0.5f, waitingRefreshIntervalSeconds));
    }

    void TryAutoJoinWaitingRoom()
    {
        if (string.IsNullOrWhiteSpace(waitingRoomId) || discovery == null)
        {
            return;
        }

        if (!discovery.TryGetRoomData(waitingRoomId, out RoomData roomData))
        {
            OnJoinStatusChanged?.Invoke(RoomJoinResult.Fail(RoomJoinResultCode.RoomNotFound, "等待中的房间已不存在。"));
            waitingRoomId = null;
            CancelInvoke(nameof(RefreshRooms));
            return;
        }

        if (roomData.IsPlaying)
        {
            OnJoinStatusChanged?.Invoke(RoomJoinResult.Fail(RoomJoinResultCode.Waiting, $"等待中：{roomData.RoomName}（游戏未结束）"));
            return;
        }

        string targetRoomId = waitingRoomId;
        waitingRoomId = null;
        CancelInvoke(nameof(RefreshRooms));
        JoinRoom(targetRoomId);
    }

    static string NormalizeRoomId(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return null;
        }

        return roomId.Trim();
    }
}
