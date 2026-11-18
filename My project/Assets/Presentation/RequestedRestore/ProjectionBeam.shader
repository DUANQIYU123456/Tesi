Shader "Xiaoduan/Room2/Projection Beam"
{
 Properties{[HDR]_Tint("Light",Color)=(.12,1,.38,.10)}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags{"LightMode"="SRPDefaultUnlit"}
   Blend SrcAlpha One Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)half4 _Tint;CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
   half4 Frag(V i):SV_Target{float soft=pow(saturate(sin(i.uv.x*3.14159265)),2);float depth=smoothstep(0,.10,i.uv.y)*(1-smoothstep(.82,1,i.uv.y));return half4(_Tint.rgb,_Tint.a*soft*depth);}
   ENDHLSL
  }
 }
}
