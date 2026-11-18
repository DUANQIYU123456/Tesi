Shader "Xiaoduan/Room3/Archive ceiling"
{
 Properties{_BaseColor("Radiance",Color)=(0.75,0.008,0.004,1)}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   CBUFFER_END
   V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
   half4 Frag(V i):SV_Target
   {
    float2 uv=i.uv;float edge=min(min(uv.x,1-uv.x),min(uv.y,1-uv.y));
    float vignette=lerp(.12,1,smoothstep(0,.26,edge));
    float d=length((uv-float2(.5,.62))*float2(1.1,.75));
    float field=lerp(.36,1,saturate(1-d));
    float grain=frac(sin(dot(floor(uv*float2(1600,1300)),float2(12.9898,78.233)))*43758.5453);
    float rib=.97+.03*sin(uv.x*1700);
    return half4(_BaseColor.rgb*vignette*field*(.96+grain*.04)*rib,1);
   }
   ENDHLSL
  }
 }
}
