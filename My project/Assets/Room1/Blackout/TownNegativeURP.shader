// Negative operation adapted from Effects Pro / Shaders/Negative.shader, by Arnab Raha.
// License: https://en.wikipedia.org/wiki/MIT_License
// The code in this script is written by Arnab Raha
// Code may be redistributed in source form, provided all the comments at the top here are kept intact.
// URP integration, depth-masked sky and retained fluorescent pigments: Luce Verde nelle Ossa.
Shader "Xiaoduan/Effects Pro/Town Radiographic Negative URP" {
 Properties{_NegativeStrength("Radiograph",Range(0,1))=0 _AftermathBlend("Nocturnal aftermath",Range(0,1))=0}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline"}
 Pass{ZWrite Off ZTest Always Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma target 3.5
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
 CBUFFER_START(UnityPerMaterial)float _NegativeStrength,_AftermathBlend;CBUFFER_END
 half4 Frag(Varyings input):SV_Target{
  UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
  float2 uv=input.texcoord;float2 screenUV=input.positionCS.xy/_ScaledScreenParams.xy;
  half3 source=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).rgb;
  float raw=SampleSceneDepth(screenUV);
  #if UNITY_REVERSED_Z
   bool sky=raw<.00001;
  #else
   bool sky=raw>.99999;
  #endif
  // Keep the radioluminescent paint and its bloom instead of inverting it to magenta.
  half chroma=source.g-source.r;
  half green=smoothstep(.004,.08,chroma)*smoothstep(.012,.12,source.g)*(1-smoothstep(source.g*1.02,source.g*1.45,source.b));
  half3 fluorescence=max(source,half3(.005,1.15,.12));
  half3 pigment=fluorescence*green;
  if(_NegativeStrength<.5)return half4(pigment,1);
  half3 negative=1-saturate(source); // Effects Pro's original negative operation.
  half value=dot(negative,half3(.2126,.7152,.0722));
  value=1-sqrt(saturate((1-value)*24));
  half3 bone=lerp(half3(.035,.055,.065),half3(.91,.95,.94),value);
  half eyeDepth=LinearEyeDepth(raw,_ZBufferParams);
  half seam=saturate((abs(ddx(eyeDepth))+abs(ddy(eyeDepth)))/max(eyeDepth,.1)*4);
  bone*=1-seam*.35;
  // After the brief film-negative exposure, preserve the town's texture in a cold low key palette.
  half luminance=dot(source,half3(.2126,.7152,.0722));
  half level=pow(saturate(luminance*42),.79);
  half3 night=lerp(half3(.0015,.0045,.006),half3(.065,.12,.13),level);
  night=lerp(night,saturate(source*8)*half3(.45,.78,.8),.22);
  float depth=raw;
  #if !UNITY_REVERSED_Z
   depth=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
  #endif
  float3 world=ComputeWorldSpacePosition(screenUV,depth,UNITY_MATRIX_I_VP);
  float3 surfaceNormal=cross(ddy(world),ddx(world));
  surfaceNormal*=rsqrt(max(dot(surfaceNormal,surfaceNormal),.0000001));
  float3 viewDirection=normalize(_WorldSpaceCameraPos-world);
  half rim=pow(1-saturate(abs(dot(surfaceNormal,viewDirection))),4);
  half closeRange=1-smoothstep(24,65,eyeDepth);
  half erosion=smoothstep(.2,.8,sin(world.x*.73+world.y*.39)*sin(world.z*.61-world.y*.23)*.5+.5);
  night+=half3(.52,.64,.6)*(rim*.085)*closeRange*lerp(.25,1,erosion);
  night=lerp(night,half3(.003,.012,.016),(1-exp(-eyeDepth*.008))*.45);
  if(sky){bone=half3(.002,.005,.009);night=half3(.001,.0035,.0055);}
  half3 architecture=lerp(bone,night,_AftermathBlend);
  half3 pigmentFinal=lerp(fluorescence,source*.9,_AftermathBlend*.70);
  half3 result=lerp(architecture,pigmentFinal,green);
  return half4(result,1);
 }
 ENDHLSL
 }}Fallback Off
}
