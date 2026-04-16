using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSetPanel : PanelBase
{
    SettingPanel settingPanel;
    int Wide = 1920, High = 1080;
    bool IsWindow=false;
    CameraFilterPack_BrightnessSaturationAndContrast BrightnessSaturationAndContrast;
    protected override void Start()
    {
        base.Start();
        settingPanel = SettingPanel.instance;
        BrightnessSaturationAndContrast = settingPanel.m_Camera.GetComponent<CameraFilterPack_BrightnessSaturationAndContrast>();

        settingPanel.GameSetPanelQuitButton.onClick.AddListener(QuitButtonClicked);

        settingPanel.LuminanceValueSlider.onValueChanged.AddListener(LuminanceValueChange);
        settingPanel.ContrastValueSlider.onValueChanged.AddListener(ContrastValueChange);
        settingPanel.SaturationValueSlider.onValueChanged.AddListener(SaturationValueChange);
        settingPanel.AudioValueSlider.onValueChanged.AddListener(AudioValueChange);

        settingPanel.LuminanceValueInputField.onValueChanged.AddListener(LuminanceInput);
        settingPanel.ContrastValueInputField.onValueChanged.AddListener(ContrastInput);
        settingPanel.SaturationValueInputField.onValueChanged.AddListener(SaturationInput);
        settingPanel.AudioValueInputField.onValueChanged.AddListener(AudioInput);

        settingPanel.ResolutionDropdown.onValueChanged.AddListener(ResolutionChange);
        settingPanel.WindowToggle.onValueChanged.AddListener(WindowStateChange);
        settingPanel.FPSDropdown.onValueChanged.AddListener(GameFPSChange);
        settingPanel.VsyncToggle.onValueChanged.AddListener(VsyncStateChange);
        InitInputValue();
    }

    /// <summary>
    /// 整体亮度
    /// </summary>
    /// <param name="value"></param>
    void LuminanceValueChange(float value)
    {
        BrightnessSaturationAndContrast.Brightness = value;
        settingPanel.LuminanceValueInputField.text = value.ToString();
    }
    /// <summary>
    /// 对比度
    /// </summary>
    /// <param name="value"></param>
    void ContrastValueChange(float value)
    {
        BrightnessSaturationAndContrast.Contrast = value;
        settingPanel.ContrastValueInputField.text = value.ToString();
    }
    /// <summary>
    /// 饱和度
    /// </summary>
    /// <param name="value"></param>
    void SaturationValueChange(float value)
    {
        BrightnessSaturationAndContrast.Saturation = value;
        settingPanel.SaturationValueInputField.text = value.ToString();
    }
    /// <summary>
    /// 音量
    /// </summary>
    /// <param name="value"></param>
    void AudioValueChange(float value)
    {
        AudioListener.volume = value;
        settingPanel.AudioValueInputField.text = value.ToString();
    }
    void InitInputValue()
    {
        settingPanel.LuminanceValueInputField.text = settingPanel.LuminanceValueSlider.value.ToString();
        settingPanel.ContrastValueInputField.text = settingPanel.ContrastValueSlider.value.ToString();
        settingPanel.SaturationValueInputField.text = settingPanel.SaturationValueSlider.value.ToString();
        settingPanel.AudioValueInputField.text = settingPanel.AudioValueSlider.value.ToString();
    }
    /// <summary>
    /// 亮度输入
    /// </summary>
    /// <param name="value"></param>
    void LuminanceInput(string value)
    {
        if (value == "")
            return;
        float mValue = float.Parse(value);
        if (mValue < settingPanel.LuminanceValueSlider.minValue)
        {
            mValue = settingPanel.LuminanceValueSlider.minValue;
        }
        if (mValue > settingPanel.LuminanceValueSlider.maxValue)
        {
            mValue = settingPanel.LuminanceValueSlider.maxValue;
        }
        //限制大小
        settingPanel.LuminanceValueSlider.value = mValue;
        BrightnessSaturationAndContrast.Brightness = mValue;
    }
    /// <summary>
    /// 对比度输入
    /// </summary>
    /// <param name="value"></param>
    void ContrastInput(string value)
    {
        if (value == "")
            return;
        float mValue = float.Parse(value);
        if (mValue < settingPanel.ContrastValueSlider.minValue)
        {
            mValue = settingPanel.ContrastValueSlider.minValue;
        }
        if (mValue > settingPanel.ContrastValueSlider.maxValue)
        {
            mValue = settingPanel.ContrastValueSlider.maxValue;
        }
        //限制大小
        settingPanel.ContrastValueSlider.value = mValue;
        BrightnessSaturationAndContrast.Contrast = mValue;
    }
    /// <summary>
    /// 饱和度输入
    /// </summary>
    /// <param name="value"></param>
    void SaturationInput(string value)
    {
        if (value == "")
            return;
        float mValue = float.Parse(value);
        if (mValue < settingPanel.SaturationValueSlider.minValue)
        {
            mValue = settingPanel.SaturationValueSlider.minValue;
        }
        if (mValue > settingPanel.SaturationValueSlider.maxValue)
        {
            mValue = settingPanel.SaturationValueSlider.maxValue;
        }
        //限制大小
        settingPanel.SaturationValueSlider.value = mValue;
        BrightnessSaturationAndContrast.Saturation = mValue;
    }
    void AudioInput(string value)
    {
        if (value == "")
            return;
        float mValue = float.Parse(value);
        if (mValue < settingPanel.AudioValueSlider.minValue)
        {
            mValue = settingPanel.AudioValueSlider.minValue;
        }
        if (mValue > settingPanel.AudioValueSlider.maxValue)
        {
            mValue = settingPanel.AudioValueSlider.maxValue;
        }
        //限制大小
        settingPanel.AudioValueSlider.value = mValue;
        AudioListener.volume = mValue;
    }

    /// <summary>
    /// 分辨率
    /// </summary>
    /// <param name="value"></param>
    public void ResolutionChange(int value)
    {
        switch (value)
        {
            case 0:
                Wide = 1920;
                High = 1080;
                break;
            case 1:
                Wide = 1680;
                High = 1050;
                break;
            case 2:
                Wide = 1600;
                High = 1024;
                break;
            case 3:
                Wide = 1600;
                High = 900;
                break;
            case 4:
                Wide = 1400;
                High = 900;
                break;
            case 5:
                Wide = 1366;
                High = 768;
                break;
            case 6:
                Wide = 1360;
                High = 768;
                break;
            case 7:
                Wide = 1280;
                High = 1024;
                break;
            case 8:
                Wide = 1280;
                High = 960;
                break;
            case 9:
                Wide = 1280;
                High = 768;
                break;
            case 10:
                Wide = 1280;
                High = 720;
                break;
            case 11:
                Wide = 1152;
                High = 864;
                break;
            case 12:
                Wide = 1024;
                High = 768;
                break;
            case 13:
                Wide = 800;
                High = 600;
                break;
        }
        ResolutionSet(Wide,High, !IsWindow);
    }
    /// <summary>
    /// 窗口化
    /// </summary>
    /// <param name="value"></param>
    public void WindowStateChange(bool value)
    {
        IsWindow = value;
        ResolutionSet(Wide, High, !IsWindow);
    }
    /// <summary>
    /// 帧速率
    /// </summary>
    /// <param name="value"></param>
    public void GameFPSChange(int value)
    {
        switch (value)
        {
            case 0:
                Application.targetFrameRate = 30;
                break;
            case 1:
                Application.targetFrameRate = 60;
                break;
            case 2:
                Application.targetFrameRate = 144;
                break;
            case 3:
                Application.targetFrameRate = -1;
                break;
        }
        ResolutionSet(Wide, High, !IsWindow);
    }
    /// <summary>
    /// 垂直同步
    /// </summary>
    /// <param name="value"></param>
    public void VsyncStateChange(bool value)
    {
        if (value)
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
        }
    }
    public void ResolutionSet(int wide, int high, bool isFullscreen)
    {
        Screen.SetResolution(wide, high, isFullscreen, 60);
    }


    /// <summary>
    /// 退出按钮
    /// </summary>
    void QuitButtonClicked()
    {
        settingPanel.HideAllPanel();
    }
}
