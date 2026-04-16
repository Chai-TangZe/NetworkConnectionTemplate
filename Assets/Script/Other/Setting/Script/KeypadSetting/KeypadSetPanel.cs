using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeypadSetPanel : PanelBase
{
    SettingPanel settingPanel;
    [HideInInspector]
    public DownAnyKey[] downAnyKeys;
    private void Awake()
    {
        downAnyKeys = GetComponentsInChildren<DownAnyKey>();
    }
    protected override void Start()
    {
        base.Start();
        settingPanel = SettingPanel.instance;
        settingPanel.KeypadSetPanelQuitButton.onClick.AddListener(QuitButtonClicked);
    }

    /// <summary>
    /// 退出按钮
    /// </summary>
    void QuitButtonClicked()
    {
        RecoverKeypadSetting();
        settingPanel.HideAllPanel();
    }
    bool RecoverKeypadSetting()
    {
        foreach (var item in downAnyKeys)
        {
            if (item.IsChangeKey)
            {
                item.RecoverState();
                return true;
            }
        }
        return false;
    }
}
