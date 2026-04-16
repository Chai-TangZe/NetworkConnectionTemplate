using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [Header("大厅信息")]
    [SerializeField] Text statusText;
    [SerializeField] InputField roomIdSearchInput;
    [SerializeField] UIItemContainer roomListContainer;
    [SerializeField] LobbyRoomListItem roomListItemPrefab;
    [Header("房间预览")]
    [SerializeField] Image selectedRoomPosterImage;
    [SerializeField] Text selectedRoomPasswordText;
    [SerializeField] Text selectedRoomStateText;
    [SerializeField] Text selectedRoomPlayersText;
    [SerializeField] Text selectedRoomMapText;
    [SerializeField] Text selectedRoomNameText;
    [SerializeField] Button joinSelectedRoomButton;
    [Header("密码面板")]
    [SerializeField] GameObject joinPasswordPanelRoot;
    [SerializeField] InputField joinPasswordInput;
    [SerializeField] Text joinPasswordTipText;

    [Header("创建房间面板")]
    [SerializeField] CreateRoomPanelUI createRoomPanel;
    /// <summary>仅当作为纯客户端加入且需本地兜底加载时使用，主机应走 RoomManager.ServerChangeScene。</summary>
    [SerializeField] string roomSceneName = "Room";

    [Header("人物角标")]
    [SerializeField] Text profileCornerText;

    [Header("进入房间前等待")]
    [SerializeField]
    [Tooltip("房主登记完成后再等待若干帧，再 ServerChangeScene，避免与 Mirror 客户端 AddPlayer 抢同一帧。")]
    int waitFramesBeforeRoomScene = 2;

    readonly List<RoomData> roomList = new List<RoomData>();
    IRoomService roomService;
    string manualStatus;
    Coroutine pendingRoomSceneCoroutine;
    RoomData selectedRoom;
    RoomData pendingPasswordRoom;

    void Awake()
    {
        ResolveRoomService();
        SetupCreateRoomPanel();
        CloseJoinPasswordPanel();
    }

    void OnEnable()
    {
        ResolveRoomService();
        RefreshProfileCorner();
        if (roomService != null)
        {
            roomService.OnRoomListUpdated += OnRoomListUpdated;
            roomService.OnJoinStatusChanged += OnJoinStatusChanged;
            if (roomService is LANRoomService lanRoom)
            {
                lanRoom.OnHostRoomAuthorityReady += OnHostRoomAuthorityReady;
            }

            roomService.RefreshRooms();
        }
        else
        {
            RefreshUI();
        }
    }

    void OnDisable()
    {
        if (pendingRoomSceneCoroutine != null)
        {
            StopCoroutine(pendingRoomSceneCoroutine);
            pendingRoomSceneCoroutine = null;
        }

        if (roomService != null)
        {
            roomService.OnRoomListUpdated -= OnRoomListUpdated;
            roomService.OnJoinStatusChanged -= OnJoinStatusChanged;
            if (roomService is LANRoomService lanRoom)
            {
                lanRoom.OnHostRoomAuthorityReady -= OnHostRoomAuthorityReady;
            }
        }
    }

    public void OnClickRefreshRoomList()
    {
        manualStatus = string.Empty;
        if (roomService != null)
        {
            roomService.RefreshRooms();
        }
        else
        {
            RefreshUI();
        }
    }

    public void OnClickJoinByRoomId()
    {
        if (roomService == null)
        {
            manualStatus = "加入失败：未绑定房间服务。";
            RefreshUI();
            return;
        }

        if (roomList.Count == 0)
        {
            manualStatus = "加入失败：当前没有可加入的房间。";
            RefreshUI();
            return;
        }

        string searchId = roomIdSearchInput != null ? roomIdSearchInput.text : string.Empty;
        if (string.IsNullOrWhiteSpace(searchId))
        {
            manualStatus = "加入失败：请输入房间ID。";
            RefreshUI();
            return;
        }

        RoomData targetRoom = roomList.Find(room => room != null && room.RoomId == searchId.Trim());
        if (targetRoom == null)
        {
            manualStatus = "加入失败：未找到该房间ID。";
            RefreshUI();
            return;
        }

        // 按房间号搜索命中后直接走加入流程（无需额外确认）。
        TryJoinRoom(targetRoom, true);
    }

    public void OnClickOpenCreateRoomPanel()
    {
        if (createRoomPanel == null)
        {
            return;
        }

        createRoomPanel.Open();
    }

    public void OnClickCreateRoom()
    {
        OnClickOpenCreateRoomPanel();
    }

    public void OnClickCloseCreateRoomPanel()
    {
        if (createRoomPanel != null)
        {
            createRoomPanel.Close();
        }
    }

    void OnCreateRoomRequested(string roomName, string mapName, int maxPlayers, string roomId, string roomPassword)
    {
        if (roomService == null)
        {
            manualStatus = "建房失败：未绑定房间服务。";
            RefreshUI();
            return;
        }

        RoomJoinResult result = roomService.CreateRoom(roomName, mapName, maxPlayers, roomId, roomPassword);
        manualStatus = result != null ? result.Message : "建房失败。";
        if (result != null && result.Code == RoomJoinResultCode.Success)
        {
            if (createRoomPanel != null)
            {
                createRoomPanel.Close();
            }

            if (roomService is LANRoomService)
            {
                // 局域网建房：房主登记在 StartHost 后异步完成，进入房间场景由 OnHostRoomAuthorityReady 触发。
            }
            else
            {
                if (pendingRoomSceneCoroutine != null)
                {
                    StopCoroutine(pendingRoomSceneCoroutine);
                }

                pendingRoomSceneCoroutine = StartCoroutine(EnterRoomSceneWhenPrepared());
            }
        }

        RefreshUI();
    }

    void OnHostRoomAuthorityReady()
    {
        if (pendingRoomSceneCoroutine != null)
        {
            StopCoroutine(pendingRoomSceneCoroutine);
        }

        pendingRoomSceneCoroutine = StartCoroutine(EnterRoomSceneWhenPrepared());
    }

    public void OnClickCancelWaitingJoin()
    {
        if (roomService == null)
        {
            return;
        }

        roomService.CancelWaitingJoin();
    }

    public void RefreshUI()
    {
        string status = !string.IsNullOrWhiteSpace(manualStatus)
            ? manualStatus
            : roomService != null
            ? (roomService.IsWaitingForRoom
                ? "大厅：等待目标房间游戏结束..."
                : (roomService.IsRefreshing ? "大厅：正在刷新房间列表..." : "大厅：已加载房间服务数据。"))
            : "大厅：未绑定房间服务。";
        SetStatus(status);
        ApplyRoomList(roomList);
        RefreshSelectedRoomPreview();
    }

    void OnRoomListUpdated(IReadOnlyList<RoomData> rooms)
    {
        string selectedRoomId = selectedRoom != null ? selectedRoom.RoomId : null;
        roomList.Clear();
        if (rooms != null)
        {
            roomList.AddRange(rooms);
        }

        if (!string.IsNullOrWhiteSpace(selectedRoomId))
        {
            selectedRoom = roomList.Find(room => room != null && room.RoomId == selectedRoomId);
        }
        else if (selectedRoom == null && roomList.Count > 0)
        {
            selectedRoom = roomList[0];
        }

        RefreshUI();
    }

    void OnJoinStatusChanged(RoomJoinResult result)
    {
        if (result == null)
        {
            return;
        }

        manualStatus = result.Message;
        RefreshUI();
    }

    void ApplyRoomList(List<RoomData> roomList)
    {
        RefreshRoomListItems(roomList);
        LogRoomListSnapshot(roomList);
    }

    void SelectRoom(RoomData room)
    {
        selectedRoom = room;
        RefreshSelectedRoomPreview();
    }

    public void OnClickJoinSelectedRoom()
    {
        TryJoinRoom(selectedRoom, false);
    }

    void TryJoinRoom(RoomData room, bool direct)
    {
        if (room == null)
        {
            manualStatus = "加入失败：请先选择房间。";
            RefreshUI();
            return;
        }

        if (room.HasPassword)
        {
            pendingPasswordRoom = room;
            OpenJoinPasswordPanel();
            if (direct && joinPasswordTipText != null)
            {
                joinPasswordTipText.text = "该房间需要密码，请输入后加入。";
            }
            return;
        }

        TryJoinRoomWithPassword(room, string.Empty);
    }

    public void OnClickConfirmJoinPassword()
    {
        if (pendingPasswordRoom == null)
        {
            CloseJoinPasswordPanel();
            return;
        }

        string password = joinPasswordInput != null ? joinPasswordInput.text : string.Empty;
        TryJoinRoomWithPassword(pendingPasswordRoom, password);
    }

    public void OnClickBackJoinPasswordPanel()
    {
        CloseJoinPasswordPanel();
    }

    void TryJoinRoomWithPassword(RoomData room, string password)
    {
        if (roomService == null || room == null)
        {
            manualStatus = "加入失败：参数无效。";
            RefreshUI();
            return;
        }

        RoomJoinResult joinResult = roomService.JoinRoom(room.RoomId, password);
        manualStatus = joinResult != null && joinResult.Code == RoomJoinResultCode.Success
            ? $"正在加入：{room.RoomName}"
            : (joinResult != null ? joinResult.Message : $"加入失败：{room.RoomName}");

        if (joinResult != null && joinResult.Code == RoomJoinResultCode.PasswordIncorrect)
        {
            if (joinPasswordTipText != null)
            {
                joinPasswordTipText.text = "密码错误！";
            }

            RefreshUI();
            return;
        }

        if (joinResult != null && joinResult.Code == RoomJoinResultCode.Success)
        {
            CloseJoinPasswordPanel();
            if (pendingRoomSceneCoroutine != null)
            {
                StopCoroutine(pendingRoomSceneCoroutine);
            }

            pendingRoomSceneCoroutine = StartCoroutine(EnterRoomSceneWhenPrepared());
        }
        RefreshUI();
    }

    /// <summary>
    /// 等网络侧与 Mirror 状态稳定后再切房间场景，减少 “There is already a player for this connection”。
    /// </summary>
    IEnumerator EnterRoomSceneWhenPrepared()
    {
        manualStatus = "正在准备进入房间...";
        RefreshUI();

        for (int i = 0; i < Mathf.Max(0, waitFramesBeforeRoomScene); i++)
        {
            yield return null;
        }

        // 主机：再等 RoomManager 与本地连接就绪；纯客户端：必须已真正连上服务器后才允许进房间场景。
        int guard = 60;
        while (guard-- > 0)
        {
            RoomManager rm = RoomManager.Instance;
            bool hostReady = !NetworkServer.active || (rm != null && NetworkServer.localConnection != null);
            bool clientReady = NetworkServer.active || NetworkClient.isConnected;
            if (hostReady && clientReady)
            {
                break;
            }

            yield return null;
        }

        if (!NetworkServer.active && !NetworkClient.isConnected)
        {
            manualStatus = "加入失败：房间已满或连接已断开。";
            RefreshUI();
            pendingRoomSceneCoroutine = null;
            yield break;
        }

        bool entered = TryEnterRoomScene();
        if (!entered)
        {
            manualStatus = "加入失败：未进入房间。";
        }

        if (manualStatus == "正在准备进入房间...")
        {
            manualStatus = string.Empty;
        }

        RefreshUI();
        pendingRoomSceneCoroutine = null;
    }

    void RefreshRoomListItems(List<RoomData> roomList)
    {
        Transform parent = roomListContainer != null ? roomListContainer.GetItemsParent() : null;
        if (parent == null || roomListItemPrefab == null)
        {
            return;
        }

        roomListContainer.ClearItems();

        foreach (RoomData room in roomList)
        {
            LobbyRoomListItem item = Instantiate(roomListItemPrefab, parent);
            item.SetData(room, SelectRoom);
        }
    }

    void ResolveRoomService()
    {
        IRoomService sessionLanService = NetworkSessionRoot.Instance != null ? NetworkSessionRoot.Instance.LanRoomService : null;
        IRoomService sessionOnlineService = NetworkSessionRoot.Instance != null ? NetworkSessionRoot.Instance.OnlineRoomService : null;

        NetworkModeType mode = PlayerProfileContext.Instance != null
            ? PlayerProfileContext.Instance.SelectedMode
            : NetworkModeType.LAN;

        if (mode == NetworkModeType.LAN && sessionLanService != null)
        {
            roomService = sessionLanService;
            return;
        }

        if (mode == NetworkModeType.Online && sessionOnlineService != null)
        {
            roomService = sessionOnlineService;
            return;
        }

        roomService = null;
    }

    void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    void SetButtonState(Button button, bool enabled)
    {
        if (button != null)
        {
            button.interactable = enabled;
        }
    }

    void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    void RefreshProfileCorner()
    {
        if (profileCornerText == null)
        {
            return;
        }

        PlayerProfileContext context = PlayerProfileContext.Instance;
        if (context == null)
        {
            profileCornerText.text = "玩家：默认";
            return;
        }

        context.EnsureDefaultProfile();
        PlayerData data = context.CurrentProfile;
        profileCornerText.text = data != null
            ? $"玩家：{data.PlayerName}  形象：{data.AvatarId}"
            : "玩家：默认";
    }

    void SetupCreateRoomPanel()
    {
        if (createRoomPanel == null)
        {
            return;
        }

        createRoomPanel.OnCreateRequested -= OnCreateRoomRequested;
        createRoomPanel.OnBackRequested -= OnClickCloseCreateRoomPanel;
        createRoomPanel.OnCreateRequested += OnCreateRoomRequested;
        createRoomPanel.OnBackRequested += OnClickCloseCreateRoomPanel;
        createRoomPanel.SetRoomIdProvider(GenerateUniqueRoomId);

        if (PlayerProfileContext.Instance != null)
        {
            createRoomPanel.SetDefaultPlayerName(PlayerProfileContext.Instance.CurrentProfile != null
                ? PlayerProfileContext.Instance.CurrentProfile.PlayerName
                : "玩家");
        }
    }

    string GenerateUniqueRoomId()
    {
        for (int i = 0; i < 32; i++)
        {
            string candidate = UnityEngine.Random.Range(100000, 999999).ToString();
            if (!roomList.Exists(room => room != null && room.RoomId == candidate))
            {
                return candidate;
            }
        }

        return DateTime.Now.Ticks.ToString().Substring(8, 6);
    }

    void RefreshSelectedRoomPreview()
    {
        RoomData room = selectedRoom;
        if (room == null)
        {
            SetText(selectedRoomPasswordText, "无密码");
            SetText(selectedRoomStateText, "状态：--");
            SetText(selectedRoomPlayersText, "--/--");
            SetText(selectedRoomMapText, "地图名称");
            SetText(selectedRoomNameText, "未选择房间");
            if (selectedRoomPosterImage != null)
            {
                selectedRoomPosterImage.sprite = null;
                selectedRoomPosterImage.enabled = false;
            }

            SetButtonState(joinSelectedRoomButton, false);
            return;
        }

        SetText(selectedRoomPasswordText, room.HasPassword ? "有密码" : "无密码");
        SetText(selectedRoomStateText, room.IsPlaying ? "游戏中" : "招募中");
        SetText(selectedRoomPlayersText, $"{room.CurrentPlayers}/{room.MaxPlayers}人");
        SetText(selectedRoomMapText, room.MapName);
        SetText(selectedRoomNameText, room.RoomName);
        MapDataDefinition mapData = MapDataRepository.GetByMapName(room.MapName);
        if (selectedRoomPosterImage != null)
        {
            selectedRoomPosterImage.sprite = mapData != null ? mapData.poster : null;
            selectedRoomPosterImage.enabled = selectedRoomPosterImage.sprite != null;
        }

        SetButtonState(joinSelectedRoomButton, !room.IsPlaying && room.CurrentPlayers < room.MaxPlayers);
    }

    void OpenJoinPasswordPanel()
    {
        if (joinPasswordPanelRoot != null)
        {
            joinPasswordPanelRoot.SetActive(true);
        }

        if (joinPasswordInput != null)
        {
            joinPasswordInput.text = string.Empty;
        }

        if (joinPasswordTipText != null)
        {
            joinPasswordTipText.text = string.Empty;
        }
    }

    void CloseJoinPasswordPanel()
    {
        if (joinPasswordPanelRoot != null)
        {
            joinPasswordPanelRoot.SetActive(false);
        }

        pendingPasswordRoom = null;
        if (joinPasswordInput != null)
        {
            joinPasswordInput.text = string.Empty;
        }
    }

    /// <summary>
    /// 主机建房/入房后进入房间场景：必须用 Mirror 的 ServerChangeScene，与 NetworkRoomManager 的房间玩家生命周期一致。
    /// 若仅用 SceneManager.LoadScene，会导致房间玩家与房主权威与 Mirror 预期不一致。
    /// </summary>
    bool TryEnterRoomScene()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (roomManager != null && NetworkServer.active && !string.IsNullOrWhiteSpace(roomManager.RoomScene))
        {
            if (!Utils.IsSceneActive(roomManager.RoomScene))
            {
                roomManager.ServerChangeScene(roomManager.RoomScene);
            }

            return true;
        }

        if (!NetworkClient.isConnected || string.IsNullOrWhiteSpace(roomSceneName))
        {
            return false;
        }

        if (SceneManager.GetActiveScene().name != roomSceneName)
        {
            SceneManager.LoadScene(roomSceneName);
        }

        return true;
    }

    void LogRoomListSnapshot(List<RoomData> rooms)
    {
        if (rooms == null || rooms.Count == 0)
        {
            Debug.Log("[大厅房间列表] 当前无房间。");
            return;
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomData room = rooms[i];
            if (room == null)
            {
                continue;
            }

            Debug.Log($"[大厅房间列表] #{i + 1} 房间ID:{room.RoomId} 名称:{room.RoomName} 地图:{room.MapName} 人数:{room.CurrentPlayers}/{room.MaxPlayers} 密码:{(room.HasPassword ? "有" : "无")} 状态:{(room.IsPlaying ? "游戏中" : "招募中")} 房主NetId:{room.OwnerPlayerNetId}");
        }
    }

}
