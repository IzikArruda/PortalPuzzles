Shader "Unlit/PuzzleWallShader"
{
	Properties{
		_MainTex ("Primary Texture", 2D) = "white" {}
		_SecondTex("Secondary Texture", 2D) = "white" {}
		_RepeatingNoiseTex("Repeating Noise Texture", 2D) = "white" {}
		_TeleportHeight("Room Teleport Height", float) = 0
	}

	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 100

		//ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
		#pragma vertex vert
		#pragma surface surf Unlit noambient

		struct Input {
			float2 uv_MainTex;
			float2 uv2_SecondTex;
			float2 uv3_RepeatingNoiseTex;
			float gradientUV;
			float gradPrioritySecondTex;
			float vertYPos;
		};
		sampler2D _MainTex;
		sampler2D _SecondTex;
		sampler2D _RepeatingNoiseTex;
		float _TeleportHeight;

		/* Get the UV2, UV3 and UV4 from the mesh */
		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.gradientUV = v.texcoord3.y;
			o.gradPrioritySecondTex = v.texcoord3.x;
			o.vertYPos = v.vertex.y;
		}

		/* Remove the lighting from the texture */
		half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten) {
			half4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}

		/* Adjust the color tint of the given fixed3 texture relative to the given values */
		fixed3 AdjustColorTint(fixed3 tex, float vertHeight, float teleHeight) {
			
			float invertAmount = abs(vertHeight) / teleHeight;
			/* If the value goes above 1, make it retrack by it's overflow amount */
			invertAmount = saturate(invertAmount) - saturate(invertAmount - 1);
			/* Apply a color tint to the texture */
			tex.r = 1*(1 - invertAmount) + 0*invertAmount;
			tex.g = 1*(1 - invertAmount) + 0*invertAmount;
			tex.b = 1*(1 - invertAmount) + 0*invertAmount;

			return tex;
		}

		/* Use the UV's X value to control the texture of the surface */
		void surf(Input IN, inout SurfaceOutput o) {
			//The UV scale of the gradient. Larger value means the gradient changes faster.
			float gradScale;
			//The power of the gradient's direct value (default 0). 1 is only use grad, -1 is never use grad.
			float gradPriority;
			fixed3 gradient;

			/* Adjust the main texture to use the gradient to make it seem not as obviously tilled */
			/* Gradient controls how hard and often the re-scaled version of the texture is used */
			fixed3 tex1 = AdjustColorTint(tex2D(_MainTex, IN.uv_MainTex), IN.vertYPos, _TeleportHeight);
			float largerTexUVScale = -0.2;
			gradPriority = 0.15;
			gradScale = 2;
			gradient = saturate(tex2D(_RepeatingNoiseTex, gradScale*IN.uv3_RepeatingNoiseTex) + gradPriority);
			tex1 = tex1*saturate(1 - (gradient)) + AdjustColorTint(tex2D(_MainTex, largerTexUVScale*IN.uv_MainTex), IN.vertYPos, _TeleportHeight)*saturate(gradient);

			/* The second texture is multiplied by the gradient texture and the gradientUV to smoothly fade between the textures */
			/* Gradient controls how hard and often the second texture is used above the main texture/wall */
			fixed3 tex2 = tex2D(_SecondTex, IN.uv2_SecondTex);
			gradPriority = IN.gradPrioritySecondTex;
			gradScale = 0.2;
			gradient = tex2D(_RepeatingNoiseTex, gradScale*IN.uv3_RepeatingNoiseTex) + gradPriority;
			tex2 = tex1*saturate(1 - (gradient)) + tex2*saturate(gradient);

			/* Depending on gradientUV, blend between the mainTex and SecondTex */
			float blend = saturate(IN.gradientUV);
			o.Albedo = blend*tex1 + (1 - blend)*tex2;
		}
		ENDCG
	}
}
