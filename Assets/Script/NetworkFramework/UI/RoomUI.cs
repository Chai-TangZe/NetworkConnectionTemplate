using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RoomUI : MonoBehaviour
{
    [Header("房间信息")]
    [SerializeField] Text roomIdText;
    [SerializeField] Text roomNameText;
    [SerializeField] Text mapNameText;
    [SerializeField] Text playerCountText;
    [SerializeField] Text roomStateText;
    [SerializeField] Image mapPosterImage;

    [Header("玩家信息")]
    [SerializeField] Transform playerListParent;
    [SerializeField] UIItemContainer playerListContainer;
    [SerializeField] RoomPlayerListItem playerListItemPrefab;

    [Header("编辑区域")]
    [FormerlySerializedAs("leaderControlsRoot")]
    [SerializeField] GameObject roomInfoEditControlsRoot;
    [FormerlySerializedAs("clientControlsRoot")]
    [SerializeField] GameObject roomInfoViewControlsRoot;
    [SerializeField] InputField roomNameInput;
    [SerializeField] Dropdown mapDropdown;
    [SerializeField] InputField maxPlayersInput;

    [Header("按钮")]
    [SerializeField] Button readyButton;
    [SerializeField] Text readyButtonText;
    [SerializeField] Button startGameButton;
    [SerializeField] Button roomInfoEditToggleButton;
    [SerializeField] Text roomInfoEditToggleButtonText;

    [Header("房间聊天")]
    [SerializeField] Text roomChatText;
    [SerializeField] InputField roomChatInput;
    [SerializeField] int maxChatLines = 20;

    readonly System.Collections.Generic.Queue<string> roomChatLines = new System.Collections.Generic.Queue<string>();
    bool roomInfoDirty;
    Coroutine delayedStartCoroutine;
    string lastPlayerSnapshot;
    bool missingRoomManagerLogged;
    bool suppressLeaderInputCallbacks;
    bool isRoomInfoEditMode;

    void OnEnable()
    {
        roomChatLines.Clear();
        if (roomChatText != null)
        {
            roomChatText.text = string.Empty;
        }

        SetupMapDropdown();
        RoomEvents.OnPlayerListChanged += RefreshUI;
        RoomEvents.OnPlayerUpdated += OnPlayerUpdated;
        RoomEvents.OnRoomInfoChanged += RefreshUI;
        RoomEvents.OnRoomStateChanged += OnRoomStateChanged;
        RoomEvents.OnRoomChatMessage += OnRoomChatMessage;
        RefreshUI();
    }

    void OnDisable()
    {
        RoomEvents.OnPlayerListChanged -= RefreshUI;
        RoomEvents.OnPlayerUpdated -= OnPlayerUpdated;
        RoomEvents.OnRoomInfoChanged -= RefreshUI;
        RoomEvents.OnRoomStateChanged -= OnRoomStateChanged;
        RoomEvents.OnRoomChatMessage -= OnRoomChatMessage;

        if (delayedStartCoroutine != null)
        {
            StopCoroutine(delayedStartCoroutine);
            delayedStartCoroutine = null;
        }

        isRoomInfoEditMode = false;
    }

    public void OnClickReady()
    {
        RoomPlayer localPlayer = GetLocalRoomPlayer();
        if (localPlayer != null)
        {
            localPlayer.CmdSetReady(!localPlayer.IsReady);
        }
    }

    public void OnClickStartGame()
    {
        if (delayedStartCoroutine != null)
        {
            return;
        }

        RoomPlayer localPlayer = GetLocalRoomPlayer();
        RoomManager roomManager = RoomManager.Instance;
        if (localPlayer != null && roomManager != null && IsLocalPlayerOwner(localPlayer) && CanLeaderStartGame(roomManager))
        {
            localPlayer.CmdSetReady(true);
            delayedStartCoroutine = StartCoroutine(DelayedStartCoroutine(localPlayer));
        }
    }

    // 兼容旧按钮绑定：原“取消准备”按钮复用同一套切换逻辑。
    public void OnClickCancelReady()
    {
        OnClickReady();
    }

    // 兼容旧按钮绑定：原“应用房间信息”按钮改为标记脏数据并在刷新时自动下发。
    public void OnClickApplyRoomInfo()
    {
        RoomManager roomManager = RoomManager.Instance;
        RoomPlayer localPlayer = GetLocalRoomPlayer();
        if (roomManager == null || localPlayer == null || !IsLocalPlayerOwner(localPlayer) || roomManager.IsPlaying)
        {
            return;
        }

        if (!isRoomInfoEditMode)
        {
            // 进入编辑态前先把输入框同步为当前展示值。
            SyncRoomInfoInputsFromCurrent(roomManager);
            roomInfoDirty = false;
            isRoomInfoEditMode = true;
            RefreshUI();
            return;
        }

        ApplyLeaderRoomInfo(localPlayer, roomManager);
        roomInfoDirty = false;
        isRoomInfoEditMode = false;
        RefreshUI();
    }

    public void OnLeaderRoomNameChanged(string _)
    {
        if (suppressLeaderInputCallbacks)
        {
            return;
        }

        if (!isRoomInfoEditMode)
        {
            return;
        }

        roomInfoDirty = true;
    }

    public void OnLeaderMapChanged(int _)
    {
        if (suppressLeaderInputCallbacks)
        {
            return;
        }

        if (!isRoomInfoEditMode)
        {
            return;
        }

        roomInfoDirty = true;

        // 房主本地改地图时立刻预览海报，避免必须等一次网络回写才更新视觉。
        RoomManager roomManager = RoomManager.Instance;
        string fallback = roomManager != null ? roomManager.MapName : "Map01";
        RefreshMapPoster(GetSelectedMapName(fallback));
    }

    public void OnLeaderMaxPlayersChanged(string _)
    {
        if (suppressLeaderInputCallbacks)
        {
            return;
        }

        if (!isRoomInfoEditMode)
        {
            return;
        }

        roomInfoDirty = true;
    }

    public void OnClickSendRoomChat()
    {
        RoomPlayer localPlayer = GetLocalRoomPlayer();
        if (localPlayer == null || roomChatInput == null)
        {
            return;
        }

        string content = roomChatInput.text;
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        localPlayer.CmdSendRoomChat(content);
        roomChatInput.text = string.Empty;
    }

    void OnPlayerUpdated(RoomPlayer _)
    {
        RefreshUI();
    }

    void OnRoomStateChanged(bool _)
    {
        RefreshUI();
    }

    void OnRoomChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        roomChatLines.Enqueue(message);
        while (roomChatLines.Count > Mathf.Max(1, maxChatLines))
        {
            roomChatLines.Dequeue();
        }

        if (roomChatText != null)
        {
            roomChatText.text = string.Join("\n", roomChatLines.ToArray());
        }
    }

    void RefreshUI()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (roomManager == null)
        {
            if (!missingRoomManagerLogged)
            {
                Debug.LogWarning("[房间界面] 未找到 RoomManager，等待网络会话初始化。");
                missingRoomManagerLogged = true;
            }

            SetText(roomNameText, string.Empty);
            SetText(roomIdText, string.Empty);
            SetText(mapNameText, string.Empty);
            SetText(playerCountText, string.Empty);
            SetText(roomStateText, string.Empty);
            return;
        }
        missingRoomManagerLogged = false;

        RoomPlayer localPlayer = GetLocalRoomPlayer();
        bool isLeader = IsLocalPlayerOwner(localPlayer);
        bool isReady = localPlayer != null && localPlayer.IsReady;
        if ((!isLeader || roomManager.IsPlaying) && isRoomInfoEditMode)
        {
            isRoomInfoEditMode = false;
        }

        string syncedRoomId = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedRoomId : roomManager.RoomId;
        string syncedRoomName = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedRoomName : roomManager.RoomName;
        string syncedMapName = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedMapName : roomManager.MapName;
        int syncedMaxPlayers = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedMaxPlayers : roomManager.MaxPlayersCount;
        bool syncedIsPlaying = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedIsPlaying : roomManager.IsPlaying;

        SetText(roomIdText, $"房间号：{syncedRoomId}");
        SetText(roomNameText, $"房间：{syncedRoomName}");
        SetText(mapNameText, $"地图：{syncedMapName}");
        SetText(playerCountText, $"人数：{roomManager.GetPlayers().Count}/{syncedMaxPlayers}");
        SetText(roomStateText, syncedIsPlaying ? "状态：游戏中" : "状态：等待中");
        RefreshMapPoster(syncedMapName);
        RefreshPlayerListItems(roomManager);
        LogPlayerSnapshot(roomManager);

        if (!isRoomInfoEditMode)
        {
            SyncRoomInfoInputsFromCurrent(roomManager);
        }

        if (roomInfoEditControlsRoot != null)
        {
            roomInfoEditControlsRoot.SetActive(isLeader && isRoomInfoEditMode && !roomManager.IsPlaying);
        }

        if (roomInfoViewControlsRoot != null)
        {
            roomInfoViewControlsRoot.SetActive(!isLeader || !isRoomInfoEditMode || roomManager.IsPlaying);
        }

        SetButtonState(readyButton, localPlayer != null && !isLeader);
        SetText(readyButtonText, isReady ? "取消准备" : "准备");
        SetButtonState(startGameButton, isLeader && CanLeaderStartGame(roomManager) && !roomManager.IsPlaying);
        SetButtonState(roomInfoEditToggleButton, isLeader && !roomManager.IsPlaying);
        SetText(roomInfoEditToggleButtonText, isRoomInfoEditMode ? "保存" : "修改");
    }

    RoomPlayer GetLocalRoomPlayer()
    {
        return RoomManager.Instance != null ? RoomManager.Instance.GetLocalRoomPlayer() : null;
    }

    void RefreshPlayerListItems(RoomManager roomManager)
    {
        Transform parent = GetPlayerListParent();
        if (parent == null || playerListItemPrefab == null)
        {
            return;
        }

        if (playerListContainer != null)
        {
            playerListContainer.ClearItems();
        }
        else
        {
            ClearPlayerListItems(parent);
        }

        foreach (RoomPlayer player in roomManager.GetPlayers())
        {
            RoomPlayerListItem item = Instantiate(playerListItemPrefab, parent);
            item.SetData(player);
        }
    }

    void ClearPlayerListItems(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    Transform GetPlayerListParent()
    {
        if (playerListContainer != null)
        {
            return playerListContainer.GetItemsParent();
        }

        return playerListParent;
    }

    void SetButtonState(Button button, bool active)
    {
        if (button != null)
        {
            button.interactable = active;
        }
    }

    void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    void SetupMapDropdown()
    {
        if (mapDropdown == null)
        {
            return;
        }

        mapDropdown.ClearOptions();
        mapDropdown.AddOptions(new System.Collections.Generic.List<string>(GameMapCatalog.GetMapNames()));
    }

    string GetSelectedMapName(string fallback)
    {
        if (mapDropdown != null && mapDropdown.options != null && mapDropdown.options.Count > 0)
        {
            int index = Mathf.Clamp(mapDropdown.value, 0, mapDropdown.options.Count - 1);
            return mapDropdown.options[index].text;
        }

        return fallback;
    }

    void SetMapDropdownValue(string mapName)
    {
        if (mapDropdown == null || mapDropdown.options == null || mapDropdown.options.Count == 0)
        {
            return;
        }

        for (int i = 0; i < mapDropdown.options.Count; i++)
        {
            if (mapDropdown.options[i].text == mapName)
            {
                if (mapDropdown.value != i)
                {
                    mapDropdown.value = i;
                }
                return;
            }
        }
    }

    void SyncRoomInfoInputsFromCurrent(RoomManager roomManager)
    {
        if (roomManager == null)
        {
            return;
        }

        string syncedRoomName = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedRoomName : roomManager.RoomName;
        string syncedMapName = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedMapName : roomManager.MapName;
        int syncedMaxPlayers = RoomPlayer.HasSyncedRoomInfo ? RoomPlayer.SyncedMaxPlayers : roomManager.MaxPlayersCount;

        suppressLeaderInputCallbacks = true;
        if (roomNameInput != null && !roomNameInput.isFocused)
        {
            if (roomNameInput.text != syncedRoomName)
            {
                roomNameInput.text = syncedRoomName;
            }
        }

        if (mapDropdown != null)
        {
            SetMapDropdownValue(syncedMapName);
        }

        if (maxPlayersInput != null && !maxPlayersInput.isFocused)
        {
            string syncedMaxPlayersText = syncedMaxPlayers.ToString();
            if (maxPlayersInput.text != syncedMaxPlayersText)
            {
                maxPlayersInput.text = syncedMaxPlayersText;
            }
        }
        suppressLeaderInputCallbacks = false;
    }

    void RefreshMapPoster(string mapName)
    {
        if (mapPosterImage == null)
        {
            return;
        }

        MapDataDefinition mapData = MapDataRepository.GetByMapName(mapName);
        Sprite targetPoster = mapData != null ? mapData.poster : null;

        mapPosterImage.sprite = targetPoster;
        mapPosterImage.enabled = targetPoster != null;
    }

    bool CanLeaderStartGame(RoomManager roomManager)
    {
        foreach (RoomPlayer player in roomManager.GetPlayers())
        {
            // RoomOwnerNetId 在首帧可能仍为 0，不能再用 netId==RoomOwnerNetId 判断房主，否则房主会被当成“未准备的普通玩家”。
            if (player == null || player.IsLeader)
            {
                continue;
            }

            if (!player.IsReady)
            {
                return false;
            }
        }

        return roomManager.GetPlayers().Count >= roomManager.minPlayers;
    }

    void ApplyLeaderRoomInfo(RoomPlayer localPlayer, RoomManager roomManager)
    {
        if (localPlayer == null || !IsLocalPlayerOwner(localPlayer))
        {
            return;
        }

        string targetRoomName = roomNameInput != null ? roomNameInput.text : roomManager.RoomName;
        string targetMapName = GetSelectedMapName(roomManager.MapName);

        int targetMaxPlayers = roomManager.MaxPlayersCount;
        if (maxPlayersInput != null && int.TryParse(maxPlayersInput.text, out int parsedMaxPlayers))
        {
            targetMaxPlayers = parsedMaxPlayers;
        }

        localPlayer.SetRoomInfo(targetRoomName, targetMapName, targetMaxPlayers);
    }

    IEnumerator DelayedStartCoroutine(RoomPlayer localPlayer)
    {
        yield return new WaitForSeconds(1f);

        if (localPlayer != null)
        {
            localPlayer.CmdStartGame();
        }

        delayedStartCoroutine = null;
    }

    void LogPlayerSnapshot(RoomManager roomManager)
    {
        StringBuilder builder = new StringBuilder();
        foreach (RoomPlayer player in roomManager.GetPlayers())
        {
            if (player == null)
            {
                continue;
            }

            builder.Append($"[{player.index + 1}]");
            builder.Append(player.PlayerName);
            builder.Append(player.IsLeader ? "(房主)" : string.Empty);
            builder.Append(player.IsReady ? "(已准备)" : "(未准备)");
            builder.Append(" ");
        }

        string snapshot = builder.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(snapshot) && snapshot != lastPlayerSnapshot)
        {
            lastPlayerSnapshot = snapshot;
            Debug.Log($"[房间玩家列表] {snapshot}");
        }
    }

    bool IsLocalPlayerOwner(RoomPlayer localPlayer)
    {
        if (localPlayer == null)
        {
            return false;
        }

        if (localPlayer.IsLeader)
        {
            return true;
        }

        return localPlayer.netId != 0 && localPlayer.netId == localPlayer.RoomOwnerNetId;
    }
}
