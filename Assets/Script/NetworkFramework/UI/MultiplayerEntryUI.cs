using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerEntryUI : MonoBehaviour
{
    [Header("场景跳转")]
    [SerializeField] string multiplayerLobbySceneName = "MultiplayerLobby";
    [SerializeField] GameObject networkSessionRootPrefab;

    [Header("进入大厅前等待")]
    [SerializeField]
    [Tooltip("等待 NetworkSessionRoot 等初始化后再 LoadScene，减少大厅首帧未就绪。")]
    int waitFramesBeforeLobbyScene = 2;

    public void OnClickSelectLAN()
    {
        SetModeAndOpenLobby(NetworkModeType.LAN);
    }

    public void OnClickSelectOnline()
    {
        SetModeAndOpenLobby(NetworkModeType.Online);
    }
    void SetModeAndOpenLobby(NetworkModeType mode)
    {
        NetworkSessionLifecycle lifecycle = NetworkSessionLifecycle.EnsureInstance(networkSessionRootPrefab);
        lifecycle.EnsureSessionCreated();

        PlayerProfileContext profileContext = PlayerProfileContext.EnsureInstance();
        profileContext.SetNetworkMode(mode);
        profileContext.EnsureDefaults();

        if (!string.IsNullOrWhiteSpace(multiplayerLobbySceneName))
        {
            StartCoroutine(EnterLobbyWhenSessionPrepared(multiplayerLobbySceneName));
        }
    }

    IEnumerator EnterLobbyWhenSessionPrepared(string sceneName)
    {
        for (int i = 0; i < Mathf.Max(0, waitFramesBeforeLobbyScene); i++)
        {
            yield return null;
        }

        int guard = 60;
        while (guard-- > 0 && NetworkSessionRoot.Instance == null)
        {
            yield return null;
        }

        if (NetworkSessionRoot.Instance == null)
        {
            Debug.LogWarning("[联网入口] 联机会话根未在预期时间内就绪，仍将尝试加载大厅场景。");
        }

        SceneManager.LoadScene(sceneName);
    }
}
