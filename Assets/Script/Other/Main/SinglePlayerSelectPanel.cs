using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SinglePlayerSelectPanel : PanelBase
{
    private void Awake()
    {
        foreach (var button in GetComponentsInChildren<Button>())
        {
            switch (button.name)
            {
                case "ReverseBackButton"://返回
                    button.onClick.AddListener(ReverseBackButtonClicked);
                    break;
            }
        }
    }
    void ReverseBackButtonClicked()
    {
        HidePanel();
    }
    protected override void Start()
    {
        base.Start();
        ImmediatelyHide();
    }
    
    void Level1ButtonClicked()
    {

    }
    void Level2ButtonClicked()
    {

    }
    void Level3ButtonClicked()
    {

    }
    void Level4ButtonClicked()
    {

    }
    void Level5ButtonClicked()
    {

    }
}
