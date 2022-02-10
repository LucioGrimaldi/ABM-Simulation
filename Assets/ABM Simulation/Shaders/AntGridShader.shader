// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.

Shader "PDT Shaders/AntGrid" {
	Properties {
		_LineColor ("Line Color", Color) = (0.2735849,0.2735849,0.2735849,1)
		_CellColor ("Cell Color", Color) = (0,0,0,0)
		_FoodColor ("Cell Color Food", Color) = (0,0,1,1)
		_HomeColor ("Cell Color Home", Color) = (0,1,0,1)
		[PerRendererData] _MainTex ("Albedo (RGB)", 2D) = "white" {}
		[IntRange] _GridSize("Grid Size", Range(1,500)) = 10
		_LineSize("Line Size", Range(0,1)) = 0.15
		[IntRange] _Toggle("Toggle ( 0 = False , 1 = True )", Range(0,1)) = 1.0
		[IntRange] _Width ("Width", Range(0,10000)) = 0
	}
		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			LOD 200

			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM

		uniform int count;
		uniform sampler1D array;
			
		#pragma enable_d3d11_debug_symbols

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:blend

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		#include "UnityCG.cginc"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		float4 _LineColor;
		float4 _CellColor;
		float4 _FoodColor;
		float4 _HomeColor;

		float _GridSize;
		float _LineSize;

		float _Toggle;
		
		#ifdef SHADER_API_D3D11
		uniform StructuredBuffer<float> _FoodGrid;
		uniform StructuredBuffer<float> _HomeGrid;
		#endif

		int _Width;

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color

			float2 uv = IN.uv_MainTex;

			fixed4 c = float4(0.0,0.0,0.0,0.0);

			float gsize = floor(_GridSize);

			gsize += _LineSize;

			float2 id;

			id.x = floor(uv.x/(1.0/gsize));
			id.y = floor(uv.y/(1.0/gsize));

			float4 color = _CellColor;
			float brightness = _CellColor.a;
						
			if (round(_Toggle) == 1.0)
			{
				#ifdef SHADER_API_D3D11
				float intensity;

				intensity = _HomeGrid[id.y + id.x * _Width];
				if (intensity > 0.0)
				{
					color = _HomeColor;
					color.a = (intensity > 1 ) ? 1 : intensity;
				}
				intensity = _FoodGrid[id.y + id.x * _Width];
				if (intensity > 0.0)
				{
					color = _FoodColor;
					color.a = (intensity > 1) ? 1 : intensity;
				}
				#endif					
			}
			

			if (frac(uv.x*gsize) <= _LineSize || frac(uv.y*gsize) <= _LineSize)
			{
				color = _LineColor;
			}
			

			//Clip transparent spots using alpha cutout
			if (color.a <= 0.00001)
			{
				clip(c.a - 1.0);
			}
			
			o.Albedo = color.rgba;
			// Metallic and smoothness come from slider variables
			o.Metallic = 0.0;
			o.Smoothness = 0.0;
			o.Alpha = color.a;
		}
		
		ENDCG
	}
	//FallBack "Diffuse"
}
