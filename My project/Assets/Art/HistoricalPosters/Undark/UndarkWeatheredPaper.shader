Shader "Xiaoduan/Environment/Weathered Archive Paper"
{
 Properties {
  _PosterMap("Original historical print",2D)="white"{}
  _WearMap("Existing town tear atlas",2D)="white"{}
  _PaperTint("Aged paper tint",Color)=(.83,.808,.701,1)
  _Cutoff("Tear cutoff",Range(0,1))=.1
  _WearRect("Tear atlas crop",Vector)=(.084,.019,.386,.452)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Cull Off ZWrite On Offset -1,-1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_PosterMap); SAMPLER(sampler_PosterMap);
   TEXTURE2D(_WearMap); SAMPLER(sampler_WearMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _PaperTint; float4 _WearRect; float _Cutoff;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
   V Vert(A a){V o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.uv=a.uv;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   float Grain(float2 p){return frac(sin(dot(floor(p),float2(127.1,311.7)))*43758.5453);}
   half4 Frag(V i):SV_Target {
    float2 wearUV=lerp(_WearRect.xy,_WearRect.zw,i.uv);
    half alpha=SAMPLE_TEXTURE2D(_WearMap,sampler_WearMap,wearUV).a;
    clip(alpha-_Cutoff);
    half ink=SAMPLE_TEXTURE2D(_PosterMap,sampler_PosterMap,i.uv).r;
    half grain=Grain(i.uv*950);
    half stain=lerp(.86,1,Grain(i.uv*float2(21,30)));
    half edge=1-smoothstep(0,.05,min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y)));
    half3 paper=_PaperTint.rgb*lerp(.90,1.02,grain)*stain*(1-edge*.12);
    half3 albedo=paper*lerp(.13,.98,ink);
    half3 n=normalize(i.normalWS);
    Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half3 illumination=SampleSH(n)+main.color*saturate(dot(n,main.direction))*main.distanceAttenuation*main.shadowAttenuation;
    return half4(MixFog(albedo*illumination,i.fog),1);
   }
   ENDHLSL
  }
 }
}
