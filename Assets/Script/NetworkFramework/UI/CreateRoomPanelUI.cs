using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomPanelUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] InputField roomNameInput;
    [SerializeField] InputField roomIdInput;
    [SerializeField] InputField roomPasswordInput;
    [SerializeField] InputField maxPlayersInput;
    [SerializeField] Text selectedMapNameText;
    [SerializeField] Image selectedMapPosterImage;
    [SerializeField] Text tipText;

    [Header("地图选项")]
    [SerializeField] UIItemContainer mapListContainer;
    [SerializeField] CreateRoomMapItem mapItemPrefab;

    readonly List<MapDataDefinition> mapOptions = new List<MapDataDefinition>();
    int selectedMapIndex;
    string defaultPlayerName = "玩家";

    Func<string> roomIdProvider;
    public event Action<string, string, int, string, string> OnCreateRequested;
    public event Action OnBackRequested;

    public void SetDefaultPlayerName(string playerName)
    {
        defaultPlayerName = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
    }

    public void SetRoomIdProvider(Func<string> provider)
    {
        roomIdProvider = provider;
    }

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (maxPlayersInput != null && string.IsNullOrWhiteSpace(maxPlayersInput.text))
        {
            maxPlayersInput.text = "10";
        }

        if (roomNameInput != null && string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            roomNameInput.text = $"{defaultPlayerName}创建的房间";
        }

        if (roomIdInput != null)
        {
            roomIdInput.text = roomIdProvider != null ? roomIdProvider.Invoke() : GenerateRandomRoomId();
        }

        if (roomPasswordInput != null)
        {
            roomPasswordInput.text = string.Empty;
        }

        LoadMapOptions();
        BuildMapItems();
        SelectMapByIndex(Mathf.Clamp(selectedMapIndex, 0, Mathf.Max(0, mapOptions.Count - 1)));
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void OnClickBackToLobby()
    {
        OnBackRequested?.Invoke();
    }

    public void OnClickCreateRoom()
    {
        if (mapOptions.Count == 0)
        {
            SetTip("创建失败：未配置地图选项。");
            return;
        }

        string roomName = roomNameInput != null ? roomNameInput.text : $"{defaultPlayerName}创建的房间";
        string mapName = mapOptions[Mathf.Clamp(selectedMapIndex, 0, mapOptions.Count - 1)].mapType.ToString();
        int maxPlayers = 10;
        if (maxPlayersInput != null && int.TryParse(maxPlayersInput.text, out int parsed))
        {
            maxPlayers = parsed;
        }

        string roomId = roomIdInput != null ? roomIdInput.text : GenerateRandomRoomId();
        string roomPassword = roomPasswordInput != null ? roomPasswordInput.text : string.Empty;
        OnCreateRequested?.Invoke(roomName, mapName, maxPlayers, roomId, roomPassword);
    }

    public void OnClickSelectMapByIndex(int index)
    {
        SelectMapByIndex(index);
    }

    void SelectMapByIndex(int index)
    {
        if (mapOptions.Count == 0)
        {
            return;
        }

        selectedMapIndex = Mathf.Clamp(index, 0, mapOptions.Count - 1);
        MapDataDefinition option = mapOptions[selectedMapIndex];

        if (selectedMapNameText != null)
        {
            selectedMapNameText.text = string.IsNullOrWhiteSpace(option.displayName) ? option.mapType.ToString() : option.displayName;
        }

        if (selectedMapPosterImage != null)
        {
            selectedMapPosterImage.sprite = option.poster;
            selectedMapPosterImage.enabled = option.poster != null;
        }
    }

    void BuildMapItems()
    {
        if (mapListContainer == null || mapItemPrefab == null)
        {
            return;
        }

        mapListContainer.ClearItems();
        Transform parent = mapListContainer.GetItemsParent();
        for (int i = 0; i < mapOptions.Count; i++)
        {
            MapDataDefinition option = mapOptions[i];
            CreateRoomMapItem item = Instantiate(mapItemPrefab, parent);
            string displayName = string.IsNullOrWhiteSpace(option.displayName) ? option.mapType.ToString() : option.displayName;
            item.SetData(option.poster, displayName, i, OnClickSelectMapByIndex);
        }
    }

    void LoadMapOptions()
    {
        mapOptions.Clear();
        mapOptions.AddRange(MapDataRepository.GetAll().Where(option => option != null));

        if (mapOptions.Count == 0)
        {
            // 没有资源配置时提供兜底地图，避免流程被卡死。
            foreach (string mapName in GameMapCatalog.GetMapNames())
            {
                if (Enum.TryParse(mapName, out GameMapType mapType))
                {
                    MapDataDefinition fallback = ScriptableObject.CreateInstance<MapDataDefinition>();
                    fallback.mapType = mapType;
                    fallback.displayName = mapType.ToString();
                    fallback.poster = null;
                    mapOptions.Add(fallback);
                }
            }
        }
    }

    void SetTip(string message)
    {
        if (tipText != null)
        {
            tipText.text = message;
        }
    }

    static string GenerateRandomRoomId()
    {
        return UnityEngine.Random.Range(100000, 999999).ToString();
    }
}
