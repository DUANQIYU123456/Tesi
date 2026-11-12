Shader "Xiaoduan/Room3/Archive Threshold"
{
 Properties { _Tint("Tint",Color)=(.3,1.1,.6,1) }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {
  Blend SrcAlpha One
  ZWrite Off
  Cull Off
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
  struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
  V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
  half4 frag(V i):SV_Target{
   float2 p=(i.uv-.5)*2;float border=pow(saturate(max(abs(p.x),abs(p.y))),12);
   float ripple=pow(saturate(sin(length(p*float2(.8,1.3))*34-_Time.y*1.3)*.5+.5),10);
   float stripe=pow(saturate(sin(i.uv.y*170+_Time.y*2)),20);
   float edge=saturate((1-abs(p.x))*10)*saturate((1-abs(p.y))*10);
   return half4(_Tint.rgb,edge*(.055+border*.4+ripple*.08+stripe*.035));
  }
  ENDHLSL
 }
 }
}
