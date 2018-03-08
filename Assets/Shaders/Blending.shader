Shader "Blending" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white"
		_BlendTex("Blend (RGB)", 2D) = "black"
		_CameraPos("CamPos", Vector) = (0, 0, 0, 0)
		_Radius ("Radius", Float) = 1
	}

	SubShader{
		CGPROGRAM
		#pragma surface surf Lambert

		struct Input {
			float2 uv_MainTex;
			float2 uv_BlendTex;
			float3 worldPos;
		};
		sampler2D _MainTex;
		sampler2D _BlendTex;
		float3 _CamPos;
		float _Radius;

		void surf(Input IN, inout SurfaceOutput o) {
			/* Get the distance between the camera's position and the pixel's point */
			float d = distance(_CamPos, IN.worldPos);
			float dN = 1 - saturate(d / _Radius);

			dN = step(dN, 0);
			//dN = step(dN, 1);
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb*(dN) +tex2D(_BlendTex, IN.uv_BlendTex).rgb*(1 - dN);
		}

		ENDCG
	}
}