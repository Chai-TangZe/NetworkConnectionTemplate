using Mirror;
using UnityEngine;

public class RoomPlayer : NetworkRoomPlayer
{
    public static bool HasSyncedRoomInfo { get; private set; }
    public static string SyncedRoomId { get; private set; } = "000000";
    public static string SyncedRoomName { get; private set; } = "默认房间";
    public static string SyncedMapName { get; private set; } = "Map01";
    public static int SyncedMaxPlayers { get; private set; } = 4;
    public static bool SyncedIsPlaying { get; private set; }
    public static double SyncedGameEndTime { get; private set; }

    // 房间阶段的玩家基础信息。
    // 这里不直接放 UI 逻辑，UI 通过 RoomEvents 监听这些数据变化。
    [Header("玩家信息")]
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string PlayerName = "玩家";

    [SyncVar(hook = nameof(OnAvatarIdChanged))]
    public int AvatarId;

    // 房主标记只描述“谁有房间管理权限”，不表示服务器身份。
    [Header("房间状态")]
    [SyncVar(hook = nameof(OnLeaderChanged))]
    public bool IsLeader;

    [SyncVar(hook = nameof(OnRoomOwnerNetIdChanged))]
    public uint RoomOwnerNetId;

    [SyncVar(hook = nameof(OnReadyFlagChanged))]
    public bool IsReady;

    public override void OnStartClient()
    {
        // 项目使用 RoomUI，关闭每个房间玩家上的 Mirror 测试用 OnGUI。
        showRoomGUI = false;
        base.OnStartClient();
        NotifyUpdated();
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        // 优先使用本地角色设置；如果还没有设置则回退到随机默认名。
        PlayerData localProfile = PlayerProfileContext.Instance != null
            ? PlayerProfileContext.Instance.CurrentProfile
            : null;

        string name = localProfile != null && !string.IsNullOrWhiteSpace(localProfile.PlayerName)
            ? localProfile.PlayerName
            : $"玩家_{Random.Range(1000, 9999)}";
        int avatarId = localProfile != null ? Mathf.Max(0, localProfile.AvatarId) : 0;

        CmdSetPlayerInfo(name, avatarId);
        CmdRequestRoomInfoSnapshot();
    }

    public override void OnClientEnterRoom()
    {
        base.OnClientEnterRoom();
        NotifyUpdated();
        RoomEvents.RaisePlayerListChanged();
    }

    public override void OnClientExitRoom()
    {
        base.OnClientExitRoom();
        // Mirror 会在任意房间玩家对象离开时广播 CallOnClientExitRoom。
        // 这里不能清空静态房间信息缓存，否则“别人退出”会把本地面板打回默认值。
        RoomEvents.RaisePlayerListChanged();
    }

    public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
    {
        base.ReadyStateChanged(oldReadyState, newReadyState);
        NotifyUpdated();
    }

    public override void IndexChanged(int oldIndex, int newIndex)
    {
        base.IndexChanged(oldIndex, newIndex);
        NotifyUpdated();
    }

    [Command]
    public void CmdSetPlayerInfo(string playerName, int avatarId)
    {
        PlayerName = string.IsNullOrWhiteSpace(playerName) ? $"玩家_{netId}" : playerName.Trim();
        AvatarId = Mathf.Max(0, avatarId);
    }

    public void CmdSetReady(bool ready)
    {
        // Mirror 原生 readyToBegin 仍同步给 NetworkRoomManager，但业务与 UI 统一使用 IsReady。
        CmdChangeReadyState(ready);
        CmdSetReadyFlag(ready);
    }

    [Command]
    void CmdSetReadyFlag(bool ready)
    {
        IsReady = ready;
    }

    [Command]
    public void CmdChangeRoomInfo(string roomName, string mapName, int maxPlayers)
    {
        // 改房间信息属于房主管理行为，所以入口放在 RoomPlayer，
        // 但真正规则仍然交给 RoomManager 处理。
        if (RoomManager.Instance == null || !RoomManager.Instance.IsRoomOwnerConnection(connectionToClient))
        {
            return;
        }

        RoomManager.Instance.ServerUpdateRoomInfo(roomName, mapName, maxPlayers);
    }

    [Command]
    void CmdRequestRoomInfoSnapshot()
    {
        if (RoomManager.Instance == null || connectionToClient == null)
        {
            return;
        }

        RoomManager.Instance.ServerBroadcastRoomInfoSnapshotTo(connectionToClient);
    }

    public void SetRoomInfo(string roomName, string mapName, int maxPlayers)
    {
        // 主机本地可直接走服务端路径，避免 Host 模式下偶发 Command 时序导致的“修改后被还原”。
        if (isServer)
        {
            if (RoomManager.Instance == null || !RoomManager.Instance.IsRoomOwnerConnection(connectionToClient))
            {
                return;
            }

            RoomManager.Instance.ServerUpdateRoomInfo(roomName, mapName, maxPlayers);
            return;
        }

        CmdChangeRoomInfo(roomName, mapName, maxPlayers);
    }

    [Server]
    public void ServerPushRoomInfoTo(NetworkConnectionToClient target, string roomId, string roomName, string mapName, int maxPlayers, bool isPlaying, double gameEndTime)
    {
        if (target == null)
        {
            return;
        }

        TargetApplyRoomInfo(target, roomId, roomName, mapName, maxPlayers, isPlaying, gameEndTime);
    }

    [Command]
    public void CmdStartGame()
    {
        // 只有房主可以触发开始，且开始条件由 RoomManager 统一判断。
        if (RoomManager.Instance == null || !RoomManager.Instance.IsRoomOwner(this))
        {
            return;
        }

        RoomManager.Instance.ServerStartGame();
    }

    [Command]
    public void CmdSendRoomChat(string text)
    {
        if (RoomManager.Instance == null)
        {
            return;
        }

        RoomManager.Instance.ServerBroadcastRoomChat(PlayerName, text);
    }

    [TargetRpc]
    public void TargetReceiveRoomChat(NetworkConnection target, string senderName, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string safeSender = string.IsNullOrWhiteSpace(senderName) ? "未知玩家" : senderName.Trim();
        RoomEvents.RaiseRoomChatMessage($"[{safeSender}] {text}");
    }

    [TargetRpc]
    void TargetApplyRoomInfo(NetworkConnection target, string roomId, string roomName, string mapName, int maxPlayers, bool isPlaying, double gameEndTime)
    {
        HasSyncedRoomInfo = true;
        SyncedRoomId = string.IsNullOrWhiteSpace(roomId) ? SyncedRoomId : roomId.Trim();
        SyncedRoomName = string.IsNullOrWhiteSpace(roomName) ? SyncedRoomName : roomName;
        SyncedMapName = string.IsNullOrWhiteSpace(mapName) ? SyncedMapName : mapName;
        SyncedMaxPlayers = Mathf.Max(1, maxPlayers);
        SyncedIsPlaying = isPlaying;
        SyncedGameEndTime = gameEndTime;
        RoomEvents.RaiseRoomInfoChanged();
        RoomEvents.RaiseRoomStateChanged(isPlaying);
    }

    void OnPlayerNameChanged(string oldValue, string newValue)
    {
        NotifyUpdated();
    }

    void OnAvatarIdChanged(int oldValue, int newValue)
    {
        NotifyUpdated();
    }

    void OnLeaderChanged(bool oldValue, bool newValue)
    {
        NotifyUpdated();
    }

    void OnRoomOwnerNetIdChanged(uint oldValue, uint newValue)
    {
        NotifyUpdated();
    }

    void OnReadyFlagChanged(bool oldValue, bool newValue)
    {
        NotifyUpdated();
    }

    void NotifyUpdated()
    {
        RoomEvents.RaisePlayerUpdated(this);
    }
}
