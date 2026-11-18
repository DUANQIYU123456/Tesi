Shader "Xiaoduan/Room2/Archive Projection"
{
 Properties { _Hologram_Texture("Original reference",2D)="white"{} _Tint("Silver green",Color)=(.78,.85,.80,1) _Opacity("Density",Range(0,1))=.9 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_Hologram_Texture);SAMPLER(sampler_Hologram_Texture);
   CBUFFER_START(UnityPerMaterial) float4 _Tint;float _Opacity;CBUFFER_END
   float _Room2ProjectionMotion;
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   V Vert(A v){V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;return o;}
   half4 Frag(V i):SV_Target
   {
    half4 photo=SAMPLE_TEXTURE2D(_Hologram_Texture,sampler_Hologram_Texture,(i.uv-.018)/.964);
    float rows=.97+.03*sin(i.uv.y*780);
    float trace=pow(saturate(1-abs(frac(i.uv.y+_Time.y*.055*_Room2ProjectionMotion)-.5)*65),2);
    half3 ink=photo.rgb*_Tint.rgb*rows+trace*float3(.025,.065,.04);
    float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
    float photograph=smoothstep(.015,.019,edge);
    half3 paper=half3(.48,.50,.46);
    return half4(lerp(paper,ink,photograph),_Opacity);
   }
   ENDHLSL
  }
 }
}
