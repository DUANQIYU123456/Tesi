Shader "Xiaoduan/Environment/Footprint Cutout"
{
 Properties {
  _Sheet("Original footprint sheet",2D)="white"{}
  _UVRect("Individual footprint region",Vector)=(0,0,1,1)
  [HDR] _Tint("Footprint color",Color)=(.02,.95,.12,1)
  _Glow("Glow strength",Range(0,5))=1.7
  _Opacity("Opacity",Range(0,1))=1
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   Cull Off ZWrite Off Offset -1,-1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_Sheet); SAMPLER(sampler_Sheet);
   CBUFFER_START(UnityPerMaterial)
    float4 _UVRect;half4 _Tint;half _Opacity;half _Glow;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
   V Vert(A a){V o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.uv=a.uv;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 Frag(V i):SV_Target {
    half3 source=SAMPLE_TEXTURE2D(_Sheet,sampler_Sheet,_UVRect.xy+saturate(i.uv)*_UVRect.zw).rgb;
    // Remove white paper rather than drawing a white rectangular card.
    half coverage=saturate((source.r-min(source.g,source.b))/max(source.r,.005));
    coverage=smoothstep(.025,.95,coverage)*_Opacity*_Tint.a;
    clip(coverage-.005);
    half3 normal=normalize(i.normalWS);
    Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half3 lighting=SampleSH(normal)+main.color*saturate(dot(normal,main.direction))*main.distanceAttenuation*main.shadowAttenuation;
    #ifdef _ADDITIONAL_LIGHTS
    for(uint n=0;n<GetAdditionalLightsCount();n++){Light light=GetAdditionalLight(n,i.positionWS);lighting+=light.color*saturate(dot(normal,light.direction))*light.distanceAttenuation;}
    #endif
    half variation=lerp(.65,1,source.r);
    return half4(MixFog(_Tint.rgb*variation*(lighting+_Glow),i.fog),coverage);
   }
   ENDHLSL
  }
 }
}
