using Mirror;
using UnityEngine;

/// <summary>
/// 游戏场景中的玩家实体。此处「用户信息」与 <see cref="UserData"/>（账号）对齐，由 <see cref="RoomPlayer"/> 带入局内；
/// 「角色身体/装饰」等属于 <see cref="CharacterData"/>，后续可在本组件或其它组件上单独扩展，勿与用户字段混用。
/// </summary>
public class GamePlayer : NetworkBehaviour
{
    [Header("用户信息（账号级，便于 UI / 头像 / 聊天等复用）")]
    [Tooltip("用户展示名，与 UserData.DisplayName / RoomPlayer.PlayerName 同源。")]
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string PlayerName;

    [Tooltip("用户头像 Id（AvatarDataDefinition.avatarId），非角色身体装扮。")]
    [SyncVar(hook = nameof(OnAvatarIdChanged))]
    public int AvatarId;

    [Tooltip("用户个人描述，与 UserData.Description / RoomPlayer.PlayerDescription 同源。")]
    [SyncVar(hook = nameof(OnUserDescriptionChanged))]
    public string UserDescription = string.Empty;
    
    [Header("头顶显示")]
    [SerializeField] TextMesh overheadText;
    PlayerMove playerMove;

    public override void OnStartClient()
    {
        base.OnStartClient();
        RefreshOverhead();
        ApplyLocalControlState();
        if (isOwned)
        {
            BindSceneCamera();
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        ApplyLocalControlState();
        BindSceneCamera();
    }

    /// <summary>
    /// 从房间阶段写入用户快照（服务器调用）。若日后需同步 UserId 等平台字段，可先在 <see cref="RoomPlayer"/> 增加 SyncVar 再在此赋值。
    /// </summary>
    [Server]
    public void ApplyRoomPlayer(RoomPlayer roomPlayer)
    {
        if (roomPlayer == null)
        {
            return;
        }

        PlayerName = roomPlayer.PlayerName;
        AvatarId = roomPlayer.AvatarId;
        UserDescription = roomPlayer.PlayerDescription != null ? roomPlayer.PlayerDescription : string.Empty;
        RefreshOverhead();
        NotifyUserProfileSynced();
    }

    /// <summary>用户展示名（与 <see cref="PlayerName"/> 相同，语义上强调「账号昵称」）。</summary>
    public string UserDisplayName => PlayerName;

    /// <summary>用户头像资源 Id，用于 <see cref="AvatarDataRepository.GetIconOrNull(int)"/> 等。</summary>
    public int UserPortraitAvatarId => AvatarId;

    /// <summary>
    /// 任一端用户相关 SyncVar 更新后触发，便于你后续挂 UI、换材质、播特效等。
    /// </summary>
    protected virtual void OnUserProfileForPresentation()
    {
    }

    [Command]
    public void CmdSendGameChat(string text)
    {
        if (RoomManager.Instance == null)
        {
            return;
        }

        RoomManager.Instance.ServerBroadcastGameChat(PlayerName, text);
    }

    [TargetRpc]
    public void TargetReceiveGameChat(NetworkConnection target, string senderName, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string safeSender = string.IsNullOrWhiteSpace(senderName) ? "未知玩家" : senderName.Trim();
        RoomEvents.RaiseGameChatMessage($"[{safeSender}] {text}");
    }

    void OnPlayerNameChanged(string oldValue, string newValue)
    {
        RefreshOverhead();
        OnUserProfileForPresentation();
    }

    void OnAvatarIdChanged(int oldValue, int newValue)
    {
        RefreshOverhead();
        OnUserProfileForPresentation();
    }

    void OnUserDescriptionChanged(string oldValue, string newValue)
    {
        OnUserProfileForPresentation();
    }

    void NotifyUserProfileSynced()
    {
        OnUserProfileForPresentation();
    }

    void RefreshOverhead()
    {
        if (overheadText != null)
        {
            overheadText.text = PlayerName;
        }
    }

    void ApplyLocalControlState()
    {
        if (playerMove == null)
        {
            playerMove = GetComponentInChildren<PlayerMove>(true);
        }

        if (playerMove != null)
        {
            // 仅本地拥有者允许输入移动，远端分身禁用控制。
            playerMove.UseOperation = isOwned;
        }
    }

    void BindSceneCamera()
    {
        CameraController sceneCamera = FindObjectOfType<CameraController>();
        if (sceneCamera == null)
        {
            return;
        }
        sceneCamera.SetTarget(playerMove.Head);

        if (playerMove != null)
        {
            playerMove.Camera = sceneCamera.transform;
        }
    }
}
