using UnityEngine;

public class NetworkSessionRoot : MonoBehaviour
{
    public static NetworkSessionRoot Instance { get; private set; }

    [Header("核心服务")]
    [SerializeField] MonoBehaviour lanRoomServiceBehaviour;
    [SerializeField] MonoBehaviour onlineRoomServiceBehaviour;

    public IRoomService LanRoomService => lanRoomServiceBehaviour as IRoomService;
    public IRoomService OnlineRoomService => onlineRoomServiceBehaviour as IRoomService;

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
}
