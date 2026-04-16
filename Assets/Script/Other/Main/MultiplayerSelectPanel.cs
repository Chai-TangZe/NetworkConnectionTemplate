using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerSelectPanel : PanelBase
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
                case "MyPlayerSettingButton":
                    button.onClick.AddListener(MyPlayerSettingButtonClicked);
                    break;
                case "LANModeButton":
                    button.onClick.AddListener(LANModeButtonClicked);
                    break;
                case "WANModeButton":
                    button.onClick.AddListener(WANModeButtonClicked);
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
    private void WANModeButtonClicked()
    {
        mainSelectPanel.Panel_Lobby.ShowPanel();
        mainSelectPanel.Panel_PlayerSettings.HidePanel();
        HidePanel();
    }

    private void LANModeButtonClicked()
    {
        mainSelectPanel.Panel_Lobby.ShowPanel();
        mainSelectPanel.Panel_PlayerSettings.HidePanel();
        HidePanel();
    }

    private void MyPlayerSettingButtonClicked()
    {
        mainSelectPanel.Panel_PlayerSettings.ShowPanel();
        HidePanel();
    }

    void ReverseBackButtonClicked()
    {
        HidePanel();
    }
}
