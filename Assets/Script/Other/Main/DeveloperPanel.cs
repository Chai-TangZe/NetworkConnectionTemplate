using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeveloperPanel : PanelBase
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
}
