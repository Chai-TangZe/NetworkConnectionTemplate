using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public string PlayerName;
    public int AvatarId;
    public List<PlayerPartData> Parts = new List<PlayerPartData>();
}

[Serializable]
public class PlayerPartData
{
    public string Slot;
    public string ItemId;
}
