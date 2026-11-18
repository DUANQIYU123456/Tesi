Shader "Xiaoduan/Room3/Local mist"
{
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 col:COLOR;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 col:COLOR;float depth:TEXCOORD1;};
   V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.depth=-TransformWorldToView(TransformObjectToWorld(i.p.xyz)).z;o.uv=i.uv;o.col=i.col;return o;}
   half4 Frag(V i):SV_Target
   {
    float2 q=(i.uv-.5)*2;float radial=pow(saturate(1-dot(q,q)),2);
    float depth=LinearEyeDepth(SampleSceneDepth(i.p.xy/_ScaledScreenParams.xy),_ZBufferParams);
    float soft=saturate((depth-i.depth)/1.1)*saturate(i.depth/.4);
    return half4(i.col.rgb,i.col.a*radial*soft);
   }
   ENDHLSL
  }
 }
}
