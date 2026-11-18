Shader "Xiaoduan/Memory Projection Contrast" {
 Properties {_MainTex("Source photo",2D)="white"{} _Opacity("Background attenuation",Range(0,1))=.78}
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
 Pass {ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);CBUFFER_START(UnityPerMaterial) half _Opacity;CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 half4 Frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);half l=dot(c.rgb,half3(.2126,.7152,.0722));half a=smoothstep(.04,.85,l)*_Opacity*c.a;clip(a-.008);return half4(0,0,0,a);}
 ENDHLSL
 }}
}
