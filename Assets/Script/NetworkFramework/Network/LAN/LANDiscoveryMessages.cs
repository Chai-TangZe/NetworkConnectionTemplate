using System;
using System.Net;
using Mirror;

public struct LANServerRequest : NetworkMessage
{
}

public struct LANServerResponse : NetworkMessage
{
    public long serverId;
    public string roomId;
    public bool hasPassword;
    public string roomPassword;
    public uint ownerPlayerNetId;
    public Uri uri;
    public string roomName;
    public string mapName;
    public int currentPlayers;
    public int maxPlayers;
    public bool isPlaying;
    public IPEndPoint EndPoint { get; set; }
}
