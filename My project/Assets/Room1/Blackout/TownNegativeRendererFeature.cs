using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Xiaoduan.Survey {
 // The URP-owned FullScreenPass handles Render Graph color/depth dependencies and transient copies.
 public sealed class TownNegativeRendererFeature:FullScreenPassRendererFeature {
  public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData){
   var state=TownBlackout.Active;
   if(!Application.isPlaying||state==null||!state.Triggered||renderingData.cameraData.camera!=state.EffectCamera||passMaterial==null)return;
   passMaterial.SetFloat("_NegativeStrength",state.NegativeStrength);
   passMaterial.SetFloat("_AftermathBlend",state.AftermathBlend);
   base.AddRenderPasses(renderer,ref renderingData);
  }
 }
}
