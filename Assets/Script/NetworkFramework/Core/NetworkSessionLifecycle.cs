using Mirror;
using UnityEngine;

public class NetworkSessionLifecycle : MonoBehaviour
{
    public static NetworkSessionLifecycle Instance { get; private set; }

    [Header("会话根预制体")]
    [SerializeField] GameObject networkSessionRootPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static NetworkSessionLifecycle EnsureInstance(GameObject sessionRootPrefab)
    {
        if (Instance != null)
        {
            if (sessionRootPrefab != null)
            {
                Instance.networkSessionRootPrefab = sessionRootPrefab;
            }

            return Instance;
        }

        GameObject holder = new GameObject("NetworkSessionLifecycle");
        NetworkSessionLifecycle lifecycle = holder.AddComponent<NetworkSessionLifecycle>();
        lifecycle.networkSessionRootPrefab = sessionRootPrefab;
        return lifecycle;
    }

    public void EnsureSessionCreated()
    {
        if (NetworkSessionRoot.Instance != null)
        {
            return;
        }

        if (networkSessionRootPrefab == null)
        {
            Debug.LogError("[联机会话] 未配置 networkSessionRootPrefab。");
            return;
        }

        Instantiate(networkSessionRootPrefab);
    }

    /// <summary>
    /// 仅停止 Mirror 主机/客户端，不销毁会话根与角色数据；从房间/游戏返回大厅以便再次建房、加入时需要先调用。
    /// </summary>
    public void StopMirrorNetworking()
    {
        if (NetworkManager.singleton == null)
        {
            return;
        }

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

    public void DestroySession()
    {
        StopMirrorNetworking();

        if (NetworkSessionRoot.Instance != null)
        {
            Destroy(NetworkSessionRoot.Instance.gameObject);
        }

        if (PlayerProfileContext.Instance != null)
        {
            Destroy(PlayerProfileContext.Instance.gameObject);
        }

        Destroy(gameObject);
    }
}
