// Scoped URP replacement for BK_AlchemistHouse's Built-in GrabPass surface shader.
// Uses the existing URP opaque texture. Original package shader is preserved.
Shader "Xiaoduan/BKHouse/URP Refraction" {
 Properties {
 _Albedo("Albedo",2D)="white"{} _Opacity("Opacity",Range(0,1))=0
 _Smoothness("Smoothness",Range(0,1))=.85 _Metalness("Metalness",Range(0,1))=0
 [Normal]_NormalMap("Normal Map",2D)="bump"{}
 _IndexofRefraction("Index of Refraction",Range(-1,1))=0
 _ChromaticAberration("Chromatic Aberration",Range(0,.3))=.08
 }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
 Pass {Tags{"LightMode"="UniversalForward"}Cull Back ZWrite Off Blend One Zero
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
 TEXTURE2D(_Albedo);SAMPLER(sampler_Albedo);TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
 CBUFFER_START(UnityPerMaterial)float4 _Albedo_ST,_NormalMap_ST;half _Opacity,_Smoothness,_Metalness,_IndexofRefraction,_ChromaticAberration;CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float4 t:TANGENT;float2 uv:TEXCOORD0;};
 struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;half3 n:TEXCOORD1;half4 t:TEXCOORD2;float2 uv:TEXCOORD3;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.ws=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.t=half4(TransformObjectToWorldDir(a.t.xyz),a.t.w*GetOddNegativeScale());o.uv=a.uv;return o;}
 half4 frag(V i):SV_Target{
 half4 albedo=SAMPLE_TEXTURE2D(_Albedo,sampler_Albedo,i.uv*_Albedo_ST.xy+_Albedo_ST.zw);
 half3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv*_NormalMap_ST.xy+_NormalMap_ST.zw));
 half3 n=normalize(i.n),t=normalize(i.t.xyz),b=cross(n,t)*i.t.w;
 half3 normal=normalize(mul(normalTS,half3x3(t,b,n)));float3 view=GetWorldSpaceNormalizeViewDir(i.ws);
 float2 uv=GetNormalizedScreenSpaceUV(i.p);float grazing=1-saturate(dot(normal,view));
 float2 offset=TransformWorldToViewDir(normal).xy*_IndexofRefraction*.025*grazing;
 half3 transmission;transmission.r=SampleSceneColor(saturate(uv+offset)).r;transmission.g=SampleSceneColor(saturate(uv+offset*(1-_ChromaticAberration))).g;transmission.b=SampleSceneColor(saturate(uv+offset*(1+_ChromaticAberration))).b;
 SurfaceData s=(SurfaceData)0;s.albedo=albedo.rgb;s.metallic=_Metalness;s.smoothness=_Smoothness;s.normalTS=normalTS;s.occlusion=1;s.alpha=1;
 InputData d=(InputData)0;d.positionWS=i.ws;d.normalWS=normal;d.viewDirectionWS=view;d.shadowCoord=TransformWorldToShadowCoord(i.ws);d.bakedGI=SampleSH(normal);d.normalizedScreenSpaceUV=uv;d.shadowMask=half4(1,1,1,1);
 half3 surface=UniversalFragmentPBR(d,s).rgb;
 float opacity=saturate(albedo.a*_Opacity);
 half3 reflection=GlossyEnvironmentReflection(reflect(-view,normal),i.ws,1-_Smoothness,1,uv);
 half fresnel=.04+.96*pow(grazing,5);
 return half4(lerp(transmission,surface,opacity)+reflection*fresnel*_Smoothness*(1-opacity),1);
 }
 ENDHLSL
 }
 } }
