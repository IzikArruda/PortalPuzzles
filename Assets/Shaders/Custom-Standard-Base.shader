Shader "Hidden/TerrainEngine/Splatmap/Custom-Standard-Base" {
	Properties {
		_MainTex ("Base (RGB) Smoothness (A)", 2D) = "white" {}
		_MetallicTex ("Metallic (R)", 2D) = "white" {}

		// used in fallback on old cards
		_Color ("Main Color", Color) = (1,1,1,1)
	}

	SubShader {
		Tags {
			"RenderType" = "Opaque"
			"Queue" = "Geometry-100"
		}
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0
		// needs more than 8 texcoords
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		sampler2D _MainTex;
		sampler2D _MetallicTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutputStandard o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = 0;
			o.Alpha = 0;
			o.Smoothness = c.a;
			o.Smoothness = 0;
			o.Metallic = tex2D (_MetallicTex, IN.uv_MainTex).r;
			o.Metallic = 0;
			o.Albedo = 0;
		}

		ENDCG
	}

	//FallBack "Diffuse"
}
