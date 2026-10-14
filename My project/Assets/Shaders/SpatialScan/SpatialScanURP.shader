Shader "Xiaoduan/Spatial Scan URP"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "SpatialScan"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ScanFragment
            #pragma target 3.5
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _XDScanOrigin, _XDScanDirection, _XDScanColor;
            float _XDScanTravel, _XDScanBand, _XDScanTail, _XDGridSize, _XDGridThickness;
            float _XDScanFade, _XDScanShock;

            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float2 Grid(float2 position)
            {
                float2 p = position / max(_XDGridSize,0.1);
                float2 edge = abs(frac(p - 0.5) - 0.5);
                float2 aa = max(fwidth(p),0.0001);
                // Fractional pixel coverage keeps the bright core narrow at oblique angles.
                float2 halfWidth = max(_XDGridThickness,aa * 0.18);
                float2 lines = saturate((halfWidth + aa * 0.5 - edge) / aa);
                float2 haloWidth = max(halfWidth * 3.5,aa * 0.85);
                float2 halo = exp(-edge * edge / (haloWidth * haloWidth));
                float resolved = 1 - smoothstep(0.35,0.8,max(aa.x,aa.y));
                return float2(max(lines.x,lines.y),max(halo.x,halo.y)) * resolved;
            }
            half4 ScanFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 sourceUV = input.texcoord;
                float2 pixelUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float depth = SampleSceneDepth(pixelUV);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,sourceUV);
                #if UNITY_REVERSED_Z
                    bool sky = depth < 0.00001;
                #else
                    bool sky = depth > 0.99999;
                #endif
                if (!sky)
                {
                    float3 world = ComputeWorldSpacePosition(pixelUV,depth,UNITY_MATRIX_I_VP);
                    float3 sampledNormal = SampleSceneNormals(pixelUV);
                    if (dot(sampledNormal,sampledNormal) < 0.01)
                        sampledNormal = cross(ddx(world),ddy(world));
                    float3 normal = abs(normalize(sampledNormal));
                    // Blend projection axes instead of switching abruptly at every small facet.
                    float3 weights = pow(normal,8);
                    weights /= max(weights.x + weights.y + weights.z,0.0001);
                    float2 grid = Grid(world.yz) * weights.x + Grid(world.xz) * weights.y + Grid(world.xy) * weights.z;
                    float architecture = lerp(0.4,1,smoothstep(0.72,0.98,max(normal.x,max(normal.y,normal.z))));
                    float coordinate = dot(world - _XDScanOrigin.xyz, _XDScanDirection.xyz);
                    float behind = _XDScanTravel - coordinate;
                    float front = 1 - smoothstep(0,max(_XDScanBand,0.1),abs(behind));
                    float tail = smoothstep(-0.15,0.15,behind) * exp(-max(behind,0) / max(_XDScanTail,0.1));
                    float core = grid.x * architecture * (front * 3.5 + tail * 0.3);
                    float halo = grid.y * architecture * (front * 0.18 + tail * 0.04);
                    scene.rgb += (_XDScanColor.rgb * (core + halo + front * 0.035) +
                        core * float3(0.2,0.2,0.2)) * _XDScanFade;
                }
                // Short, continuous electronic interference; chromatic separation is handled by the Volume.
                float row = Hash(float2(floor(sourceUV.y * 180),floor(_Time.y * 22)));
                float shift = (row - 0.5) * _XDScanShock * 0.008;
                half3 shifted = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,
                    saturate(sourceUV + float2(shift,0))).rgb;
                float3 noise = float3(Hash(sourceUV * 700 + _Time.y),
                    Hash(sourceUV * 590 + _Time.y * 1.3), Hash(sourceUV * 810 - _Time.y));
                scene.rgb = lerp(scene.rgb,shifted,_XDScanShock * 0.2);
                scene.rgb += (noise - 0.5) * _XDScanShock * 0.09;
                return scene;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
