using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AvatarDataRepository
{
    /// <summary>未配置头像、或 Id 无效时使用的默认头像（与 AvatarDataDefinition.avatarId 对应）。</summary>
    public const int DefaultAvatarId = 0;

    static readonly List<AvatarDataDefinition> loaded = new List<AvatarDataDefinition>();
    static bool loadedFlag;

    public static IReadOnlyList<AvatarDataDefinition> GetAll()
    {
        EnsureLoaded();
        return loaded;
    }

    public static AvatarDataDefinition GetByAvatarId(int avatarId)
    {
        EnsureLoaded();
        return loaded.FirstOrDefault(a => a != null && a.avatarId == avatarId);
    }

    /// <summary>
    /// 展示与存档用：负数、或未找到资源、或资源无 Sprite 时，视为使用 <see cref="DefaultAvatarId"/>。
    /// </summary>
    public static int ResolveEffectiveAvatarId(int avatarId)
    {
        EnsureLoaded();
        if (avatarId < 0)
        {
            return DefaultAvatarId;
        }

        AvatarDataDefinition def = GetByAvatarId(avatarId);
        if (def != null && def.icon != null)
        {
            return avatarId;
        }

        return DefaultAvatarId;
    }

    public static Sprite GetIconOrNull(int avatarId)
    {
        int id = ResolveEffectiveAvatarId(avatarId);
        AvatarDataDefinition def = GetByAvatarId(id);
        if (def != null && def.icon != null)
        {
            return def.icon;
        }

        return UiPlaceholderSprite.White();
    }

    static void EnsureLoaded()
    {
        if (loadedFlag)
        {
            return;
        }

        loadedFlag = true;
        loaded.Clear();
        loaded.AddRange(Resources.LoadAll<AvatarDataDefinition>("AvatarDefinitions"));
        loaded.RemoveAll(a => a == null);
        loaded.Sort((a, b) => a.avatarId.CompareTo(b.avatarId));

        if (loaded.Count > 0)
        {
            return;
        }

        // 无资源配置时的占位；打包后若仍无 Resources，至少给 icon 白块避免 Image 全透明不可见。
        Sprite placeholder = UiPlaceholderSprite.White();
        for (int i = 0; i < 3; i++)
        {
            AvatarDataDefinition fallback = ScriptableObject.CreateInstance<AvatarDataDefinition>();
            fallback.avatarId = i;
            fallback.displayName = $"头像{i}";
            fallback.icon = placeholder;
            loaded.Add(fallback);
        }
    }
}
