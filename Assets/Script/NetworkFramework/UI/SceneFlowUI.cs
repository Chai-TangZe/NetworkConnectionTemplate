using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowUI : MonoBehaviour
{    [Header("场景配置")]
    [SerializeField] string homeSceneName = "Home";
    [SerializeField] string networkSelectSceneName = "NetworkSelect";
    [SerializeField] string multiplayerLobbySceneName = "MultiplayerLobby";
    [SerializeField] string roomSceneName = "Room";

    public void OnClickGoToNetworkSelect()
    {
        LoadScene(networkSelectSceneName);
    }

    public void OnClickGoToNetworkSelectAndShutdownSession()
    {
        if (NetworkSessionLifecycle.Instance != null)
        {
            NetworkSessionLifecycle.Instance.DestroySession();
        }

        LoadScene(networkSelectSceneName);
    }

    public void OnClickGoToMultiplayerLobby()
    {
        StartCoroutine(GoToMultiplayerLobbyRoutine());
    }

    /// <summary>
    /// 从房间/游戏返回大厅时必须先断开当前联机会话，否则 LANRoomService.CreateRoom 会认为「已在会话中」而拒绝再次建房。
    /// </summary>
    IEnumerator GoToMultiplayerLobbyRoutine()
    {
        if (NetworkSessionLifecycle.Instance != null)
        {
            NetworkSessionLifecycle.Instance.StopMirrorNetworking();
        }
        else if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopServer();
            }
        }

        yield return null;
        LoadScene(multiplayerLobbySceneName);
    }

    public void OnClickGoToRoomScene()
    {
        StartCoroutine(ExitGameRoutine());
    }

    /// <summary>
    /// 游戏内“退出”按钮：
    /// - 房主(Host/Server)退出：ServerChangeScene(Room)，所有客户端一起回房间。
    /// - 普通客户端退出：断开连接并回大厅。
    /// </summary>
    IEnumerator ExitGameRoutine()
    {
        RoomManager roomManager = RoomManager.Instance;
        if (NetworkServer.active && roomManager != null && !string.IsNullOrWhiteSpace(roomManager.RoomScene))
        {
            roomManager.ServerChangeScene(roomManager.RoomScene);
            yield break;
        }

        if (!NetworkServer.active && NetworkClient.isConnected && NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
            yield return null;
            LoadScene(multiplayerLobbySceneName);
            yield break;
        }

        // 离线兜底：无会话时回大厅
        LoadScene(multiplayerLobbySceneName);
    }

    public void OnClickGoToHome()
    {
        LoadScene(homeSceneName);
    }

    public void OnClickGoToHomeAndShutdownSession()
    {
        if (NetworkSessionLifecycle.Instance != null)
        {
            NetworkSessionLifecycle.Instance.DestroySession();
        }

        LoadScene(homeSceneName);
    }

    void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[场景跳转] 目标场景名为空。");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
