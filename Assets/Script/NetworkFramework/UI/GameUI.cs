using Mirror;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("游戏信息")]
    [SerializeField] Text playerInfoText;
    [SerializeField] Text roomInfoText;
    [SerializeField] Text timerText;
    [SerializeField] Text gameChatText;
    [SerializeField] InputField gameChatInput;
    [SerializeField] int maxChatLines = 20;

    readonly System.Collections.Generic.Queue<string> gameChatLines = new System.Collections.Generic.Queue<string>();
    bool missingRoomManagerLogged;

    void OnEnable()
    {
        gameChatLines.Clear();
        if (gameChatText != null)
        {
            gameChatText.text = string.Empty;
        }

        RoomEvents.OnGameChatMessage += OnGameChatMessage;
    }

    void OnDisable()
    {
        RoomEvents.OnGameChatMessage -= OnGameChatMessage;
    }

    void Update()
    {
        RefreshUI();
    }

    // 当前游戏界面只展示最核心信息，后续可扩展聊天、积分和结算。
    void RefreshUI()
    {
        RoomManager roomManager = RoomManager.Instance;
        GamePlayer localPlayer = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<GamePlayer>()
            : null;

        bool hasSyncedRoomInfo = RoomPlayer.HasSyncedRoomInfo;
        if (roomManager != null || hasSyncedRoomInfo)
        {
            missingRoomManagerLogged = false;
            string roomName = hasSyncedRoomInfo ? RoomPlayer.SyncedRoomName : roomManager.RoomName;
            string mapName = hasSyncedRoomInfo ? RoomPlayer.SyncedMapName : roomManager.MapName;
            double syncedGameEndTime = hasSyncedRoomInfo ? RoomPlayer.SyncedGameEndTime : roomManager.GameEndTime;
            bool syncedIsPlaying = hasSyncedRoomInfo ? RoomPlayer.SyncedIsPlaying : roomManager.IsPlaying;
            if (roomInfoText != null)
            {
                roomInfoText.text = $"房间：{roomName} | 地图：{MapDataRepository.GetDisplayName(mapName)}";
            }

            if (timerText != null)
            {
                float remainingSeconds = syncedIsPlaying && syncedGameEndTime > 0d
                    ? Mathf.Max(0f, (float)(syncedGameEndTime - NetworkTime.time))
                    : 0f;
                timerText.text = $"剩余时间：{Mathf.CeilToInt(remainingSeconds)}秒";
            }
        }
        else
        {
            if (!missingRoomManagerLogged)
            {
                Debug.LogWarning("[游戏界面] 未找到 RoomManager，等待网络会话初始化。");
                missingRoomManagerLogged = true;
            }

            SetText(roomInfoText, string.Empty);
            SetText(timerText, string.Empty);
        }

        if (playerInfoText != null)
        {
            int activePlayers = NetworkClient.spawned.Values.Count(identity => identity != null && identity.GetComponent<GamePlayer>() != null);
            playerInfoText.text = localPlayer != null
                ? $"玩家：{localPlayer.PlayerName} | 形象：{localPlayer.AvatarId} | 在线：{activePlayers}"
                : $"在线：{activePlayers}";
        }
    }

    public void OnClickSendGameChat()
    {
        GamePlayer localPlayer = NetworkClient.localPlayer != null
            ? NetworkClient.localPlayer.GetComponent<GamePlayer>()
            : null;
        if (localPlayer == null || gameChatInput == null)
        {
            return;
        }

        string content = gameChatInput.text;
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        localPlayer.CmdSendGameChat(content);
        gameChatInput.text = string.Empty;
    }

    void OnGameChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        gameChatLines.Enqueue(message);
        while (gameChatLines.Count > Mathf.Max(1, maxChatLines))
        {
            gameChatLines.Dequeue();
        }

        if (gameChatText != null)
        {
            gameChatText.text = string.Join("\n", gameChatLines.ToArray());
        }
    }

    void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
