Shader "Xiaoduan/Room3/Clinical beam"
{
 Properties{_Tint("Clinical white",Color)=(.88,1,1,1) _Strength("Strength",Float)=.12}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent"}
  Pass
  {
   Blend SrcAlpha One ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;};
   CBUFFER_START(UnityPerMaterial)
   half4 _Tint;float _Strength;
   CBUFFER_END
   V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;return o;}
   half4 Frag(V i):SV_Target
   {
    float facing=pow(abs(dot(normalize(i.n),normalize(GetWorldSpaceViewDir(i.w)))),2);
    float fade=smoothstep(0,.12,i.uv.y)*(1-smoothstep(.9,1,i.uv.y));
    float grain=.9+.1*sin(i.w.y*6+sin(i.w.x*9)+i.w.z*5);
    return half4(_Tint.rgb,_Strength*facing*fade*grain);
   }
   ENDHLSL
  }
 }
}
