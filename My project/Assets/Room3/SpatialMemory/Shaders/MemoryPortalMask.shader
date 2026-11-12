// Adapted from Zbajnek/urp-portals (MIT); see LICENSE.Zbajnek.txt.
Shader "Xiaoduan/Memory/PortalMask"
{
    Properties
    {
		_MainTex("Main Texture", 2D) = "white" {}
    	_InactiveColor ("Inactive Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
		Tags 
		{ 
			"RenderType" = "Opaque"
			"Queue" = "Geometry+10"
			"RenderPipeline" = "UniversalPipeline"
		}
		Cull Off

		HLSLINCLUDE
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		ENDHLSL

        Pass
        {
			Name "Mask"

			Stencil
			{
				Ref 1
				Pass replace
			}

			HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				struct appdata
				{
					float4 vertex : POSITION;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float4 screenPos : TEXCOORD0;
				};

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = TransformObjectToHClip(v.vertex.xyz);
					o.screenPos = ComputeScreenPos(o.vertex);
					return o;
				}

				uniform sampler2D _MainTex;
				uniform float4 _InactiveColor;
				int displayMask; // set to 1 to display texture, otherwise it will draw test color

				float4 frag(v2f i) : SV_Target
				{
					float2 uv = i.screenPos.xy / i.screenPos.w;
					float4 color = tex2D(_MainTex, uv);
					return color * displayMask + _InactiveColor * (1 - displayMask);
				}
			ENDHLSL
        }
    }
}
