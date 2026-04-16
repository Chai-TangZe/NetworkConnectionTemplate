using Mirror;
using UnityEngine;

public class GamePlayer : NetworkBehaviour
{
    // 这些是从房间阶段带进游戏阶段的最小玩家信息。
    // 后面做头顶名字、角色外观初始化时可以直接复用。
    [Header("玩家信息")]
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string PlayerName;

    [SyncVar(hook = nameof(OnAvatarIdChanged))]
    public int AvatarId;
    
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

    [Server]
    public void ApplyRoomPlayer(RoomPlayer roomPlayer)
    {
        if (roomPlayer == null)
        {
            return;
        }

        PlayerName = roomPlayer.PlayerName;
        AvatarId = roomPlayer.AvatarId;
        RefreshOverhead();
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
    }

    void OnAvatarIdChanged(int oldValue, int newValue)
    {
        RefreshOverhead();
    }

    void RefreshOverhead()
    {
        if (overheadText != null)
        {
            overheadText.text = $"{PlayerName}\n形象:{AvatarId}";
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
