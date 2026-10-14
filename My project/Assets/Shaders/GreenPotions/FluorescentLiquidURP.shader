Shader "Xiaoduan/Fluorescent Liquid URP"
{
    Properties
    {
        _TopColor("Surface Green", Color) = (0.18,0.6,0.025,1)
        _BottomColor("Deep Green", Color) = (0.015,0.15,0.004,1)
        [HDR] _GlowColor("Fluorescent Green", Color) = (0.12,1,0.025,1)
        _EmissionStrength("Emission Strength", Float) = 0.08
        _Size("Liquid Height (World)", Float) = 1
        _WaterLine("Fill Amount", Range(0,1)) = 0.62
        _WaveIntensity("Movement", Float) = 0
        [HideInInspector] _Stencil("Legacy Compatibility", Int) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Transparent-10" }
        Pass
        {
            Name "Liquid"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor, _BottomColor, _GlowColor;
                float _EmissionStrength, _Size, _WaterLine, _WaveIntensity;
                float _Stencil;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float relativeY : TEXCOORD2;
                half fog : TEXCOORD3;
            };
            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 originWS = TransformObjectToWorld(float3(0,0,0));
                float height = max(_Size, 0.0001);
                float surface = originWS.y + height * (_WaterLine - 0.5);
                float wave = (sin(positionWS.x * 9 + _Time.y * 1.1) +
                    cos(positionWS.z * 11 + _Time.y * 0.9)) * 0.003 * (0.25 + _WaveIntensity);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                if (positionWS.y > surface)
                {
                    positionWS.y = surface + wave;
                    normalWS = float3(0,1,0);
                }
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.relativeY = (positionWS.y - originWS.y) / height + 0.5;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                clip(_WaterLine - 0.001);
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half gradient = saturate(input.relativeY / max(_WaterLine,0.001));
                half3 baseColor = lerp(_BottomColor.rgb, _TopColor.rgb, gradient);
                Light main = GetMainLight();
                half diffuse = saturate(dot(normal, main.direction));
                half specular = pow(saturate(dot(normal, normalize(main.direction + view))), 64);
                half rim = pow(1 - saturate(dot(normal, view)), 3);
                half3 color = baseColor * (0.12 + SampleSH(normal) + main.color * diffuse * 0.4);
                color += main.color * specular * 0.4;
                color += _GlowColor.rgb * _EmissionStrength * (0.65 + 0.25 * rim + 0.1 * gradient);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
