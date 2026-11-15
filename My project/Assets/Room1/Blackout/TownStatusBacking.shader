Shader "Xiaoduan/UI/Soft Survey Backing" {
 Properties {
  [PerRendererData]_MainTex("Sprite",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)]_UseUIAlphaClip("Use Alpha Clip",Float)=0
 }
 SubShader {
  Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
  Stencil{Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest[unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask[_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct A{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct V{float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
   float4 _Color,_ClipRect;
   V vert(A a){V o;o.world=a.vertex;o.vertex=UnityObjectToClipPos(a.vertex);o.color=a.color*_Color;o.uv=a.uv;return o;}
   half4 frag(V i):SV_Target{half4 col=i.color;col.a*=(1-smoothstep(.48,1,i.uv.x))*smoothstep(0,.28,i.uv.y);
    #ifdef UNITY_UI_CLIP_RECT
     col.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
     clip(col.a-.001);
    #endif
    return col;
   }
   ENDCG
  }
 }
}
