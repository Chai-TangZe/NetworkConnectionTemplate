using System;

/// <summary>
/// 账号级用户信息（与游戏内「角色」分离）。
/// </summary>
[Serializable]
public class UserData
{
    /// <summary>用户身份标识，持久化后不变；可对接 Steam/微信 OpenId 等。</summary>
    public string UserId;

    /// <summary>用户昵称（展示名）。</summary>
    public string DisplayName;

    /// <summary>用户个人说明（非角色设定）。</summary>
    public string Description;

    /// <summary>用户头像（与 AvatarDataDefinition.avatarId 对应）；0 为默认头像，未配置时亦视为 0。</summary>
    public int AvatarId;
}
