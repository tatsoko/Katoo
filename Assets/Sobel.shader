Shader "Hidden/Sobel"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_EdgeColor("Edge Color", Color) = (1, 1, 1, 1)
		_Threshold("Edge Threshold", Range(0, 1)) = 0.2
	}

		SubShader
		{
			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				struct appdata_t
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				sampler2D _MainTex;
				float4 _EdgeColor;
				float _Threshold;

				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					// Sample the input texture
					fixed4 col = tex2D(_MainTex, i.uv);

				// Calculate the Sobel edge detection
				float3 gx = tex2D(_MainTex, i.uv + float2(-1.0 / 256.0, -1.0 / 256.0)).rrr * -1 +
							tex2D(_MainTex, i.uv + float2(1.0 / 256.0, -1.0 / 256.0)).rrr +
							tex2D(_MainTex, i.uv + float2(-1.0 / 256.0,  0.0 / 256.0)).rrr * -2 +
							tex2D(_MainTex, i.uv + float2(1.0 / 256.0,  0.0 / 256.0)).rrr * 2 +
							tex2D(_MainTex, i.uv + float2(-1.0 / 256.0,  1.0 / 256.0)).rrr * -1 +
							tex2D(_MainTex, i.uv + float2(1.0 / 256.0,  1.0 / 256.0)).rrr;

				float3 gy = tex2D(_MainTex, i.uv + float2(-1.0 / 256.0, -1.0 / 256.0)).rrr * -1 +
							tex2D(_MainTex, i.uv + float2(1.0 / 256.0, -1.0 / 256.0)).rrr * -2 +
							tex2D(_MainTex, i.uv + float2(-1.0 / 256.0,  0.0 / 256.0)).rrr * -1 +
							tex2D(_MainTex, i.uv + float2(1.0 / 256.0,  0.0 / 256.0)).rrr +
							tex2D(_MainTex, i.uv + float2(-1.0 / 256.0,  1.0 / 256.0)).rrr +
							tex2D(_MainTex, i.uv + float2(1.0 / 256.0,  1.0 / 256.0)).rrr * 2;

				float3 gradient = sqrt(gx * gx + gy * gy);
				// Apply the edge threshold
				float3 edgeColor =   step(0.6, tex2D(_MainTex, i.uv).rgb);

				// Output the final color
				return fixed4(edgeColor, 1);
			}
			ENDCG
		}
		}
}
