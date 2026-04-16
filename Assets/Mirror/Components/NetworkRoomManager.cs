using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Mirror
{
    /// <summary>
    /// This is a specialized NetworkManager that includes a networked room.
    /// </summary>
    /// <remarks>
    /// <para>The room has slots that track the joined players, and a maximum player count that is enforced. It requires that the NetworkRoomPlayer component be on the room player objects.</para>
    /// <para>NetworkRoomManager is derived from NetworkManager, and so it implements many of the virtual functions provided by the NetworkManager class. To avoid accidentally replacing functionality of the NetworkRoomManager, there are new virtual functions on the NetworkRoomManager that begin with "OnRoom". These should be used on classes derived from NetworkRoomManager instead of the virtual functions on NetworkManager.</para>
    /// <para>The OnRoom*() functions have empty implementations on the NetworkRoomManager base class, so the base class functions do not have to be called.</para>
    /// </remarks>
    [AddComponentMenu("Network/Network Room Manager")]
    [HelpURL("https://mirror-networking.gitbook.io/docs/components/network-room-manager")]
    public class NetworkRoomManager : NetworkManager
    {
        public struct PendingPlayer
        {
            public NetworkConnectionToClient conn;
            public GameObject roomPlayer;
        }

        [Header("房间设置")]
        [FormerlySerializedAs("m_ShowRoomGUI")]
        [SerializeField]
        [Tooltip("此标志用于控制是否显示该房间的默认用户界面。")]
        public bool showRoomGUI = true;

        [FormerlySerializedAs("m_MinPlayers")]
        [SerializeField]
        [Tooltip("自动启动游戏所需的最低玩家人数")]
        public int minPlayers = 1;

        [FormerlySerializedAs("m_RoomPlayerPrefab")]
        [SerializeField]
        [Tooltip("用于“房间玩家”的预制件")]
        public NetworkRoomPlayer roomPlayerPrefab;

        /// <summary>
        /// 用于房间的场景设定。这与网络管理器的离线场景类似。
        /// </summary>
        [Scene]
        public string RoomScene;

        /// <summary>
        /// 用于在房间内进行游戏的场景设定。这与网络管理器的在线场景类似。
        /// </summary>
        [Scene]
        public string GameplayScene;

        /// <summary>
        /// 房间内玩家列表
        /// </summary>
        [FormerlySerializedAs("m_PendingPlayers")]
        public HashSet<PendingPlayer> pendingPlayers = new HashSet<PendingPlayer>();

        [Header("Diagnostics")]
        /// <summary>
        /// 当所有玩家都发送了“准备就绪”的消息时，此条件才成立。
        /// </summary>
        [Tooltip("表示所有玩家都已准备好开始游戏的诊断标志")]
        [FormerlySerializedAs("allPlayersReady")]
        [ReadOnly, SerializeField] bool _allPlayersReady;

        /// <summary>
        /// 这些插槽会记录进入房间的玩家信息。
        /// <para>玩家的“槽位编号”在整个游戏中是全局性的 - 适用于所有玩家。</para>
        /// </summary>
        [ReadOnly, Tooltip("List of Room Player objects")]
        public HashSet<NetworkRoomPlayer> roomSlots = new HashSet<NetworkRoomPlayer>();

        public bool allPlayersReady
        {
            get => _allPlayersReady;
            set
            {
                bool wasReady = _allPlayersReady;
                bool nowReady = value;

                if (wasReady != nowReady)
                {
                    _allPlayersReady = value;

                    if (nowReady)
                    {
                        OnRoomServerPlayersReady();
                    }
                    else
                    {
                        OnRoomServerPlayersNotReady();
                    }
                }
            }
        }

        public override void OnValidate()
        {
            base.OnValidate();

            // always <= maxConnections
            minPlayers = Mathf.Min(minPlayers, maxConnections);

            // always >= 0
            minPlayers = Mathf.Max(minPlayers, 0);

            if (roomPlayerPrefab != null)
            {
                NetworkIdentity identity = roomPlayerPrefab.GetComponent<NetworkIdentity>();
                if (identity == null)
                {
                    roomPlayerPrefab = null;
                    Debug.LogError("RoomPlayer prefab must have a NetworkIdentity component.");
                }
            }
        }

        void SceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
        {
            //Debug.Log($"NetworkRoom SceneLoadedForPlayer scene: {SceneManager.GetActiveScene().path} {conn}");

            if (Utils.IsSceneActive(RoomScene))
            {
                // cant be ready in room, add to ready list
                PendingPlayer pending;
                pending.conn = conn;
                pending.roomPlayer = roomPlayer;
                pendingPlayers.Add(pending);
                return;
            }

            GameObject gamePlayer = OnRoomServerCreateGamePlayer(conn, roomPlayer);
            if (gamePlayer == null)
            {
                // get start position from base class
                Transform startPos = GetStartPosition();
                gamePlayer = startPos != null
                    ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                    : Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            }

            if (!OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer))
                return;

            // 将“房间玩家”替换为“游戏玩家”
            NetworkServer.ReplacePlayerForConnection(conn, gamePlayer, ReplacePlayerOptions.KeepAuthority);
        }

        internal void CallOnClientEnterRoom()
        {
            OnRoomClientEnter();
            foreach (NetworkRoomPlayer player in roomSlots)
                if (player != null)
                {
                    player.OnClientEnterRoom();
                }
        }

        internal void CallOnClientExitRoom()
        {
            OnRoomClientExit();
            foreach (NetworkRoomPlayer player in roomSlots)
                if (player != null)
                {
                    player.OnClientExitRoom();
                }
        }

        /// <summary>
        /// “CheckReadyToBegin”会检查房间内所有玩家的状态，以确认他们的“readyToBegin”标志是否已设置。
        /// <para>如果所有玩家都已就绪，那么服务器就会从“房间场景”切换到“游戏场景”，从而正式开始游戏。这一操作是根据“NetworkRoomPlayer.CmdChangeReadyState”指令自动执行的。</para>
        /// </summary>
        public void CheckReadyToBegin()
        {
            if (!Utils.IsSceneActive(RoomScene))
                return;

            int numberOfReadyPlayers = NetworkServer.connections.Count(conn =>
                conn.Value != null &&
                conn.Value.identity != null &&
                conn.Value.identity.TryGetComponent(out NetworkRoomPlayer nrp) &&
                nrp.readyToBegin);

            bool enoughReadyPlayers = minPlayers <= 0 || numberOfReadyPlayers >= minPlayers;
            if (enoughReadyPlayers)
            {
                pendingPlayers.Clear();
                allPlayersReady = true;
            }
            else
                allPlayersReady = false;
        }

        #region server handlers

        /// <summary>
        /// 当有新客户端连接时，会向服务器发送此信号。
        /// <para>当客户端连接到服务器时，Unity 会在服务器端调用此函数。通过重写该函数，可以向网络管理器告知当客户端连接到服务器时应采取的操作。</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            // 无法加入正在进行的游戏
            if (!Utils.IsSceneActive(RoomScene))
            {
                Debug.Log($"不在房间场景中……断开连接 {conn}");
                conn.Disconnect();
                return;
            }

            base.OnServerConnect(conn);
            OnRoomServerConnect(conn);
        }

        /// <summary>
        /// 当客户端断开连接时，会向服务器发送此消息。
        /// <para>当客户端与服务器断开连接时，此操作会在服务器端触发。使用“覆盖”功能来决定在检测到断开连接的情况时应采取何种措施。</para>
        /// </summary>
        /// <param name="conn">来自客户端的连接。</param>
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (conn.identity != null)
            {
                NetworkRoomPlayer roomPlayer = conn.identity.GetComponent<NetworkRoomPlayer>();

                if (roomPlayer != null)
                    roomSlots.Remove(roomPlayer);

                foreach (NetworkIdentity clientOwnedObject in conn.owned)
                {
                    roomPlayer = clientOwnedObject.GetComponent<NetworkRoomPlayer>();
                    if (roomPlayer != null)
                        roomSlots.Remove(roomPlayer);
                }
            }

            allPlayersReady = false;

            foreach (NetworkRoomPlayer player in roomSlots)
            {
                if (player != null)
                    player.GetComponent<NetworkRoomPlayer>().readyToBegin = false;
            }

            if (Utils.IsSceneActive(RoomScene))
                RecalculateRoomPlayerIndices();

            OnRoomServerDisconnect(conn);
            base.OnServerDisconnect(conn);

            // 如果当前处于无界面模式且没有玩家连接，则重新启动服务器。
            // 这将使服务器进入离线模式，在此模式下自动启动功能将会运行。
            if (Utils.IsHeadless() && numPlayers < 1)
                StopServer();
        }

        // 在玩家按轮次分配至实例以及分数排序的过程中所使用的顺序索引
        public int clientIndex;

        /// <summary>
        /// 当客户端准备好时，会向服务器发出此调用。
        /// <para>此函数的默认实现会调用 NetworkServer.SetClientReady() 来继续网络设置流程。</para>
        /// </summary>
        /// <param name="conn">来自客户端的连接。</param>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            //Debug.Log($"NetworkRoomManager OnServerReady {conn}");
            base.OnServerReady(conn);

            if (conn != null && conn.identity != null)
            {
                GameObject roomPlayer = conn.identity.gameObject;

                // 如果为空值或者不是房间玩家，则不要替换它
                if (roomPlayer != null && roomPlayer.GetComponent<NetworkRoomPlayer>() != null)
                    SceneLoadedForPlayer(conn, roomPlayer);
            }
        }

        /// <summary>
        /// 当客户端使用 NetworkClient.AddPlayer 方法添加新玩家时，会触发此回调函数。
        /// <para>此函数的默认实现是从“玩家预制体”创建一个新的玩家对象。</para>
        /// </summary>
        /// <param name="conn">来自客户端的连接。</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // 在添加玩家之前先增加索引值，这样第一个玩家的索引值就会是 1 了。
            clientIndex++;

            if (Utils.IsSceneActive(RoomScene))
            {
                allPlayersReady = false;

                //Debug.Log("NetworkRoomManager.OnServerAddPlayer playerPrefab: {roomPlayerPrefab.name}");

                GameObject newRoomGameObject = OnRoomServerCreateRoomPlayer(conn);
                if (newRoomGameObject == null)
                    newRoomGameObject = Instantiate(roomPlayerPrefab.gameObject, Vector3.zero, Quaternion.identity);

                NetworkServer.AddPlayerForConnection(conn, newRoomGameObject);
            }
            else
            {
                // 晚加入的玩家不被支持……应该在“OnServerDisconnect”事件中将其踢出游戏。
                Debug.Log($"不在房间场景中……断开连接 {conn}");
                conn.Disconnect();
            }
        }

        [Server]
        public void RecalculateRoomPlayerIndices()
        {
            if (roomSlots.Count > 0)
            {
                int i = 0;
                foreach (NetworkRoomPlayer player in roomSlots)
                    player.index = i++;
            }
        }

        /// <summary>
        /// 这会导致服务器切换场景，并设置网络场景名称。
        /// <para>连接到此服务器的客户端将自动切换至此场景。如果设置了“在线场景”或“离线场景”，则会自动执行此操作，但也可以通过用户代码在游戏进行过程中再次调用此功能来切换场景。
        /// 此操作会自动将客户端设置为“未准备好”状态。客户端必须再次调用 NetworkClient.Ready() 方法才能参与新的场景。</para>
        /// </summary>
        /// <param name="newSceneName"></param>
        public override void ServerChangeScene(string newSceneName)
        {
            if (newSceneName == RoomScene)
            {
                foreach (NetworkRoomPlayer roomPlayer in roomSlots)
                {
                    if (roomPlayer == null)
                        continue;

                    // 找到与此连接相关的游戏参与者对象，并将其销毁。
                    NetworkIdentity identity = roomPlayer.GetComponent<NetworkIdentity>();

                    if (NetworkServer.active)
                    {
                        // 重新添加房间对象
                        roomPlayer.GetComponent<NetworkRoomPlayer>().readyToBegin = false;
                        NetworkServer.ReplacePlayerForConnection(identity.connectionToClient, roomPlayer.gameObject, ReplacePlayerOptions.KeepAuthority);
                    }
                }

                allPlayersReady = false;
            }

            base.ServerChangeScene(newSceneName);
        }

        /// <summary>
        /// 当场景加载完成时（即由服务器发起加载操作时，通过调用 ServerChangeScene() 函数），会触发此回调函数。
        /// </summary>
        /// <param name="sceneName">新场景的名称。</param>
        public override void OnServerSceneChanged(string sceneName)
        {
            if (sceneName != RoomScene)
            {
                // call SceneLoadedForPlayer on any players that become ready while we were loading the scene.
                foreach (PendingPlayer pending in pendingPlayers)
                    SceneLoadedForPlayer(pending.conn, pending.roomPlayer);

                pendingPlayers.Clear();
            }

            OnRoomServerSceneChanged(sceneName);
        }

        /// <summary>
        /// 当服务器启动时（包括主机启动时）会调用此功能。
        /// <para>StartServer 函数具有多种签名，但它们都会导致此钩子函数被调用。</para>
        /// </summary>
        public override void OnStartServer()
        {
            if (string.IsNullOrWhiteSpace(RoomScene))
            {
                Debug.LogError("NetworkRoomManager RoomScene is empty. Set the RoomScene in the inspector for the NetworkRoomManager");
                return;
            }

            if (string.IsNullOrWhiteSpace(GameplayScene))
            {
                Debug.LogError("NetworkRoomManager PlayScene is empty. Set the PlayScene in the inspector for the NetworkRoomManager");
                return;
            }

            OnRoomStartServer();
        }

        /// <summary>
        /// 当主机启动时，此功能就会被启用。
        /// <para>StartHost 具有多重签名，但它们都会导致此钩子被调用。</para>
        /// </summary>
        public override void OnStartHost()
        {
            OnRoomStartHost();
        }

        /// <summary>
        /// 当服务器停止运行（包括主机停止运行的情况）时，会触发此操作。
        /// </summary>
        public override void OnStopServer()
        {
            roomSlots.Clear();
            OnRoomStopServer();
        }

        /// <summary>
        /// 当主机停止运行时，就会触发此操作。
        /// </summary>
        public override void OnStopHost()
        {
            OnRoomStopHost();
        }

        #endregion

        #region client handlers

        /// <summary>
        /// 当客户端启动时，此操作就会被触发。
        /// </summary>
        public override void OnStartClient()
        {
            if (roomPlayerPrefab == null || roomPlayerPrefab.gameObject == null)
                Debug.LogError("网络房间管理器：未注册房间玩家预制体。请添加一个房间玩家预制体。");
            else
                NetworkClient.RegisterPrefab(roomPlayerPrefab.gameObject);

            if (playerPrefab == null)
                Debug.LogError("网络房间管理器：未注册游戏玩家预制体。请添加一个游戏玩家预制体。");

            OnRoomStartClient();
        }

        /// <summary>
        /// 当与服务器连接成功后，会向客户端发出此调用。
        /// <para>此函数的默认实现会将客户端设置为已就绪状态，并添加一个玩家。请重写该函数，以规定客户端连接时应发生的具体操作。</para>
        /// </summary>
        public override void OnClientConnect()
        {
            OnRoomClientConnect();
            base.OnClientConnect();
        }

        /// <summary>
        /// 当与服务器断开连接时会向客户端发送通知。
        /// <para>当客户端与服务器断开连接时，会调用此函数。请重写此函数以决定客户端断开连接时应发生什么情况。</para>
        /// </summary>
        public override void OnClientDisconnect()
        {
            OnRoomClientDisconnect();
            base.OnClientDisconnect();
        }

        /// <summary>
        /// 当客户端停止运行时，就会调用此函数。
        /// </summary>
        public override void OnStopClient()
        {
            OnRoomStopClient();
            CallOnClientExitRoom();
            roomSlots.Clear();
        }

        /// <summary>
        /// 在场景加载完成后会向客户端发出通知，该通知是在服务器启动场景加载操作的情况下产生的。
        /// <para>场景切换可能会导致玩家对象被销毁。在网络管理器中，OnClientSceneChanged 的默认实现是：如果当前没有玩家对象，则为该连接添加一个玩家对象。</para>
        /// </summary>
        public override void OnClientSceneChanged()
        {
            if (Utils.IsSceneActive(RoomScene))
            {
                if (NetworkClient.isConnected)
                    CallOnClientEnterRoom();
            }
            else
                CallOnClientExitRoom();

            base.OnClientSceneChanged();
            OnRoomClientSceneChanged();
        }

        #endregion

        #region room server virtuals

        /// <summary>
        /// This is called on the host when a host is started.
        /// </summary>
        public virtual void OnRoomStartHost() {}

        /// <summary>
        /// This is called on the host when the host is stopped.
        /// </summary>
        public virtual void OnRoomStopHost() {}

        /// <summary>
        /// This is called on the server when the server is started - including when a host is started.
        /// </summary>
        public virtual void OnRoomStartServer() {}

        /// <summary>
        /// This is called on the server when the server is started - including when a host is stopped.
        /// </summary>
        public virtual void OnRoomStopServer() {}

        /// <summary>
        /// This is called on the server when a new client connects to the server.
        /// </summary>
        /// <param name="conn">The new connection.</param>
        public virtual void OnRoomServerConnect(NetworkConnectionToClient conn) {}

        /// <summary>
        /// This is called on the server when a client disconnects.
        /// </summary>
        /// <param name="conn">The connection that disconnected.</param>
        public virtual void OnRoomServerDisconnect(NetworkConnectionToClient conn) {}

        /// <summary>
        /// This is called on the server when a networked scene finishes loading.
        /// </summary>
        /// <param name="sceneName">Name of the new scene.</param>
        public virtual void OnRoomServerSceneChanged(string sceneName) {}

        /// <summary>
        /// This allows customization of the creation of the room-player object on the server.
        /// <para>By default the roomPlayerPrefab is used to create the room-player, but this function allows that behaviour to be customized.</para>
        /// </summary>
        /// <param name="conn">The connection the player object is for.</param>
        /// <returns>The new room-player object.</returns>
        public virtual GameObject OnRoomServerCreateRoomPlayer(NetworkConnectionToClient conn)
        {
            return null;
        }

        /// <summary>
        /// This allows customization of the creation of the GamePlayer object on the server.
        /// <para>By default the gamePlayerPrefab is used to create the game-player, but this function allows that behaviour to be customized. The object returned from the function will be used to replace the room-player on the connection.</para>
        /// </summary>
        /// <param name="conn">The connection the player object is for.</param>
        /// <param name="roomPlayer">The room player object for this connection.</param>
        /// <returns>A new GamePlayer object.</returns>
        public virtual GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
        {
            return null;
        }

        /// <summary>
        /// This allows customization of the creation of the GamePlayer object on the server.
        /// <para>This is only called for subsequent GamePlay scenes after the first one.</para>
        /// <para>See <see cref="OnRoomServerCreateGamePlayer(NetworkConnectionToClient, GameObject)">OnRoomServerCreateGamePlayer(NetworkConnection, GameObject)</see> to customize the player object for the initial GamePlay scene.</para>
        /// </summary>
        /// <param name="conn">The connection the player object is for.</param>
        public virtual void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
        }

        // for users to apply settings from their room player object to their in-game player object
        /// <summary>
        /// This is called on the server when it is told that a client has finished switching from the room scene to a game player scene.
        /// <para>When switching from the room, the room-player is replaced with a game-player object. This callback function gives an opportunity to apply state from the room-player to the game-player object.</para>
        /// </summary>
        /// <param name="conn">The connection of the player</param>
        /// <param name="roomPlayer">The room player object.</param>
        /// <param name="gamePlayer">The game player object.</param>
        /// <returns>False to not allow this player to replace the room player.</returns>
        public virtual bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            return true;
        }

        /// <summary>
        /// This is called on server from NetworkRoomPlayer.CmdChangeReadyState when client indicates change in Ready status.
        /// </summary>
        public virtual void ReadyStatusChanged()
        {
            int CurrentPlayers = 0;
            int ReadyPlayers = 0;

            foreach (NetworkRoomPlayer item in roomSlots)
            {
                if (item != null)
                {
                    CurrentPlayers++;
                    if (item.readyToBegin)
                        ReadyPlayers++;
                }
            }

            if (CurrentPlayers == ReadyPlayers)
                CheckReadyToBegin();
            else
                allPlayersReady = false;
        }

        /// <summary>
        /// This is called on the server when all the players in the room are ready.
        /// <para>The default implementation of this function uses ServerChangeScene() to switch to the game player scene. By implementing this callback you can customize what happens when all the players in the room are ready, such as adding a countdown or a confirmation for a group leader.</para>
        /// </summary>
        public virtual void OnRoomServerPlayersReady()
        {
            // all players are readyToBegin, start the game
            ServerChangeScene(GameplayScene);
        }

        /// <summary>
        /// This is called on the server when CheckReadyToBegin finds that players are not ready
        /// <para>May be called multiple times while not ready players are joining</para>
        /// </summary>
        public virtual void OnRoomServerPlayersNotReady() {}

        #endregion

        #region room client virtuals

        /// <summary>
        /// This is a hook to allow custom behaviour when the game client enters the room.
        /// </summary>
        public virtual void OnRoomClientEnter() {}

        /// <summary>
        /// This is a hook to allow custom behaviour when the game client exits the room.
        /// </summary>
        public virtual void OnRoomClientExit() {}

        /// <summary>
        /// This is called on the client when it connects to server.
        /// </summary>
        public virtual void OnRoomClientConnect() {}

        /// <summary>
        /// This is called on the client when disconnected from a server.
        /// </summary>
        public virtual void OnRoomClientDisconnect() {}

        /// <summary>
        /// This is called on the client when a client is started.
        /// </summary>
        public virtual void OnRoomStartClient() {}

        /// <summary>
        /// This is called on the client when the client stops.
        /// </summary>
        public virtual void OnRoomStopClient() {}

        /// <summary>
        /// This is called on the client when the client is finished loading a new networked scene.
        /// </summary>
        public virtual void OnRoomClientSceneChanged() {}

        #endregion

        #region optional UI

        /// <summary>
        /// 这样使得派生类就可以自行实现相关功能了。
        /// </summary>
        public virtual void OnGUI()
        {
            if (!showRoomGUI)
                return;

            if (NetworkServer.active && Utils.IsSceneActive(GameplayScene))
            {
                GUILayout.BeginArea(new Rect(Screen.width - 150f, 10f, 140f, 30f));
                if (GUILayout.Button("返回房间"))
                    ServerChangeScene(RoomScene);
                GUILayout.EndArea();
            }

            if (Utils.IsSceneActive(RoomScene))
                GUI.Box(new Rect(10f, 180f, 520f, 150f), "玩家");
        }

        #endregion
    }
}
