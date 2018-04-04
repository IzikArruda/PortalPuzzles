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

		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma vertex vert
		#pragma surface surf Unlit noambient

		struct Input {
			float2 uv_MainTex;
			float blendUV;
			float flipUV;
		};
		sampler2D _MainTex;
		sampler2D _SecondTex;

		/* Get the UV2 from the mesh */
		void vert(inout appdata_full v, out Input o){
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.blendUV = v.texcoord1.x;
			o.flipUV = v.texcoord1.y;
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
			/* Flip the blend/texture if the UV's Y value is negative */
			float blend = (IN.flipUV < 0) ? (1 - IN.blendUV) : IN.blendUV;

			o.Albedo = blend*tex2D(_SecondTex, IN.uv_MainTex) + (1 - blend)*tex2D(_MainTex, IN.uv_MainTex);
		}
		ENDCG
	}
}
