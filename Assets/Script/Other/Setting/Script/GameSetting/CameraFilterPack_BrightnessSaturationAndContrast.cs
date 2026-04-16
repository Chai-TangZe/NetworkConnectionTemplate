using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Camera Filter Pack/BrightnessSaturationAndContrast")]
public class CameraFilterPack_BrightnessSaturationAndContrast : MonoBehaviour   //继承自基类PostEffectsBase
{
    //指定的Shader，对应名为BrightnessSaturationAndContrast的Shader
    public Shader BriSatConShader;
    //创建的材质
    Material _briSatConMaterial;

    //访问材质
    public Material Material
    {
        get
        {
            if (_briSatConMaterial == null)
            {
                _briSatConMaterial = new Material(BriSatConShader);
                _briSatConMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            //使用CheckShaderAndCreateMaterial函数来得到对应的材质
            //_briSatConMaterial = CheckShaderAndCreateMaterial(BriSatConShader, _briSatConMaterial);
            return _briSatConMaterial;
        }
    }

    //提供调整亮度、饱和度、对比度的参数
    [Header("亮度")]
    [Range(0.0f, 3.0f)] public float Brightness = 1.0f;
    [Header("饱和度")]
    [Range(0.0f, 3.0f)] public float Saturation = 1.0f;
    [Header("对比度")]
    [Range(0.0f, 3.0f)] public float Contrast = 1.0f;
    private void Start()
    {
        BriSatConShader = Shader.Find("Unity Shaders Book/Chapter 12/BrightnessSaturationAndContrast");
    }

    //定义OnRenderImage函数来进行真正的特效处理
    //src存储当前渲染的图像纹理，dest为经过处理后的目标渲染纹理
    //通常情况下会在所有透明和不透明Pass执行完毕后被调用
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        //此函数被调用时会检查材质是否可用，可用就把参数传递给材质再调用Graphics.Blit进行处理
        //否则直接把原图显示到屏幕上，不做任何处理
        if (Material != null)
        {
            Material.SetFloat("_Brightness", Brightness);
            Material.SetFloat("_Saturation", Saturation);
            Material.SetFloat("_Contrast", Contrast);

            //使用此函数来完成对渲染纹理的处理
            //此函数会把第一个参数传递给Shader中名为_MainTex的属性，所以Shader中必须有_MainTex纹理
            Graphics.Blit(src, dest, Material);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }

    void OnDisable()
    {
        if (_briSatConMaterial)
        {
            DestroyImmediate(_briSatConMaterial);
        }
    }
}