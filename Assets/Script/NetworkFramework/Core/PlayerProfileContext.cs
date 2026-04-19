using System;
using UnityEngine;

public class PlayerProfileContext : MonoBehaviour
{
    public static PlayerProfileContext Instance { get; private set; }

    const string PlayerDataKey = "NetworkFramework.PlayerData";

    public event Action ProfileChanged;

    public NetworkModeType SelectedMode { get; private set; } = NetworkModeType.LAN;

    public UserData User { get; private set; } = new UserData();
    public CharacterData Character { get; private set; } = new CharacterData();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadProfile();
        string idBeforeMigration = User != null ? User.UserId : null;
        EnsureDefaults();
        if (string.IsNullOrWhiteSpace(idBeforeMigration) && User != null && !string.IsNullOrWhiteSpace(User.UserId))
        {
            SaveProfile();
        }
    }

    public static PlayerProfileContext EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject holder = new GameObject("PlayerProfileContext");
        return holder.AddComponent<PlayerProfileContext>();
    }

    public void SetNetworkMode(NetworkModeType mode)
    {
        SelectedMode = mode;
    }

    /// <summary>
    /// 预留：接入 Steam / 微信等平台时写入用户展示信息（头像属于用户，非角色）。
    /// </summary>
    public void ApplyExternalPlatformProfile(string platformUserId, string displayName, string description, int avatarId)
    {
        EnsureDefaults();
        if (!string.IsNullOrWhiteSpace(platformUserId))
        {
            User.UserId = platformUserId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            User.DisplayName = displayName.Trim();
        }

        User.Description = description ?? string.Empty;
        User.AvatarId = AvatarDataRepository.ResolveEffectiveAvatarId(Mathf.Max(0, avatarId));
        SaveProfile();
    }

    public void UpdateProfile(UserData user, CharacterData character)
    {
        if (user != null)
        {
            User = user;
        }

        if (character != null)
        {
            Character = character;
        }

        EnsureDefaults();
        ProfileChanged?.Invoke();
    }

    public void SaveProfile()
    {
        var root = new PlayerProfileRoot
        {
            Version = 2,
            User = User,
            Character = Character
        };
        string json = JsonUtility.ToJson(root);
        PlayerPrefs.SetString(PlayerDataKey, json);
        PlayerPrefs.Save();
        ProfileChanged?.Invoke();
    }

    public void LoadProfile()
    {
        if (!PlayerPrefs.HasKey(PlayerDataKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(PlayerDataKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        // 旧版扁平存档只有 PlayerName，没有 User.DisplayName；先走迁移避免误解析成空 User。
        if (json.IndexOf("\"PlayerName\"", StringComparison.Ordinal) >= 0 &&
            json.IndexOf("\"DisplayName\"", StringComparison.Ordinal) < 0 &&
            TryMigrateLegacyV1(json))
        {
            return;
        }

        PlayerProfileLoadDto dto = null;
        try
        {
            dto = JsonUtility.FromJson<PlayerProfileLoadDto>(json);
        }
        catch
        {
            dto = null;
        }

        if (dto != null && dto.User != null)
        {
            User = dto.User;
            Character = new CharacterData
            {
                CharacterId = dto.Character != null ? dto.Character.CharacterId : null,
                Parts = dto.Character != null && dto.Character.Parts != null
                    ? dto.Character.Parts
                    : new System.Collections.Generic.List<PlayerPartData>()
            };

            // 旧版曾把头像写在 Character.AvatarId，合并到用户头像。
            if (User.AvatarId == 0 && dto.Character != null && dto.Character.AvatarId > 0)
            {
                User.AvatarId = dto.Character.AvatarId;
            }

            return;
        }

        TryMigrateLegacyV1(json);
    }

    [Serializable]
    class LegacyPlayerDataV1
    {
        public string PlayerId;
        public string PlayerName;
        public string Description;
        public int AvatarId;
        public System.Collections.Generic.List<PlayerPartData> Parts;
    }

    bool TryMigrateLegacyV1(string json)
    {
        LegacyPlayerDataV1 legacy = null;
        try
        {
            legacy = JsonUtility.FromJson<LegacyPlayerDataV1>(json);
        }
        catch
        {
            return false;
        }

        if (legacy == null)
        {
            return false;
        }

        User = new UserData
        {
            UserId = legacy.PlayerId,
            DisplayName = legacy.PlayerName,
            Description = legacy.Description ?? string.Empty,
            AvatarId = Mathf.Max(0, legacy.AvatarId)
        };

        Character = new CharacterData
        {
            CharacterId = Guid.NewGuid().ToString("N"),
            Parts = legacy.Parts ?? new System.Collections.Generic.List<PlayerPartData>()
        };

        SaveProfile();
        return true;
    }

    public void EnsureDefaults()
    {
        if (User == null)
        {
            User = new UserData();
        }

        if (Character == null)
        {
            Character = new CharacterData();
        }

        if (string.IsNullOrWhiteSpace(User.UserId))
        {
            User.UserId = Guid.NewGuid().ToString("N");
        }

        if (string.IsNullOrWhiteSpace(User.DisplayName))
        {
            User.DisplayName = $"用户{UnityEngine.Random.Range(100000000, 1000000000)}";
        }

        if (User.Description == null)
        {
            User.Description = string.Empty;
        }

        User.AvatarId = AvatarDataRepository.ResolveEffectiveAvatarId(User.AvatarId);

        if (string.IsNullOrWhiteSpace(Character.CharacterId))
        {
            Character.CharacterId = Guid.NewGuid().ToString("N");
        }

        if (Character.Parts == null)
        {
            Character.Parts = new System.Collections.Generic.List<PlayerPartData>();
        }
    }
}
