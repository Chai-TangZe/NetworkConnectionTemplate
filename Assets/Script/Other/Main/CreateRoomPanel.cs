using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomPanel : PanelBase
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
                case "ConfirmCreateButton":
                    button.onClick.AddListener(ConfirmCreateButtonClicked);
                    break;
            }
        }
    }

    private void ConfirmCreateButtonClicked()
    {
        mainSelectPanel.Panel_Room.ShowPanel();
        HidePanel();
    }

    void ReverseBackButtonClicked()
    {
        HidePanel();
        mainSelectPanel.Panel_Lobby.ShowPanel();
    }
    protected override void Start()
    {
        base.Start();
        ImmediatelyHide();
        mainSelectPanel = MainSelectPanel.instance;
    }
}
