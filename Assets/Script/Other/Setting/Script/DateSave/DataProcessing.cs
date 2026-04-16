using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TFramework;

public class DataProcessing : MonoBehaviour
{
    public float Data_LuminanceValue=1, Data_ContrastValue=1, Data_SaturationValue=1, Data_AudioValue=1, Data_VisibilityValue=200;
    public int Data_ResolutionIndex = 0, Data_FPSIndex = 1, Data_AntiAliasingIndex=0, Data_ShadowQualityIndex=2, Data_GrainQualityIndex=2;
    public bool Data_IsWindow=false, Data_IsVsync=false;
    public List<int> Keypads = new List<int>();
    private void Awake()
    {
        ReadDate();
    }
    /// <summary>
    /// 数据保存在本地StreamingAssets文件夹
    /// </summary>
    public void SaveDate()
    {
        FileUtil.ClearStreamingAssetsData("SettingData");//清空之前的数据
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_LuminanceValue=" + Data_LuminanceValue);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_ContrastValue=" + Data_ContrastValue);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_SaturationValue=" + Data_SaturationValue);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_AudioValue=" + Data_AudioValue);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_VisibilityValue=" + Data_VisibilityValue);
        FileUtil.WriteStreamingAssetsData("SettingData", "");
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_ResolutionIndex=" + Data_ResolutionIndex);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_FPSIndex=" + Data_FPSIndex);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_AntiAliasingIndex=" + Data_AntiAliasingIndex);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_ShadowQualityIndex=" + Data_ShadowQualityIndex);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_GrainQualityIndex=" + Data_GrainQualityIndex);
        FileUtil.WriteStreamingAssetsData("SettingData", "");
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_IsWindow=" + Data_IsWindow);
        FileUtil.WriteStreamingAssetsData("SettingData", "Data_IsVsync=" + Data_IsVsync);
        FileUtil.WriteStreamingAssetsData("SettingData", "");
        int keyIndex = 1;
        foreach (int key in Keypads)
        {
            FileUtil.WriteStreamingAssetsData("SettingData", "Key"+ keyIndex+"=" + key);
            keyIndex++;
        }
    }

    void ReadDate()
    {
        Keypads.Clear();
        List<string> SettingDate = FileUtil.ReadStreamingAssetsData("SettingData");
        if (SettingDate == null)
        {
            SetDefaultData();
            return;
        }
        foreach (var item in SettingDate)
        {
            if (item=="")
                continue;
            string str = StringUtil.Substring(item, '=', false);
            string value = StringUtil.Substring(item, '=', true);
            switch (str)
            {
                case "Data_LuminanceValue":
                    Data_LuminanceValue = float.Parse(value);
                    break;
                case "Data_ContrastValue":
                    Data_ContrastValue = float.Parse(value);
                    break;
                case "Data_SaturationValue":
                    Data_SaturationValue = float.Parse(value);
                    break;
                case "Data_AudioValue":
                    Data_AudioValue = float.Parse(value);
                    break;
                case "Data_VisibilityValue":
                    Data_VisibilityValue = float.Parse(value);
                    break;
                case "Data_ResolutionIndex":
                    Data_ResolutionIndex = int.Parse(value);
                    break;
                case "Data_FPSIndex":
                    Data_FPSIndex = int.Parse(value);
                    break;
                case "Data_AntiAliasingIndex":
                    Data_AntiAliasingIndex = int.Parse(value);
                    break;
                case "Data_ShadowQualityIndex":
                    Data_ShadowQualityIndex = int.Parse(value);
                    break;
                case "Data_GrainQualityIndex":
                    Data_GrainQualityIndex = int.Parse(value);
                    break;
                case "Data_IsWindow":
                    Data_IsWindow = bool.Parse(value);
                    break;
                case "Data_IsVsync":
                    Data_IsVsync = bool.Parse(value);
                    break;
            }
            if (str.Substring(0,3)=="Key")
            {
                Keypads.Add(int.Parse(value));
            }
        }
    }
    void SetDefaultData()
    {
        Data_LuminanceValue = 1;
        Data_ContrastValue = 1;
        Data_SaturationValue = 1;
        Data_AudioValue = 1;
        Data_VisibilityValue = 200;
        Data_ResolutionIndex = 0;
        Data_FPSIndex = 1;
        Data_AntiAliasingIndex = 1;
        Data_ShadowQualityIndex = 1;
        Data_GrainQualityIndex = 2;
        Data_IsWindow = false;
        Data_IsVsync = false;
    }
}
