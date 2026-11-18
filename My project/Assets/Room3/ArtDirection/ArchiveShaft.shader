Shader "Xiaoduan/Room3/Archive light shaft"
{
 Properties { _Tint("Light",Color)=(.65,.78,.72,1) _Density("Density",Range(0,1))=.12 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass { Blend SrcAlpha One ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "ThirdParty/NoiseShader/SimplexNoise3D.hlsl"
 CBUFFER_START(UnityPerMaterial)half4 _Tint;float _Density;CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;};
 V vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);o.p=p.positionCS;o.w=p.positionWS;o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;return o;}
 half4 frag(V i):SV_Target{
  float facing=pow(abs(dot(normalize(i.n),normalize(GetWorldSpaceViewDir(i.w)))),2);
  float fade=smoothstep(0,.15,i.uv.y)*(1-smoothstep(.85,1,i.uv.y));
  float grain=.75+.25*SimplexNoise(i.w*.23+float3(0,_Time.y*.04,0));
  return half4(_Tint.rgb,facing*fade*grain*_Density);
 }
 ENDHLSL
 } }
}
