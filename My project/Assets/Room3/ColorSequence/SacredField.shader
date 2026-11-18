Shader "Xiaoduan/Room3/Sacred field"
{
 Properties{_Strength("Strength",Float)=0 _Bone("Bone white",Color)=(.96,.96,.86,1) _Gold("Gold",Color)=(1,.7,.12,1)}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
  Pass
  {
   Blend One OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   CBUFFER_START(UnityPerMaterial)
   float _Strength;half4 _Bone;half4 _Gold;
   CBUFFER_END
   V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
   half4 Frag(V i):SV_Target
   {
    float2 p=(i.uv-.5)*2;float r=length(p);float a=atan2(p.y,p.x);
    float halo=exp(-r*r*2.1)*(1-smoothstep(.65,1,r));
    float core=1-smoothstep(.12,.43,r);
    float grain=frac(sin(dot(floor(i.uv*1800),float2(12.9898,78.233)))*43758.5453);
    float alpha=saturate((core+halo*.50)*_Strength);
    half3 color=lerp(_Gold.rgb,_Bone.rgb,exp(-r*r*10));
    return half4(color*(1.15+core*1.7)*alpha*(.985+grain*.015),alpha);
   }
   ENDHLSL
  }
 }
}
