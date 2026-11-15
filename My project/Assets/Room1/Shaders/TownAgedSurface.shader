Shader "Xiaoduan/Environment/Town Aged Surface" {
 Properties{_BaseColor("Color",Color)=(.2,.1,.04,1) _Metallic("Metal",Range(0,1))=0 _Smoothness("Polish",Range(0,1))=.3 _Wood("Wood grain",Range(0,1))=1}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass{Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)half4 _BaseColor;half _Metallic,_Smoothness,_Wood;CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;float3 n:TEXCOORD1;float3 local:TEXCOORD2;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.ws=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.local=a.p.xyz;return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 half4 frag(V i):SV_Target{
 float3 p=i.local;float grain=sin(p.z*1730+p.y*1167+sin(p.x*19+p.z*78)*.8+sin(p.x*7)*1.2);
 float pore=hash(floor(i.ws*670));float streak=smoothstep(.83,.98,grain)*_Wood;
 float variation=lerp(.95,1.05,pore);float wear=pow(saturate(abs(p.y)*2),14)*.04*_Wood;
 SurfaceData s=(SurfaceData)0;s.albedo=_BaseColor.rgb*(variation-streak*.08)+wear*half3(.16,.12,.075);s.metallic=_Metallic;s.smoothness=saturate(_Smoothness-(1-pore)*.08);s.occlusion=1;s.alpha=1;s.normalTS=half3(0,0,1);s.emission=s.albedo*.08;
 InputData d=(InputData)0;d.positionWS=i.ws;d.normalWS=normalize(i.n);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.ws);d.shadowCoord=TransformWorldToShadowCoord(i.ws);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=half4(1,1,1,1);
 return UniversalFragmentPBR(d,s);}
 ENDHLSL
 }
 Pass{Tags{"LightMode"="ShadowCaster"}ZWrite On ZTest LEqual ColorMask 0
 HLSLPROGRAM
 #pragma vertex ShadowVert
 #pragma fragment DepthFrag
 #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 float3 _LightDirection,_LightPosition;
 struct ShadowInput{float4 p:POSITION;float3 n:NORMAL;};
 float4 ShadowVert(ShadowInput i):SV_POSITION{float3 p=TransformObjectToWorld(i.p.xyz),n=TransformObjectToWorldNormal(i.n);float3 light=_LightDirection;
 #if _CASTING_PUNCTUAL_LIGHT_SHADOW
 light=normalize(_LightPosition-p);
 #endif
 float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,light));
 #if UNITY_REVERSED_Z
 clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
 #else
 clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
 #endif
 return clip;}
 half4 DepthFrag():SV_Target{return 0;}
 ENDHLSL
 }
 Pass{Tags{"LightMode"="DepthOnly"}ZWrite On ColorMask R
 HLSLPROGRAM
 #pragma vertex DepthVert
 #pragma fragment DepthFrag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 DepthVert(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}
 half4 DepthFrag():SV_Target{return 0;}
 ENDHLSL
 }
 } }
