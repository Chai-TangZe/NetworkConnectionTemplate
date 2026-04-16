using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainSelectPanel : MonoBehaviour
{
    static MainSelectPanel mainSelectPanel;
    public static MainSelectPanel instance
    {
        get { return mainSelectPanel; }
    }
    InitalPanel Panel_InitalPanel;
    //需要展示的页面
    [HideInInspector]
    public SinglePlayerSelectPanel Panel_SinglePlayerSelect;
    [HideInInspector]
    public MultiplayerSelectPanel Panel_MultiplayerSelect;
    [HideInInspector]
    public SettingPanel Panel_Setting;
    [HideInInspector]
    public DeveloperPanel Panel_Developer;

    [HideInInspector]
    public PlayerSettingPanel Panel_PlayerSettings;
    [HideInInspector]
    public LobbyPanel Panel_Lobby;
    [HideInInspector]
    public CreateRoomPanel Panel_CreateRoom;
    [HideInInspector]
    public RoomPanel Panel_Room;


    private void Awake()
    {
        mainSelectPanel = this;
        foreach (var button in GetComponentsInChildren<Button>())
        {
            switch (button.name)
            {
                case "SinglePlayerSelectButton"://单人
                    button.onClick.AddListener(SinglePlayerSelectButtonClicked);
                    break;
                case "MultiplayerSelectButton"://多人
                    button.onClick.AddListener(MultiplayerSelectButtonClicked);
                    break;
                case "SettingButton"://设置
                    button.onClick.AddListener(SettingButtonClicked);
                    break;
                case "DeveloperButton"://开发者团队
                    button.onClick.AddListener(DeveloperButtonClicked);
                    break;
                case "QuitButton"://退出按钮
                    button.onClick.AddListener(QuitGameButtonClicked);
                    break;
            }
        }
        foreach (RectTransform panel in transform)
        {
            switch (panel.name)
            {
                case "InitalPanel":
                    Panel_InitalPanel = panel.GetComponent<InitalPanel>();
                    break;
                case "SinglePlayerSelectPanel":
                    Panel_SinglePlayerSelect = panel.GetComponent<SinglePlayerSelectPanel>();
                    break;
                case "MultiplayerSelectPanel":
                    Panel_MultiplayerSelect = panel.GetComponent<MultiplayerSelectPanel>();
                    break;
                case "SettingPanel":
                    Panel_Setting = panel.GetComponent<SettingPanel>();
                    break;
                case "DeveloperPanel":
                    Panel_Developer = panel.GetComponent<DeveloperPanel>();
                    break;
                case "PlayerSettingPanel":
                    Panel_PlayerSettings = panel.GetComponent<PlayerSettingPanel>();
                    break;
                case "LobbyPanel":
                    Panel_Lobby = panel.GetComponent<LobbyPanel>();
                    break;
                case "CreateRoomPanel":
                    Panel_CreateRoom = panel.GetComponent<CreateRoomPanel>();
                    break;
                case "RoomPanel":
                    Panel_Room = panel.GetComponent<RoomPanel>();
                    break;
            }
        }
    }
    #region 按钮事件
    private void QuitGameButtonClicked()
    {
        Application.Quit();
    }

    private void DeveloperButtonClicked()
    {
        Panel_Developer.ShowPanel();
    }

    private void SettingButtonClicked()
    {
        Panel_Setting.ShowPanel();
    }

    private void MultiplayerSelectButtonClicked()
    {
        Panel_MultiplayerSelect.ShowPanel();
    }

    private void SinglePlayerSelectButtonClicked()
    {
        Panel_SinglePlayerSelect.ShowPanel();
    }
    #endregion
    void HideInitalPanel()
    {
        Panel_InitalPanel.HidePanel();
    }
}
