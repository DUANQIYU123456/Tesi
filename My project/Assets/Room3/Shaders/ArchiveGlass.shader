Shader "Xiaoduan/Room3/Wired Glass"
{
 Properties { _BaseColor("Glass tint",Color)=(.12,.23,.23,.12) }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  Cull Off
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
  CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
  struct A { float4 p:POSITION; }; struct V {float4 p:SV_POSITION; float3 w:TEXCOORD0;};
  V vert(A i){V o;o.w=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.w);return o;}
  half4 frag(V i):SV_Target {
   float2 uv=i.w.zy*1.65;float2 d=abs(frac(uv-.5)-.5)/max(fwidth(uv),.001);float gridLine=1-saturate(min(d.x,d.y));
   float2 screenUV=GetNormalizedScreenSpaceUV(i.p);
   half3 frosted=(SampleSceneColor(screenUV)+SampleSceneColor(screenUV+float2(.005,0))+SampleSceneColor(screenUV-float2(.005,0))+SampleSceneColor(screenUV+float2(0,.005))+SampleSceneColor(screenUV-float2(0,.005)))*.2;
   half3 tint=lerp(frosted,_BaseColor.rgb,.3);
   return half4(lerp(tint,half3(.28,.38,.37),gridLine),max(.32,gridLine*.5));
  }
  ENDHLSL
 }
 }
}
