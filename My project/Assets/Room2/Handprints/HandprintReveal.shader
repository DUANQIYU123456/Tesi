Shader "Xiaoduan/Environment/Fluorescent Handprint"
{
 Properties {
  _MainTex("Original supplied handprint",2D)="white"{}
  _UVRect("Image region",Vector)=(.015,.025,.97,.94)
  [HDR] _Tint("Isotope green",Color)=(.09,1,.22,1)
  _Glow("Fluorescence",Range(0,5))=1.8
  _Opacity("Visibility",Range(0,1))=.82
  _Mirror("Mirror",Float)=0
  _Wear("Surface wear",Range(0,1))=0
  _Seed("Wear seed",Float)=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+5"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off Cull Off Offset -1,-1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _UVRect;half4 _Tint;half _Glow,_Opacity,_Mirror,_Wear,_Seed;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;};
   V Vert(A a){V o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);o.positionCS=p.positionCS;o.uv=a.uv;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Grain(float2 p){float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);}
   half4 Frag(V i):SV_Target {
    float2 uv=i.uv;uv.x=lerp(uv.x,1-uv.x,_Mirror);
    half gray=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,_UVRect.xy+uv*_UVRect.zw).r;
    half ink=smoothstep(.055,.94,1-gray);
    // Two scales of erosion keep palm ridges legible while breaking up flat pools of ink.
    float fine=Grain(uv*float2(145,193)+_Seed);
    float patch=Grain(uv*float2(17,23)+_Seed*1.73);
    half erosion=smoothstep(_Wear*.72,_Wear*.72+.19,fine*.62+patch*.38);
    half grain=lerp(1,lerp(.60,1,fine)*lerp(.80,1,patch),saturate(_Wear*1.5));
    half a=ink*_Opacity*_Tint.a*lerp(1,erosion,saturate(_Wear*2))*grain;clip(a-.008);
    return half4(MixFog(_Tint.rgb*_Glow*lerp(.55,1,ink)*lerp(1,lerp(.72,1.05,fine),saturate(_Wear*2)),i.fog),a);
   }
   ENDHLSL
  }
 }
}
