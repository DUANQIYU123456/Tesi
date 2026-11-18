Shader "Xiaoduan/Room3/Archive textile"
{
 Properties{_BaseColor("Dyed cotton",Color)=(.12,.13,.12,1) _WeaveScale("Weave",Float)=170 _Wear("Handling wear",Range(0,1))=.35}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 Cull Off
 Pass{
  Name "ForwardLit" Tags{"LightMode"="UniversalForwardOnly"}
  HLSLPROGRAM
  #pragma vertex Vert
  #pragma fragment Frag
  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
  #pragma multi_compile_fragment _ _SHADOWS_SOFT
  #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
  #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  CBUFFER_START(UnityPerMaterial)
  half4 _BaseColor;float _WeaveScale;half _Wear;
  CBUFFER_END
  struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
  struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;};
  V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;return o;}
  half4 Frag(V i, FRONT_FACE_TYPE front:FRONT_FACE_SEMANTIC):SV_Target{
   float2 p=i.uv*_WeaveScale;float2 aa=fwidth(p);float fine=saturate(1-max(aa.x,aa.y)*1.5);
   float yarn=sin(p.x*6.28318)*sin(p.y*6.28318)*.022*fine;
   float grain=sin(i.uv.x*43+sin(i.uv.y*29))*sin(i.uv.y*35+i.uv.x*17)*.035;
   float handling=(sin(i.uv.y*7+i.uv.x*3)*.5+.5)*_Wear*.11;
   SurfaceData s=(SurfaceData)0;s.albedo=_BaseColor.rgb*(1+yarn+grain-handling);s.alpha=1;s.metallic=0;s.smoothness=.065;s.occlusion=1;
   InputData d=(InputData)0;d.positionWS=i.w;d.normalWS=normalize(i.n)*IS_FRONT_VFACE(front,1,-1);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=SampleSH(d.normalWS);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=half4(1,1,1,1);
   return UniversalFragmentPBR(d,s);
  }
  ENDHLSL
 }
 Pass{
  Name "ShadowCaster" Tags{"LightMode"="ShadowCaster"} ColorMask 0 ZWrite On
  HLSLPROGRAM
  #pragma vertex Vert
  #pragma fragment Frag
  #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
  float3 _LightDirection;float3 _LightPosition;
  float4 Vert(float3 p:POSITION,float3 n:NORMAL):SV_POSITION{
   float3 w=TransformObjectToWorld(p),normal=TransformObjectToWorldNormal(n);
   #if _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 l=normalize(_LightPosition-w);
   #else
   float3 l=_LightDirection;
   #endif
   float4 c=TransformWorldToHClip(ApplyShadowBias(w,normal,l));
   #if UNITY_REVERSED_Z
   c.z=min(c.z,c.w*UNITY_NEAR_CLIP_VALUE);
   #else
   c.z=max(c.z,c.w*UNITY_NEAR_CLIP_VALUE);
   #endif
   return c;
  }
  half4 Frag():SV_Target{return 0;}
  ENDHLSL
 }
 Pass{
  Name "DepthOnly" Tags{"LightMode"="DepthOnly"} ColorMask 0 ZWrite On
  HLSLPROGRAM
  #pragma vertex Vert
  #pragma fragment Frag
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  float4 Vert(float3 p:POSITION):SV_POSITION{return TransformObjectToHClip(p);}
  half4 Frag():SV_Target{return 0;}
  ENDHLSL
 }
 }
}
