Shader "Custom/Unlit" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	}

	SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Unlit

		struct Input {
			float2 uv_MainTex;
		};

		sampler2D _MainTex;

		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
		{
			return half4(s.Albedo, s.Alpha);
		}

		void surf(Input IN, inout SurfaceOutput o) {
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		}

		ENDCG
	}
}