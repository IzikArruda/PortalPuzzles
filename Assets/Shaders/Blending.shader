Shader "Blending" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white"
		_BlendTex("Blend (RGB)", 2D) = "black"
		_CameraPos("CamPos", Vector) = (0, 0, 0, 0)
		_BlendDistance("BlendDistance", Float) = 0
		_BlendRate ("BlendRate", Float) = 0
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
		float3 _CameraPos;
		float _BlendDistance;
		float _BlendRate;

		//Before the textures are used, alter the second texture so that it is the same as the first texture



		void surf(Input IN, inout SurfaceOutput o) {
			half3 col;

			/* Cause the blending texture to have a larger scale */
			//Use the base blend texture
			//col = tex2D(_MainTex, IN.uv_MainTex);
			//Use the blend texture but stretched. make the 0.25 negative if you want to flip it. multiply the whole thing to saturate it.
			//col = tex2D(_BlendTex, IN.uv_BlendTex * 0.25).rgb;
			//Mix the blend terrain with itself at a larger scale, flipped and saturated
			col = tex2D(_BlendTex, IN.uv_BlendTex).rgb * tex2D(_BlendTex, IN.uv_BlendTex * -0.25).rgb * 4;


			/* Get the distance between the camera's position and the pixel's point */
			float d = distance(_CameraPos, IN.worldPos);
			float dN = 1 - saturate((d - _BlendDistance) / (_BlendRate));

			//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb*(dN) +tex2D(_BlendTex, IN.uv_BlendTex).rgb*(1 - dN);
			o.Albedo = col;
		}

		ENDCG
	}
}