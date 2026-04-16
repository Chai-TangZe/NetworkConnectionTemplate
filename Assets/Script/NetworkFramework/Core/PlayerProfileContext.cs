using UnityEngine;

public class PlayerProfileContext : MonoBehaviour
{
    public static PlayerProfileContext Instance { get; private set; }

    const string PlayerDataKey = "NetworkFramework.PlayerData";

    public NetworkModeType SelectedMode { get; private set; } = NetworkModeType.LAN;
    public PlayerData CurrentProfile { get; private set; } = new PlayerData
    {
        PlayerName = "玩家",
        AvatarId = 0
    };

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
        EnsureDefaultProfile();
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

    public void UpdateProfile(PlayerData data)
    {
        if (data == null)
        {
            return;
        }

        CurrentProfile = data;
        EnsureDefaultProfile();
    }

    public void SaveProfile()
    {
        string json = JsonUtility.ToJson(CurrentProfile);
        PlayerPrefs.SetString(PlayerDataKey, json);
        PlayerPrefs.Save();
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

        PlayerData loaded = JsonUtility.FromJson<PlayerData>(json);
        if (loaded != null)
        {
            CurrentProfile = loaded;
        }

        EnsureDefaultProfile();
    }

    public void EnsureDefaultProfile()
    {
        if (CurrentProfile == null)
        {
            CurrentProfile = new PlayerData();
        }

        if (string.IsNullOrWhiteSpace(CurrentProfile.PlayerName))
        {
            CurrentProfile.PlayerName = "玩家";
        }

        if (CurrentProfile.AvatarId < 0)
        {
            CurrentProfile.AvatarId = 0;
        }

        if (CurrentProfile.Parts == null)
        {
            CurrentProfile.Parts = new System.Collections.Generic.List<PlayerPartData>();
        }
    }
}
