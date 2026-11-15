Shader "Xiaoduan/Environment/Town Isotope Rift" {
 Properties{_Motion("Motion",Range(0,1))=1}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass{Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial) float _Motion; CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 k=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(k),hash(k+float2(1,0)),f.x),lerp(hash(k+float2(0,1)),hash(k+1),f.x),f.y);}
 half4 frag(V i):SV_Target{float2 p=(i.uv-.5)*2;float r=length(p),a=atan2(p.y,p.x),t=_Time.y*_Motion*.3;
 float2 orbit=float2(cos(a+t*.12),sin(a+t*.12));float n=noise(orbit*7+float2(r*8-t,t*.4));float edge=.80+(n-.5)*.055+sin(a*3+t)*.018;float d=r-edge;
 float broken=noise(orbit*19+float2(t*.2,r*16));float rim=exp(-abs(d)*115)*lerp(.2,1,broken);float halo=exp(-abs(d)*22)*.12;
 float spiral=noise(float2(cos(a+r*6-t),sin(a+r*6-t))*5+r*12);float wisps=pow(saturate(spiral),5)*smoothstep(.15,.7,r)*(1-smoothstep(.77,.94,r));
 half3 color=lerp(half3(.001,.004,.006),half3(.008,.025,.028),smoothstep(.15,.8,r));color+=half3(.10,1.65,1.07)*rim+half3(.04,.35,.26)*halo+half3(.016,.13,.10)*wisps;
 return half4(color,1-smoothstep(edge-.025,edge+.13,r));}
 ENDHLSL
 }} }
