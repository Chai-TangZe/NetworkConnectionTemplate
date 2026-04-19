using System;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRoomListItem : MonoBehaviour
{
    [SerializeField] Text roomNameText;
    [SerializeField] Text mapNameText;
    [SerializeField] Text playerCountText;
    [SerializeField] Text stateText;
    [SerializeField] Button joinButton;

    RoomData roomData;
    Action<RoomData> onSelectClicked;

    public void SetData(RoomData room, Action<RoomData> onSelect)
    {
        roomData = room;
        onSelectClicked = onSelect;

        if (roomData == null)
        {
            SetText(roomNameText, "未知房间");
            SetText(mapNameText, "地图：--");
            SetText(playerCountText, "人数：--");
            SetText(stateText, "状态：--");
            SetJoinEnabled(false);
            return;
        }

        SetText(roomNameText, roomData.RoomName);
        SetText(mapNameText, $"地图：{MapDataRepository.GetDisplayName(roomData.MapName)}");
        SetText(playerCountText, $"人数：{roomData.CurrentPlayers}/{roomData.MaxPlayers}");
        SetText(stateText, roomData.IsPlaying ? "状态：进行中" : "状态：招募中");
        SetJoinEnabled(true);
    }

    public void OnClickJoin()
    {
        if (roomData == null || onSelectClicked == null)
        {
            return;
        }

        onSelectClicked.Invoke(roomData);
    }

    void SetJoinEnabled(bool enabled)
    {
        if (joinButton != null)
        {
            joinButton.interactable = enabled;
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
