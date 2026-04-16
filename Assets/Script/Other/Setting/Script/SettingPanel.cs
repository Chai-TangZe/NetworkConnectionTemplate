using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : PanelBase
{
    static SettingPanel settingPanel;
    public static SettingPanel instance
    {
        get { return settingPanel; }
    }

    [HideInInspector]
    public Camera m_Camera;//得到相机
    public DataProcessing dataProcessing;
    CameraFilterPack_Blur Blur;//屏幕虚化效果

    //得到三个页面
    private GameSetPanel Panel_GameSet;
    private GraphicsSetPanel Panel_GraphicsSet;
    private KeypadSetPanel Panel_KeypadSet;

    //各种可调参数
    [HideInInspector]
    public Slider LuminanceValueSlider, ContrastValueSlider, SaturationValueSlider, AudioValueSlider
        , VisibilityValueSlider;
    [HideInInspector]
    public InputField LuminanceValueInputField, ContrastValueInputField, SaturationValueInputField, AudioValueInputField
        , VisibilityValueInputField;
    [HideInInspector]
    public Dropdown ResolutionDropdown, FPSDropdown
        , AntiAliasingDropdown, ShadowQualityDropdown, GrainQualityDropdown;
    [HideInInspector]
    public Toggle WindowToggle, VsyncToggle;
    [HideInInspector]
    public Button GameSetPanelQuitButton, KeypadSetPanelQuitButton, GraphicsSetPanelQuitButton;

    bool IsActivate = false;

    void GetData()
    {
        dataProcessing.Data_LuminanceValue = LuminanceValueSlider.value;
        dataProcessing.Data_ContrastValue = ContrastValueSlider.value;
        dataProcessing.Data_SaturationValue = SaturationValueSlider.value;
        dataProcessing.Data_AudioValue = AudioValueSlider.value;
        dataProcessing.Data_VisibilityValue = VisibilityValueSlider.value;

        dataProcessing.Data_ResolutionIndex = ResolutionDropdown.value;
        dataProcessing.Data_FPSIndex = FPSDropdown.value;
        dataProcessing.Data_AntiAliasingIndex = AntiAliasingDropdown.value;
        dataProcessing.Data_ShadowQualityIndex = ShadowQualityDropdown.value;
        dataProcessing.Data_GrainQualityIndex = GrainQualityDropdown.value;

        dataProcessing.Data_IsWindow = WindowToggle.isOn;
        dataProcessing.Data_IsVsync = VsyncToggle.isOn;

        dataProcessing.Keypads.Clear();
        foreach (var key in Panel_KeypadSet.downAnyKeys)
        {
            dataProcessing.Keypads.Add((int)key.key);
        }
    }
    void SetData()
    {
        LuminanceValueSlider.value = dataProcessing.Data_LuminanceValue;
        ContrastValueSlider.value = dataProcessing.Data_ContrastValue;
        SaturationValueSlider.value = dataProcessing.Data_SaturationValue;
        AudioValueSlider.value = dataProcessing.Data_AudioValue;
        VisibilityValueSlider.value = dataProcessing.Data_VisibilityValue;

        ResolutionDropdown.value = dataProcessing.Data_ResolutionIndex;
        Panel_GameSet.ResolutionChange(dataProcessing.Data_ResolutionIndex);
        FPSDropdown.value = dataProcessing.Data_FPSIndex;
        Panel_GameSet.GameFPSChange(dataProcessing.Data_FPSIndex);
        AntiAliasingDropdown.value = dataProcessing.Data_AntiAliasingIndex;
        Panel_GraphicsSet.AntiAliasingChange(dataProcessing.Data_AntiAliasingIndex);
        ShadowQualityDropdown.value = dataProcessing.Data_ShadowQualityIndex;
        Panel_GraphicsSet.ShadowQualityChange(dataProcessing.Data_ShadowQualityIndex);
        GrainQualityDropdown.value = dataProcessing.Data_GrainQualityIndex;
        Panel_GraphicsSet.GrainQualityChange(dataProcessing.Data_GrainQualityIndex);

        WindowToggle.isOn = dataProcessing.Data_IsWindow;
        Panel_GameSet.WindowStateChange(dataProcessing.Data_IsWindow);
        VsyncToggle.isOn = dataProcessing.Data_IsVsync;
        Panel_GameSet.VsyncStateChange(dataProcessing.Data_IsVsync);

        for (int Index = 0; Index < dataProcessing.Keypads.Count; Index++)
        {
            Panel_KeypadSet.downAnyKeys[Index].SetKeyValue(dataProcessing.Keypads[Index]);
        }
    }
    /// <summary>
    /// 用于得到全部可控参数
    /// </summary>
    private void Awake()
    {
        settingPanel = this;
        m_Camera = Camera.main;
        Blur = m_Camera.GetComponent<CameraFilterPack_Blur>();
        foreach (var button in GetComponentsInChildren<Button>())
        {
            switch (button.name)
            {
                case "游戏设置Button":
                    button.onClick.AddListener(GameSetButtonClicked);
                    break;
                case "按键设置Button":
                    button.onClick.AddListener(KeypadSetButtonClicked);
                    break;
                case "图像设置Button":
                    button.onClick.AddListener(GraphicsSetButtonClicked);
                    break;
                //case "退出游戏Button":
                //    button.onClick.AddListener(QuitGameSettingPanel);
                //    break;
                case "游戏设置退出Button":
                    GameSetPanelQuitButton = button;
                    break;
                case "按键设置退出Button":
                    KeypadSetPanelQuitButton = button;
                    break;
                case "图像设置退出Button":
                    GraphicsSetPanelQuitButton = button;
                    break;
                case "ReverseBackButton"://返回
                    button.onClick.AddListener(ReverseBackButtonClicked);
                    break;
            }
        }
        foreach (RectTransform panel in transform)
        {
            switch (panel.name)
            {
                case "游戏设置Panel":
                    Panel_GameSet = panel.GetComponent<GameSetPanel>();
                    break;
                case "按键设置Panel":
                    Panel_KeypadSet = panel.GetComponent<KeypadSetPanel>();
                    break;
                case "图像设置Panel":
                    Panel_GraphicsSet = panel.GetComponent<GraphicsSetPanel>();
                    break;
            }
        }
        foreach (var slider in GetComponentsInChildren<Slider>())
        {
            switch (slider.name)
            {
                case "亮度调节Slider":
                    LuminanceValueSlider = slider;
                    break;
                case "对比度调节Slider":
                    ContrastValueSlider = slider;
                    break;
                case "饱和度调节Slider":
                    SaturationValueSlider = slider;
                    break;
                case "全局音量调节Slider":
                    AudioValueSlider = slider;
                    break;
                case "整体可见度Slider":
                    VisibilityValueSlider = slider;
                    break;
            }
        }
        foreach (var inputField in GetComponentsInChildren<InputField>())
        {
            switch (inputField.name)
            {
                case "亮度调节InputField":
                    LuminanceValueInputField = inputField;
                    break;
                case "对比度调节InputField":
                    ContrastValueInputField = inputField;
                    break;
                case "饱和度调节InputField":
                    SaturationValueInputField = inputField;
                    break;
                case "全局音量调节InputField":
                    AudioValueInputField = inputField;
                    break;
                case "整体可见度InputField":
                    VisibilityValueInputField = inputField;
                    break;
            }
        }
        foreach (var dropDown in GetComponentsInChildren<Dropdown>())
        {
            switch (dropDown.name)
            {
                case "分辨率设置Dropdown":
                    ResolutionDropdown = dropDown;
                    break;
                case "帧率设置Dropdown":
                    FPSDropdown = dropDown;
                    break;
                case "抗锯齿设置Dropdown":
                    AntiAliasingDropdown = dropDown;
                    break;
                case "阴影质量Dropdown":
                    ShadowQualityDropdown = dropDown;
                    break;
                case "纹理质量Dropdown":
                    GrainQualityDropdown = dropDown;
                    break;
            }
        }
        foreach (var toggle in GetComponentsInChildren<Toggle>())
        {
            switch (toggle.name)
            {
                case "窗口化Toggle":
                    WindowToggle = toggle;
                    break;
                case "垂直同步Toggle":
                    VsyncToggle = toggle;
                    break;
            }
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    protected override void Start()
    {
        base.Start();
        HideAllPanel();
        ImmediatelyHide();
        LimitPhoneRotate();
    }
    void LimitPhoneRotate()
    {
        //设置屏幕自动旋转， 并置支持的方向
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
    }

    /// <summary>
    /// 关闭所有打开的页面
    /// </summary>
    public void HideAllPanel()
    {
        Panel_GameSet.HidePanel();
        Panel_GraphicsSet.HidePanel();
        Panel_KeypadSet.HidePanel();
    }

    /// <summary>
    /// 得到页面的状态,一旦有一个打开则返回true
    /// </summary>
    /// <returns></returns>
    bool AllPanelState()
    {
        if (Panel_GameSet.ActivateState || Panel_GraphicsSet.ActivateState || Panel_KeypadSet.ActivateState)
        {
            return true;
        }
        return false;
    }

    #region 按钮事件

    void GameSetButtonClicked()
    {
        Panel_GameSet.ShowPanel();
    }
    void KeypadSetButtonClicked()
    {
        Panel_KeypadSet.ShowPanel();
    }
    void GraphicsSetButtonClicked()
    {
        Panel_GraphicsSet.ShowPanel();
    }
    void QuitGameSettingPanel()
    {
        GetData();
        dataProcessing.SaveDate();
        Application.Quit();
    }

    #endregion

    /// <summary>
    /// 打开设置页面
    /// </summary>
    public override void ShowPanel()
    {
        base.ShowPanel();
        SetData();
        Blur.enabled = true;
        IsActivate = true;
    }
    /// <summary>
    /// 关闭设置页面
    /// </summary>
    public override void HidePanel()
    {
        if (AllPanelState())//如果有页面打开则先关闭页面
        {
            HideAllPanel();
        }
        else
        {
            base.HidePanel();
            GetData();
            dataProcessing.SaveDate();
            Blur.enabled = false;
            IsActivate = false;
        }
    }

    void ReverseBackButtonClicked()
    {
        HidePanel();
    }
    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsActivate)
            {
                HidePanel();
            }
            else
            {
                //ShowPanel();
            }
        }
    }
}
