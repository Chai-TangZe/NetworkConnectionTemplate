using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPanel : PanelBase
{
    MainSelectPanel mainSelectPanel = null;
    private void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>())
        {
            switch (button.name)
            {
                case "ReverseBackButton"://返回
                    button.onClick.AddListener(ReverseBackButtonClicked);
                    break;
                case "CreateRoomButton":
                    button.onClick.AddListener(CreateRoomButtonClicked);
                    break;
                case "RefreshListButton":
                    button.onClick.AddListener(RefreshListButtonClicked);
                    break;
                case "JoinRoomButton":
                    button.onClick.AddListener(JoinRoomButtonClicked);
                    break;
            }
        }
    }
    protected override void Start()
    {
        base.Start();
        ImmediatelyHide();
        mainSelectPanel = MainSelectPanel.instance;
    }

    private void JoinRoomButtonClicked()
    {
        mainSelectPanel.Panel_Room.ShowPanel();
        HidePanel();
    }

    private void CreateRoomButtonClicked()
    {
        mainSelectPanel.Panel_CreateRoom.ShowPanel();
        HidePanel();
    }

    private void RefreshListButtonClicked()
    {
        Debug.Log("刷新列表...");
    }

    void ReverseBackButtonClicked()
    {
        HidePanel();
    }
}
