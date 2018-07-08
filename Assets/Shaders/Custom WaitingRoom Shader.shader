﻿Shader "Unlit/Custom WaitingRoom Shader"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_EntrTint("Entrance Tint", Vector) = (.0, .0, .0)
		_ExitTint("Exit Tint", Vector) = (.0, .0, .0)
		_RoomCenter("Room Z Center", float) = 0
		_RoomDepthBuffer("Room Z Depth", float) = 0
		_TextureZLengthEntr("Texture Z Length Entrance", float) = 0
		_TextureZLengthExit("Texture Z Length Exit", float) = 0
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma vertex vert
		#pragma surface surf Unlit noambient

		struct Input {
			float2 uv_MainTex;
			float zPos;
		};
		sampler2D _MainTex;
		float3 _EntrTint;
		float3 _ExitTint;
		float _RoomCenter;
		float _RoomDepthBuffer;
		float _TextureZLengthEntr;
		float _TextureZLengthExit;

		/* Get the UV2 from the mesh */
		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.zPos = mul(unity_ObjectToWorld, v.vertex).z;
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

			/* Get whether we are using the entrance or exit lengths and tints */
			fixed isNegative = saturate(sign(IN.zPos - _RoomCenter));
			fixed length = _TextureZLengthExit*isNegative + _TextureZLengthEntr*(1 - isNegative);
			fixed3 tint = _ExitTint*isNegative + _EntrTint*(1 - isNegative);

			/* Adjust the texture relative to how far the pixel is relative to the room center */
			fixed blendRatio = saturate(((abs(IN.zPos - _RoomCenter) - _RoomDepthBuffer)/ length));

			/* Adjust the texture relative to the blend amount and the color tint */
			fixed3 tex = tex2D(_MainTex, IN.uv_MainTex);
			tex.r = tex.r + blendRatio*tint.r;
			tex.g = tex.g + blendRatio*tint.g;
			tex.b = tex.b + blendRatio*tint.b;

			o.Albedo = tex;
		}
		ENDCG
	}
}