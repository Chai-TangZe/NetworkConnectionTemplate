using System;
using System.Collections.Generic;
using UnityEngine;

public class OnlineRoomService : MonoBehaviour, IRoomService
{
    [SerializeField] bool autoRefreshOnEnable = true;
    readonly List<RoomData> cachedRooms = new List<RoomData>();

    public bool IsRefreshing { get; private set; }
    public bool IsWaitingForRoom => false;
    public event Action<IReadOnlyList<RoomData>> OnRoomListUpdated;
    public event Action<RoomJoinResult> OnJoinStatusChanged;

    void OnEnable()
    {
        if (autoRefreshOnEnable)
        {
            RefreshRooms();
        }
    }

    public void RefreshRooms()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;
        Invoke(nameof(FinishRefresh), 0.3f);
    }

    public RoomJoinResult CreateRoom(string roomName, string mapName, int maxPlayers, string roomId = null, string roomPassword = null)
    {
        RoomJoinResult result = RoomJoinResult.Fail(RoomJoinResultCode.Unsupported, "当前版本未实现广域网建房。");
        OnJoinStatusChanged?.Invoke(result);
        return result;
    }

    public RoomJoinResult JoinRoom(string roomId, string roomPassword = null)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return RoomJoinResult.Fail(RoomJoinResultCode.InvalidRoomId, "加入失败：房间ID无效。");
        }

        Debug.Log($"[在线房间服务] 尝试加入房间：{roomId}");
        RoomJoinResult result = RoomJoinResult.Fail(RoomJoinResultCode.Unsupported, "当前版本未实现广域网加入。");
        OnJoinStatusChanged?.Invoke(result);
        return result;
    }

    public void CancelWaitingJoin()
    {
    }

    void FinishRefresh()
    {
        cachedRooms.Clear();
        IsRefreshing = false;
        OnRoomListUpdated?.Invoke(cachedRooms);
    }
}
