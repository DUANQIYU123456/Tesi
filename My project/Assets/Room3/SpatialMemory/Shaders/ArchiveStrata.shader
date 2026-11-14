Shader "Xiaoduan/Memory/Exposed Strata"
{
 Properties {
  _BaseMap("Concrete",2D)="white"{}
  _BaseColor("Mineral tint",Color)=(.38,.42,.41,1)
  _Erosion("Surface loss",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull Back ZWrite On
  HLSLINCLUDE
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Erosion;
   CBUFFER_END
   float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
   float noise3(float3 p){float3 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),b.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),b.x),b.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),b.x),lerp(hash(a+float3(0,1,1)),hash(a+1),b.x),b.y),b.z);}
   float surface(float3 w){return noise3(w*1.8)*.48+noise3(w*6.7)*.34+noise3(w*23)*.18-_Erosion*.70;}
   struct A{float4 p:POSITION;float3 n:NORMAL;};
   struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;half fog:TEXCOORD2;};
   V vert(A i){V o;VertexPositionInputs v=GetVertexPositionInputs(i.p.xyz);o.p=v.positionCS;o.w=v.positionWS;o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
  ENDHLSL
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
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   half4 frag(V i):SV_Target{
    float h=surface(i.w);clip(h);float edge=1-smoothstep(.002,.012,h);
    half3 n=normalize(i.n);float3 weights=pow(abs(n),4);weights/=max(dot(weights,1),.001);
    float grain=noise3(i.w*31)*.008+noise3(i.w*12)*.012;
    float3 dx=ddx(i.w),dy=ddy(i.w),rx=cross(dy,n),ry=cross(n,dx);
    float determinant=dot(dx,rx);
    float3 gradient=(ddx(grain)*rx+ddy(grain)*ry)*sign(determinant)/max(abs(determinant),.00001);
    n=normalize(n-gradient);
    float3 p=i.w*.24;
    half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.yz).rgb*weights.x+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*weights.y+SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*weights.z;
    float pores=smoothstep(.30,.47,noise3(i.w*19));
    float brokenRim=1-smoothstep(.008,.065,h);
    SurfaceData s=(SurfaceData)0;s.albedo=lerp(tex*_BaseColor.rgb*lerp(.52,1,pores),half3(.22,.25,.23),brokenRim*.7);s.alpha=1;s.smoothness=.08;s.occlusion=lerp(.55,1,pores);
    s.emission=half3(.035,.28,.10)*edge*step(.57,noise3(i.w*8))*saturate(_Erosion*3);
    InputData d=(InputData)0;d.positionWS=i.w;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.w);d.shadowCoord=TransformWorldToShadowCoord(i.w);d.bakedGI=max(SampleSH(n),half3(.10,.14,.14));d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);d.shadowMask=1;
    half4 color=UniversalFragmentPBR(d,s);color.rgb=MixFog(color.rgb,i.fog);return color;
   }
   ENDHLSL
  }
  Pass {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
   ColorMask R
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment fragDepth
   half4 fragDepth(V i):SV_Target{clip(surface(i.w));return 0;}
   ENDHLSL
  }
 }
}
