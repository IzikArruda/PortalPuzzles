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
			o.Albedo = c.rgb;
			o.Alpha = 1;
			o.Smoothness = c.a;
			o.Metallic = tex2D (_MetallicTex, IN.uv_MainTex).r;

			/* Apply a blending effect to the texture */
			//Get a second texture of the main texture
			half3 blend = tex2D(_MainTex, IN.uv_MainTex * 0.2).rgb;
			/* Get the distance between the camera's position and the pixel's point */
			//float d = distance(_CameraPos, IN.worldPos);
			//float dN = 1 - saturate((d - _BlendDistance) / (_BlendRate));
			/* Change the output depending on how far from the  */
			//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb*(dN)+blend*(1 - dN);
			o.Albedo = blend;
		}

		ENDCG
	}

	FallBack "Diffuse"
}
