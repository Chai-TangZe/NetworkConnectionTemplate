using UnityEngine;

public interface ILANRoomDataMapper
{
    RoomData Map(LANServerResponse response);
}

public class LANRoomDataMapper : ILANRoomDataMapper
{
    public RoomData Map(LANServerResponse response)
    {
        string roomId = string.IsNullOrWhiteSpace(response.roomId) ? response.serverId.ToString() : response.roomId.Trim();
        return new RoomData
        {
            RoomId = roomId,
            HasPassword = response.hasPassword,
            RoomPassword = response.roomPassword,
            OwnerPlayerNetId = response.ownerPlayerNetId,
            RoomName = string.IsNullOrWhiteSpace(response.roomName) ? $"局域网房间 {response.EndPoint.Address}" : response.roomName,
            MapName = string.IsNullOrWhiteSpace(response.mapName) ? "未知" : response.mapName,
            CurrentPlayers = Mathf.Max(0, response.currentPlayers),
            MaxPlayers = Mathf.Max(0, response.maxPlayers),
            IsPlaying = response.isPlaying
        };
    }
}
