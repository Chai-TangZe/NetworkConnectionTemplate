using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsSetPanel : PanelBase
{
    SettingPanel settingPanel;
    protected override void Start()
    {
        base.Start();
        settingPanel = SettingPanel.instance;
        settingPanel.GraphicsSetPanelQuitButton.onClick.AddListener(QuitButtonClicked);

        settingPanel.VisibilityValueSlider.onValueChanged.AddListener(VisibilityValueChange);
        settingPanel.VisibilityValueInputField.onValueChanged.AddListener(VisibilityInput);
        settingPanel.AntiAliasingDropdown.onValueChanged.AddListener(AntiAliasingChange);
        settingPanel.ShadowQualityDropdown.onValueChanged.AddListener(ShadowQualityChange);
        settingPanel.GrainQualityDropdown.onValueChanged.AddListener(GrainQualityChange);
        InitInputValue();
    }
    /// <summary>
    /// 整体可见度
    /// </summary>
    /// <param name="value"></param>
    void VisibilityValueChange(float value)
    {
        settingPanel.m_Camera.farClipPlane = value;
        settingPanel.VisibilityValueInputField.text = value.ToString();
    }
    /// <summary>
    /// 可见度输入
    /// </summary>
    /// <param name="value"></param>
    void VisibilityInput(string value)
    {
        if (value == "")
            return;
        float mValue = float.Parse(value);
        if (mValue < settingPanel.VisibilityValueSlider.minValue)
        {
            mValue = settingPanel.VisibilityValueSlider.minValue;
        }
        if (mValue > settingPanel.VisibilityValueSlider.maxValue)
        {
            mValue = settingPanel.VisibilityValueSlider.maxValue;
        }
        //限制大小
        settingPanel.VisibilityValueSlider.value = mValue;
        settingPanel.m_Camera.farClipPlane = mValue;
    }
    void InitInputValue()
    {
        settingPanel.VisibilityValueInputField.text = settingPanel.VisibilityValueSlider.value.ToString();
    }
    /// <summary>
    /// 抗锯齿
    /// </summary>
    /// <param name="value"></param>
    public void AntiAliasingChange(int value)
    {
        switch (value)
        {
            case 0:
                settingPanel.m_Camera.GetComponent<FXAA>().enabled = true;
                break;
            case 1:
                settingPanel.m_Camera.GetComponent<FXAA>().enabled = false;
                break;
            case 2:
                settingPanel.m_Camera.GetComponent<FXAA>().enabled = false;
                break;
        }
        
    }
    /// <summary>
    /// 阴影质量
    /// </summary>
    /// <param name="value"></param>
    public void ShadowQualityChange(int value)
    {
        switch (value)
        {
            case 0:
                QualitySettings.shadows = ShadowQuality.Disable;
                break;
            case 1:
                QualitySettings.shadows = ShadowQuality.All;
                break;
            case 2:
                break;
        }
    }
    /// <summary>
    /// 纹理质量
    /// </summary>
    /// <param name="value"></param>
    public void GrainQualityChange(int value)
    {
        switch (value)
        {
            case 0:
                QualitySettings.pixelLightCount = 0;
                break;
            case 1:
                QualitySettings.pixelLightCount = 2;
                break;
            case 2:
                QualitySettings.pixelLightCount = 4;
                break;
        }
    }

    /// <summary>
    /// 退出按钮
    /// </summary>
    void QuitButtonClicked()
    {
        settingPanel.HideAllPanel();
    }
}
