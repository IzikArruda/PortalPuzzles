Shader "Custom/Unlit" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	_BoxPos("BoxPos", Vector) = (0, 0, 0, 0)
	}

		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Unlit

		struct Input {
		float2 uv_MainTex;
		float3 worldPos;
	};
	sampler2D _MainTex;
	float3 _BoxPos;

	half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
	{
		return half4(s.Albedo, s.Alpha);
	}

	void surf(Input IN, inout SurfaceOutput o) {
		half3 blend = 0;

		/* Get the distance between the center of the world and the pixel being rendered */
		float d = distance(_BoxPos, IN.worldPos);
		float dN = 1 - saturate((d - 1) / (1));



		/* step(x, y) implements x < y */

		//So: if the pixel rendered is on the top of the box, render it black. Top tex will be 1 if we use the top texture, 0 if we dont
		float boxHeight = 1;
		int topTex = step(boxHeight, abs(IN.worldPos.y));


		o.Albedo = tex2D(_MainTex, IN.uv_MainTex) * topTex;
		//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb*(dN) + blend*(1 - dN);
	}

	ENDCG
	}
}