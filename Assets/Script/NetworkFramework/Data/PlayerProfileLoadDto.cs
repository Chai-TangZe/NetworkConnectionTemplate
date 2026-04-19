using System;
using System.Collections.Generic;

/// <summary>
/// 仅用于反序列化存档：旧版曾在 Character 上存 AvatarId，需合并到 <see cref="UserData.AvatarId"/>。
/// </summary>
[Serializable]
public class PlayerProfileLoadDto
{
    public int Version;
    public UserData User = new UserData();
    public CharacterDataLoadDto Character = new CharacterDataLoadDto();
}

[Serializable]
public class CharacterDataLoadDto
{
    public string CharacterId;
    /// <summary>旧版误存在角色上的头像 Id，加载后合并到 User.AvatarId。</summary>
    public int AvatarId;
    public List<PlayerPartData> Parts = new List<PlayerPartData>();
}
