Shader "Xiaoduan/Room3/World Concrete"
{
 Properties {
  _BaseMap("Concrete",2D)="white"{}
  _BaseColor("Tint",Color)=(.48,.5,.49,1)
  _TextureScale("World scale",Float)=.24
  _Smoothness("Smoothness",Range(0,1))=.18
  _Pathology("Porous corrosion",Range(0,1))=0
  _NormalMap("Concrete normal",2D)="bump"{}
  _ORMMap("Occlusion / roughness",2D)="white"{}
  _NormalStrength("Normal relief",Range(0,2))=.65
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
  #include "../ArtDirection/ThirdParty/NoiseShader/SimplexNoise3D.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
  TEXTURE2D(_ORMMap); SAMPLER(sampler_ORMMap);
  CBUFFER_START(UnityPerMaterial)
  float4 _BaseMap_ST; half4 _BaseColor; float _TextureScale; half _Smoothness; float _Pathology;float _NormalStrength;
  CBUFFER_END
  float _Room3Reveal;
  float4 _R3PulseOrigin;float _R3PulseAge,_R3PulseEnabled;
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
   half3 nx=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p.zy),_NormalStrength);
   half3 ny=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p.xz),_NormalStrength);
   half3 nz=UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,p.xy),_NormalStrength);
   nx=half3(nx.xy+n.zy,abs(nx.z)*n.x);ny=half3(ny.xy+n.xz,abs(ny.z)*n.y);nz=half3(nz.xy+n.xy,abs(nz.z)*n.z);
   n=normalize(nx.zyx*b.x+ny.xzy*b.y+nz.xyz*b.z);
   half2 orm=SAMPLE_TEXTURE2D(_ORMMap,sampler_ORMMap,p.yz).rg*b.x+SAMPLE_TEXTURE2D(_ORMMap,sampler_ORMMap,p.xz).rg*b.y+SAMPLE_TEXTURE2D(_ORMMap,sampler_ORMMap,p.xy).rg*b.z;
   float4 mineral=SimplexNoiseGrad(i.w*3.2);
   n=normalize(n-(mineral.xyz-n*dot(n,mineral.xyz))*.018);
   float deposit=SimplexNoise(i.w*.37)*.5+.5;
   c*=lerp(.68,1.07,deposit)*lerp(.93,1.02,mineral.w*.5+.5);
   half3 emission=0;
   if(_Pathology>.01){
    float2 surface=p.yz*b.x+p.xz*b.y+p.xy*b.z;
    float pores=1-smoothstep(.035,.23,cellDistance(surface*9));
    c*=1-pores*.87*_Pathology;
    float2 lanes=abs(frac(surface*3)-.5);float trace=1-smoothstep(.012,.035,min(lanes.x,lanes.y));
    float pattern=step(.48,cellHash(floor(surface*3)).x);
    float pulse=.62+.15*sin(_Time.y*2+surface.x*1.7);
    emission=half3(.09,1.15,.28)*trace*pattern*pulse*_Pathology*_Room3Reveal*smoothstep(.25,.78,deposit);
   }
   // A thin world-space spherical shell follows the existing isotope pulse.
   float shell=abs(length(i.w-_R3PulseOrigin.xyz)-_R3PulseAge*23);
   float band=(1-smoothstep(.07,.65,shell))*_R3PulseEnabled;
   emission+=half3(.045,.55,.19)*band*(.6+.4*deposit);
   SurfaceData s=(SurfaceData)0;s.albedo=c*_BaseColor.rgb;s.alpha=1;s.metallic=.015;s.smoothness=saturate((1-orm.y)*.22+_Smoothness*.35);s.occlusion=lerp(.65,1,orm.x);
   s.emission=emission;
   InputData d=(InputData)0;d.positionWS=i.w;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=max(SampleSH(n),half3(.045,.055,.057));d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=half4(1,1,1,1);
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
