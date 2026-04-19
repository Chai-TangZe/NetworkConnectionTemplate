using System;

/// <summary>
/// 本地持久化的根结构：用户 + 当前使用的角色（单角色阶段）。
/// </summary>
[Serializable]
public class PlayerProfileRoot
{
    public int Version = 2;
    public UserData User = new UserData();
    public CharacterData Character = new CharacterData();
}
