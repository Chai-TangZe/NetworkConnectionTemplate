using System;

[Serializable]
public class RoomData
{
    public string RoomId;
    public bool HasPassword;
    public string RoomPassword;
    public uint OwnerPlayerNetId;
    public string RoomName;
    public string MapName;
    public int CurrentPlayers;
    public int MaxPlayers;
    public bool IsPlaying;
}
