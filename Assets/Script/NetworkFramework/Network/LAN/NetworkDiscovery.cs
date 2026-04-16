using System.Collections.Generic;
using System;
using UnityEngine;

public class NetworkDiscovery : MonoBehaviour
{
    [Header("局域网发现")]
    [SerializeField] LANMirrorDiscovery mirrorDiscovery;
    [SerializeField] float discoveryWindowSeconds = 2f;

    readonly Dictionary<string, Uri> roomUriMap = new Dictionary<string, Uri>();
    readonly List<RoomData> discoveredRooms = new List<RoomData>();
    ILANRoomDataMapper roomDataMapper;
    bool advertisingServer;
    bool listeningServerResponses;

    public IReadOnlyList<RoomData> GetLatestRooms()
    {
        return discoveredRooms;
    }

    void OnEnable()
    {
        if (mirrorDiscovery == null)
        {
            mirrorDiscovery = GetComponent<LANMirrorDiscovery>();
        }

        roomDataMapper ??= new LANRoomDataMapper();
        RegisterServerFoundListener();
    }

    void OnDisable()
    {
        if (mirrorDiscovery != null)
        {
            mirrorDiscovery.StopDiscovery();
            mirrorDiscovery.OnServerFound.RemoveListener(OnServerFound);
        }

        listeningServerResponses = false;
        advertisingServer = false;
    }

    void Update()
    {
        if (mirrorDiscovery == null)
        {
            return;
        }

        if (Mirror.NetworkServer.active && !advertisingServer)
        {
            mirrorDiscovery.AdvertiseServer();
            advertisingServer = true;
            return;
        }

        if (!Mirror.NetworkServer.active && advertisingServer)
        {
            mirrorDiscovery.StopDiscovery();
            advertisingServer = false;
        }
    }

    public void RefreshDiscovery()
    {
        if (mirrorDiscovery == null)
        {
            Debug.LogWarning("[局域网发现] 未绑定 Mirror NetworkDiscovery 组件。");
            return;
        }

        RegisterServerFoundListener();
        roomUriMap.Clear();
        discoveredRooms.Clear();
        mirrorDiscovery.StartDiscovery();
        CancelInvoke(nameof(StopDiscovery));
        Invoke(nameof(StopDiscovery), Mathf.Max(0.5f, discoveryWindowSeconds));
    }

    public bool TryGetRoomUri(string roomId, out Uri uri)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            uri = null;
            return false;
        }

        return roomUriMap.TryGetValue(roomId, out uri);
    }

    public bool TryGetRoomData(string roomId, out RoomData roomData)
    {
        roomData = null;
        if (string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        roomData = discoveredRooms.Find(room => room != null && room.RoomId == roomId);
        return roomData != null;
    }

    void RegisterServerFoundListener()
    {
        if (mirrorDiscovery == null || listeningServerResponses)
        {
            return;
        }

        mirrorDiscovery.OnServerFound.AddListener(OnServerFound);
        listeningServerResponses = true;
    }

    void StopDiscovery()
    {
        if (mirrorDiscovery != null)
        {
            mirrorDiscovery.StopDiscovery();
        }
    }

    void OnServerFound(LANServerResponse response)
    {
        RoomData mappedRoom = roomDataMapper != null ? roomDataMapper.Map(response) : null;
        if (mappedRoom == null)
        {
            return;
        }

        string roomId = string.IsNullOrWhiteSpace(mappedRoom.RoomId) ? response.serverId.ToString() : mappedRoom.RoomId.Trim();
        mappedRoom.RoomId = roomId;
        roomUriMap[roomId] = response.uri;
        int existingIndex = discoveredRooms.FindIndex(room => room.RoomId == roomId);

        if (existingIndex >= 0)
        {
            discoveredRooms[existingIndex] = mappedRoom;
        }
        else
        {
            discoveredRooms.Add(mappedRoom);
        }
    }
}
