Shader "Nature/Terrain/Custom-Standard" {
	Properties {
		// set by terrain engine
		[HideInInspector] _Control ("Control (RGBA)", 2D) = "red" {}
		[HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
		[HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
		[HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
		[HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
		[HideInInspector] _Normal3 ("Normal 3 (A)", 2D) = "bump" {}
		[HideInInspector] _Normal2 ("Normal 2 (B)", 2D) = "bump" {}
		[HideInInspector] _Normal1 ("Normal 1 (G)", 2D) = "bump" {}
		[HideInInspector] _Normal0 ("Normal 0 (R)", 2D) = "bump" {}
		[HideInInspector] [Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0	
		[HideInInspector] [Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0	
		[HideInInspector] [Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0	
		[HideInInspector] [Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0
		[HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 1.0	
		[HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 1.0	
		[HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 1.0	
		[HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 1.0

		// used in fallback on old cards & base map
		_MainTex ("BaseMap (RGB)", 2D) = "white" {}
		[HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
	}

	SubShader {
		Tags {
			"Queue" = "Geometry-100"
			"RenderType" = "Opaque"
		}

		CGPROGRAM
		#pragma surface surf Standard vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer fullforwardshadows
		#pragma multi_compile_fog
		#pragma target 3.0
		// needs more than 8 texcoords
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		#pragma multi_compile __ _TERRAIN_NORMAL_MAP

		#define TERRAIN_STANDARD_SHADER
		#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
		#include "TerrainSplatmapCommon.cginc"

		half _Metallic0;
		half _Metallic1;
		half _Metallic2;
		half _Metallic3;
		
		half _Smoothness0;
		half _Smoothness1;
		half _Smoothness2;
		half _Smoothness3;


		/* Use a custom SplatmapMix function */
		void SplatmapMixCustom(Input IN, half4 defaultAlpha, out half4 splat_control, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal)
		{
			splat_control = tex2D(_Control, IN.tc_Control);
			weight = dot(splat_control, half4(1, 1, 1, 1));

			#if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
				clip(weight == 0.0f ? -1 : 1);
			#endif

			// Normalize weights before lighting and restore weights in final modifier functions so that the overal
			// lighting result can be correctly weighted.
			splat_control /= (weight + 1e-3f);

			mixedDiffuse = 0.0f;
			#ifdef TERRAIN_STANDARD_SHADER
				mixedDiffuse += splat_control.r * tex2D(_Splat0, IN.uv_Splat0) * half4(1.0, 1.0, 1.0, defaultAlpha.r);
				mixedDiffuse += splat_control.g * tex2D(_Splat1, IN.uv_Splat1) * half4(1.0, 1.0, 1.0, defaultAlpha.g);
				mixedDiffuse += splat_control.b * tex2D(_Splat2, IN.uv_Splat2) * half4(1.0, 1.0, 1.0, defaultAlpha.b);
				mixedDiffuse += splat_control.a * tex2D(_Splat3, IN.uv_Splat3) * half4(1.0, 1.0, 1.0, defaultAlpha.a);
			#else
				mixedDiffuse += splat_control.r * tex2D(_Splat0, IN.uv_Splat0);
				mixedDiffuse += splat_control.g * tex2D(_Splat1, IN.uv_Splat1);
				mixedDiffuse += splat_control.b * tex2D(_Splat2, IN.uv_Splat2);
				mixedDiffuse += splat_control.a * tex2D(_Splat3, IN.uv_Splat3);
			#endif

			#ifdef _TERRAIN_NORMAL_MAP
				fixed4 nrm = 0.0f;
				nrm += splat_control.r * .5 * (tex2D(_Normal0, IN.uv_Splat0) + tex2D(_Normal0, IN.uv_Splat0 * -.25));
				nrm += splat_control.g * .5 * (tex2D(_Normal1, IN.uv_Splat1) + tex2D(_Normal1, IN.uv_Splat1 * -.25));
				nrm += splat_control.b * .5 * (tex2D(_Normal2, IN.uv_Splat2) + tex2D(_Normal2, IN.uv_Splat2 * -.25));
				nrm += splat_control.a * .5 * (tex2D(_Normal3, IN.uv_Splat3) + tex2D(_Normal3, IN.uv_Splat3 * -.25));
				mixedNormal = UnpackNormal(nrm);
			#endif

			mixedDiffuse = 0.0f;
			mixedDiffuse += splat_control.r * .5 * (tex2D(_Splat0, IN.uv_Splat0) + tex2D(_Splat0, IN.uv_Splat0 * -.25));
			mixedDiffuse += splat_control.g * .5 * (tex2D(_Splat1, IN.uv_Splat1) + tex2D(_Splat1, IN.uv_Splat1 * -.25));
			mixedDiffuse += splat_control.b * .5 * (tex2D(_Splat2, IN.uv_Splat2) + tex2D(_Splat2, IN.uv_Splat2 * -.25));
			mixedDiffuse += splat_control.a * .5 * (tex2D(_Splat3, IN.uv_Splat3) + tex2D(_Splat3, IN.uv_Splat3 * -.25));
		}

		void surf (Input IN, inout SurfaceOutputStandard o) {
			half4 splat_control;
			half weight;
			fixed4 mixedDiffuse;
			half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);


			//half3 blend = tex2D(_MainTex, IN.uv_MainTex * 0.2).rgb;

			SplatmapMixCustom(IN, defaultSmoothness, splat_control, weight, mixedDiffuse, o.Normal);
			splat_control.r = 0;
			splat_control.g = 0;
			splat_control.b = 0;
			splat_control.a = 0;
			o.Albedo = mixedDiffuse.rgb;
			o.Alpha = weight;
			o.Smoothness = mixedDiffuse.a;
			o.Metallic = dot(splat_control, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));

			/* Apply a blending effect to the texture */
			//Get a second texture of the main texture
			/* Get the distance between the camera's position and the pixel's point */
			//float d = distance(_CameraPos, IN.worldPos);
			//float dN = 1 - saturate((d - _BlendDistance) / (_BlendRate));
			/* Change the output depending on how far from the  */
			//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb*(dN)+blend*(1 - dN);
			o.Albedo = mixedDiffuse.rgb;
		}
		ENDCG
	}

	Dependency "AddPassShader" = "Hidden/TerrainEngine/Splatmap/Custom-Standard-AddPass"
	Dependency "BaseMapShader" = "Hidden/TerrainEngine/Splatmap/Custom-Standard-Base"

	Fallback "Nature/Terrain/Diffuse"
}
