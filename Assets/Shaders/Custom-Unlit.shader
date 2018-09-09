Shader "Unlit/Custom-Unlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SecondTex("Texture", 2D) = "white" {}
		_RoundRange("Round Range", Float) = 0.001
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
		float _RoundRange;

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
		
		half4 ClampRanges(half4 rgb, float range){
			/*
			 * Given a texture, round each rbg value to a range of values
			 */
			 
			rgb.r = round(rgb.r / range) * (range);
			rgb.g = round(rgb.g / range) * (range);
			rgb.b = round(rgb.b / range) * (range);

			return rgb;
		}

		/* Use the UV's X value to control the texture of the surface */
		void surf(Input IN, inout SurfaceOutput o) {
			/* Flip the blend/texture if the UV's Y value is negative */
			float blend = (IN.flipUV < 0) ? (1 - IN.blendUV) : IN.blendUV;
			
			half4 t1 = blend*ClampRanges(tex2D(_SecondTex, IN.uv_MainTex), _RoundRange);
			half4 t2 = (1 - blend)*ClampRanges(tex2D(_MainTex, IN.uv_MainTex), _RoundRange);
			o.Albedo = t1 + t2;
		}
		ENDCG
	}
}
