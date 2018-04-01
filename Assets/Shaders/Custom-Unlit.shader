Shader "Unlit/Custom-Unlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SecondTex("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma surface surf Unlit vertex:vert

		struct Input {
			float2 uv_MainTex;
			float blendUV;
		};
		sampler2D _MainTex;
		sampler2D _SecondTex;

		/* Get the UV2 from the mesh */
		void vert(inout appdata_full v, out Input o){
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.blendUV = v.texcoord1.x;
		}

		/* Remove the lighting from the texture */
		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten) {
			half4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}

		/* Use the UV's X value to control the texture of the surface */
		void surf(Input IN, inout SurfaceOutput o) {
			float blend = IN.blendUV;

			o.Albedo = blend*tex2D(_SecondTex, IN.uv_MainTex) + (1 - blend)*tex2D(_MainTex, IN.uv_MainTex);
		}
		ENDCG
	}
}
