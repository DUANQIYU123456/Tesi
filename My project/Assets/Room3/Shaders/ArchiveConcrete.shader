Shader "Xiaoduan/Room3/World Concrete"
{
 Properties {
  _BaseMap("Concrete",2D)="white"{}
  _BaseColor("Tint",Color)=(.48,.5,.49,1)
  _TextureScale("World scale",Float)=.24
  _Smoothness("Smoothness",Range(0,1))=.18
  _Pathology("Porous corrosion",Range(0,1))=0
 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 Pass {
  Name "ForwardLit" Tags {"LightMode"="UniversalForwardOnly"}
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
  #pragma multi_compile_fragment _ _SHADOWS_SOFT
  #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
  #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
  #pragma multi_compile_fog
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
  float4 _BaseMap_ST; half4 _BaseColor; float _TextureScale; half _Smoothness; float _Pathology;
  CBUFFER_END
  float _Room3Reveal;
  float2 cellHash(float2 p){return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);}
  float cellDistance(float2 p){float2 a=floor(p),b=frac(p);float nearest=8;for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++){float2 q=float2(x,y);nearest=min(nearest,length(q+cellHash(a+q)-b));}return nearest;}
  struct A {float4 p:POSITION; float3 n:NORMAL;};
  struct V {float4 p:SV_POSITION; float3 w:TEXCOORD0; half3 n:TEXCOORD1; half fog:TEXCOORD2;};
  V vert(A i) {V o; VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);o.p=p.positionCS;o.w=p.positionWS;o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
  half4 frag(V i):SV_Target {
   half3 n=normalize(i.n);half3 b=pow(abs(n),4);b/=max(dot(b,1),.0001);
   float3 p=i.w*_TextureScale;
   half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.yz).rgb*b.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*b.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*b.z;
   float seam=max(step(.982,frac(i.w.x*.42)),step(.985,frac(i.w.z*.28)));
   c*=lerp(1,.84,seam);
   half3 emission=0;
   if(_Pathology>.01){
    float2 surface=p.yz*b.x+p.xz*b.y+p.xy*b.z;
    float pores=1-smoothstep(.035,.23,cellDistance(surface*9));
    c*=1-pores*.87*_Pathology;
    float2 lanes=abs(frac(surface*3)-.5);float trace=1-smoothstep(.012,.035,min(lanes.x,lanes.y));
    float pattern=step(.48,cellHash(floor(surface*3)).x);
    float pulse=.62+.15*sin(_Time.y*2+surface.x*1.7);
    emission=half3(.21,2.5,.5)*trace*pattern*pulse*_Pathology*_Room3Reveal;
   }
   SurfaceData s=(SurfaceData)0;s.albedo=c*_BaseColor.rgb;s.alpha=1;s.metallic=.03;s.smoothness=_Smoothness;s.occlusion=1;
   s.emission=emission;
   InputData d=(InputData)0;d.positionWS=i.w;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=max(SampleSH(n),half3(.12,.16,.16));d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=half4(1,1,1,1);
   half4 outColor=UniversalFragmentPBR(d,s);outColor.rgb=MixFog(outColor.rgb,i.fog);return outColor;
  }
  ENDHLSL
 }
 Pass {
  Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
  ColorMask 0
  ZWrite On
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
  float3 _LightDirection;
  float4 vert(float3 p:POSITION,float3 n:NORMAL):SV_POSITION {
   float3 w=TransformObjectToWorld(p);float3 normal=TransformObjectToWorldNormal(n);
   float4 clip=TransformWorldToHClip(ApplyShadowBias(w,normal,_LightDirection));
   #if UNITY_REVERSED_Z
   clip.z=min(clip.z,clip.w*UNITY_NEAR_CLIP_VALUE);
   #else
   clip.z=max(clip.z,clip.w*UNITY_NEAR_CLIP_VALUE);
   #endif
   return clip;
  }
  half4 frag():SV_Target{return 0;}
  ENDHLSL
 }
 Pass {
  Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
  ColorMask R
  ZWrite On
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  float4 vert(float3 p:POSITION):SV_POSITION{return TransformObjectToHClip(p);}
  half4 frag():SV_Target{return 0;}
  ENDHLSL
 }
 }
}
