Shader "Xiaoduan/Environment/Town Two Layer Poster" {
 Properties{_PosterMap("Historical print",2D)="white"{} _WearMap("Tear atlas",2D)="white"{} _GhostMap("Thesis Fig 2.7",2D)="black"{} _Radiograph("Reconstructed skeletal effect",2D)="black"{} _PaperTint("Paper tint",Color)=(.65,.56,.4,1) _WearRect("Tear crop",Vector)=(.084,.019,.386,.452) _FaceRect("Face reveal region",Vector)=(.7,.74,.23,.22)}
 SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
 Pass{Tags{"LightMode"="UniversalForward"}Cull Off ZWrite On Offset -1,-1
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_PosterMap);SAMPLER(sampler_PosterMap);TEXTURE2D(_WearMap);SAMPLER(sampler_WearMap);TEXTURE2D(_GhostMap);SAMPLER(sampler_GhostMap);TEXTURE2D(_Radiograph);SAMPLER(sampler_Radiograph);
 float _TownIsotope;
 CBUFFER_START(UnityPerMaterial)float4 _PaperTint,_WearRect,_FaceRect;CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float2 uv:TEXCOORD1;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;return o;}
 float ellipse(float2 p,float2 r){return 1-smoothstep(.95,1.03,length(p/r));}
 half4 frag(V i):SV_Target {
 float alpha=SAMPLE_TEXTURE2D(_WearMap,sampler_WearMap,lerp(_WearRect.xy,_WearRect.zw,i.uv)).a;
 float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
 alpha=lerp(1,alpha,1-smoothstep(.025,.085,edge));
 float ripple=sin(i.uv.y*95)*.002;
 float tearA=step(abs(i.uv.x-.32+ripple),.006)*smoothstep(.84,.9,i.uv.y);
 float tearB=step(abs(i.uv.x-.48-ripple),.007)*(1-smoothstep(.08,.14,i.uv.y));
 alpha*=1-max(tearA,tearB);clip(alpha-.1);
 float ink=SAMPLE_TEXTURE2D(_PosterMap,sampler_PosterMap,i.uv).r;float grain=frac(sin(dot(floor(i.uv*900),float2(127.1,311.7)))*43758.5453);
 half3 albedo=_PaperTint.rgb*lerp(.13,.98,ink)*lerp(.91,1,grain);Light l=GetMainLight();half3 col=albedo*(SampleSH(normalize(i.n))+l.color*saturate(dot(normalize(i.n),l.direction)));
 if(_FaceRect.z>.001){float2 p=(i.uv-_FaceRect.xy)/_FaceRect.zw;
 half4 bone=SAMPLE_TEXTURE2D(_Radiograph,sampler_Radiograph,saturate(p+.5));float inside=step(abs(p.x),.5)*step(abs(p.y),.5);
 float face=ellipse(p,float2(.56,.66));float ghost=SAMPLE_TEXTURE2D(_GhostMap,sampler_GhostMap,saturate(p+.5)).r;
 col=lerp(col,col*.08,_TownIsotope*face);col+=bone.rgb*bone.a*inside*1.8*_TownIsotope+half3(.01,.08,.02)*ghost*face*_TownIsotope;
 }return half4(col,1);}
 ENDHLSL
 }} }
