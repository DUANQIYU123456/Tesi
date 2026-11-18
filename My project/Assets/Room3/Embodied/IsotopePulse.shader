Shader "Xiaoduan/Room3/Isotope Pulse"
{
 Properties { _Tint("Phosphor",Color)=(.15,1,.35,1) _Always("Permanent glow",Range(0,1))=0 _Opacity("Opacity",Range(0,1))=1 _Aurora("Aurora curtain",Range(0,1))=0 _SurfaceMode("0 Line / 1 Ghost / 2 Particle",Float)=0 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha One
 ZWrite Off
 Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "../ArtDirection/ThirdParty/NoiseShader/SimplexNoise3D.hlsl"
 CBUFFER_START(UnityPerMaterial) half4 _Tint;float _Always,_Opacity,_Aurora,_SurfaceMode; CBUFFER_END
 float4 _R3PulseOrigin;float _R3PulseAge,_R3PulseEnabled;
 struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float3 n:NORMAL;half4 color:COLOR;};
 struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;float2 uv:TEXCOORD1;half3 n:TEXCOORD2;half4 color:COLOR;};
 V vert(A i){V o;i.p.x+=sin(i.p.y*.28+_Time.y*.45)*.65*_Aurora;VertexPositionInputs v=GetVertexPositionInputs(i.p.xyz);o.p=v.positionCS;o.w=v.positionWS;o.uv=i.uv;o.n=TransformObjectToWorldNormal(i.n);o.color=i.color;return o;}
 half4 frag(V i):SV_Target {
  float distance=length(i.w-_R3PulseOrigin.xyz);
  float age=_R3PulseAge-distance/23;
  float reveal=smoothstep(-.035,.04,age)*(1-smoothstep(.3,1.65,age))*_R3PulseEnabled;
  float front=exp(-abs(age)*35)*.75*_R3PulseEnabled;
  float natural=SimplexNoise(i.w*2.5+float3(0,_Time.y*.15,0))*.5+.5;
  float grain=.72+.28*natural;
  float alpha=max(_Always,max(reveal,front))*_Opacity;
  float curtain=pow(saturate(sin(i.uv.x*3.141593)),1.8)*smoothstep(0,.17,i.uv.y)*(1-smoothstep(.78,1,i.uv.y));
  alpha*=lerp(1,curtain*(.65+.2*sin(i.w.y*.31+_Time.y*.4)),_Aurora);
  if(_SurfaceMode>.5&&_SurfaceMode<1.5){float rim=pow(saturate(1-abs(dot(normalize(i.n),normalize(GetWorldSpaceViewDir(i.w))))),2.2);alpha*=lerp(.035,.62,rim)*smoothstep(.1,.42,natural);}
  if(_SurfaceMode>1.5){float r=length(i.uv*2-1);alpha*=pow(saturate(1-r),2)*i.color.a;}
  float rim=saturate(1-abs(i.uv.x-.5)*2);
  return half4(_Tint.rgb*(.65+rim*.6+front),alpha*grain);
 }
 ENDHLSL
 }
 }
}
