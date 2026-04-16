Shader "Unity Shaders Book/Chapter 12/BrightnessSaturationAndContrast"
{
	Properties
	{
		//由于Graphic.Blit方法，所以必须有_MainTex
		_MainTex("Base(RGB)", 2D) = "white" {}
	//由于这些属性声明只是为了显示在面板中，但是对于屏幕特效来说，它们使用的材质都是临时创建的，这些值是直接从脚本中传递给Shader
	//所以这些声明可以省略
	//亮度
	_Brightness("Brightness", Float) = 1
		//饱和度
		_Saturation("Saturation", Float) = 1
		//对比度
		_Contrast("Contrast", Float) = 1

	}
		SubShader
	{
		Pass
		{
			//屏幕后处理渲染设置的标配
			//关闭深度写入，防止挡住在其后面被渲染的物体
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			half _Brightness;
			half _Saturation;
			half _Contrast;

			/*struct a2v
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};*/

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv : TEXCOORD0;
			};

			//使用内置appdata_img结构体作为顶点着色器的输入，它只包含了图像处理时必需的顶点坐标和纹理坐标等变量
			v2f vert(appdata_img v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				return o;
			}

			//实现用于调整亮度、饱和度、对比度的片元着色器
			fixed4 frag(v2f i) : SV_Target
			{
				//对原屏幕图像进行采样
				fixed4 renderTex = tex2D(_MainTex, i.uv);
			//调整亮度
			//原颜色乘以亮度系数
			fixed3 finalColor = renderTex.rgb * _Brightness;

			//调整饱和度
			//计算该像素对应的亮度值，每一个颜色分量乘以一个特定的系数值再相加
			fixed luminance = 0.2125 * renderTex.r + 0.7154 * renderTex.g + 0.0721 * renderTex.b;
			//创建一个饱和度为0的颜色值
			fixed3 luminanceColor = fixed3(luminance, luminance, luminance);
			//使用_Saturation和其上一步得到的颜色之间进行插值，得到希望的饱和度
			finalColor = lerp(luminanceColor, finalColor, _Saturation);

			//调整对比度
			//创建一个对比度为0的颜色值，每个分量均为0.5
			fixed3 avgColor = fixed3(0.5, 0.5, 0.5);
			//使用_Contrast在其和上一步得到的颜色之间进行插值
			finalColor = lerp(avgColor, finalColor, _Contrast);

			return fixed4(finalColor, renderTex.a);
		}
		ENDCG
	}
	}
		//关闭Fallback
			Fallback off
}