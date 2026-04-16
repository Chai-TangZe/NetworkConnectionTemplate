using System;
using System.Collections.Generic;

public interface IRoomService
{
    bool IsRefreshing { get; }
    bool IsWaitingForRoom { get; }
    event Action<IReadOnlyList<RoomData>> OnRoomListUpdated;
    event Action<RoomJoinResult> OnJoinStatusChanged;
    void RefreshRooms();
    RoomJoinResult CreateRoom(string roomName, string mapName, int maxPlayers, string roomId = null, string roomPassword = null);
    RoomJoinResult JoinRoom(string roomId, string roomPassword = null);
    void CancelWaitingJoin();
}
