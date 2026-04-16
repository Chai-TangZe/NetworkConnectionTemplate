using System;
using System.Net;
using Mirror.Discovery;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Network/NetworkFramework LAN Discovery")]
public class LANMirrorDiscovery : NetworkDiscoveryBase<LANServerRequest, LANServerResponse>
{
    protected override LANServerResponse ProcessRequest(LANServerRequest request, IPEndPoint endpoint)
    {
        RoomManager roomManager = RoomManager.Instance;
        RoomData roomData = roomManager != null
            ? roomManager.GetCurrentRoomData()
            : new RoomData
            {
                RoomId = ServerId.ToString(),
                HasPassword = false,
                RoomPassword = null,
                RoomName = "局域网房间",
                MapName = "未知",
                CurrentPlayers = 0,
                MaxPlayers = 0,
                IsPlaying = false
            };

        try
        {
            return new LANServerResponse
            {
                serverId = ServerId,
                roomId = roomData.RoomId,
                hasPassword = roomData.HasPassword,
                roomPassword = roomData.RoomPassword,
                ownerPlayerNetId = roomData.OwnerPlayerNetId,
                uri = transport.ServerUri(),
                roomName = roomData.RoomName,
                mapName = roomData.MapName,
                currentPlayers = roomData.CurrentPlayers,
                maxPlayers = roomData.MaxPlayers,
                isPlaying = roomData.IsPlaying
            };
        }
        catch (NotImplementedException)
        {
            Debug.LogError($"传输层 {transport} 不支持局域网发现。");
            throw;
        }
    }

    protected override LANServerRequest GetRequest()
    {
        return new LANServerRequest();
    }

    protected override void ProcessResponse(LANServerResponse response, IPEndPoint endpoint)
    {
        response.EndPoint = endpoint;
        UriBuilder realUri = new UriBuilder(response.uri)
        {
            Host = response.EndPoint.Address.ToString()
        };
        response.uri = realUri.Uri;
        OnServerFound.Invoke(response);
    }
}
