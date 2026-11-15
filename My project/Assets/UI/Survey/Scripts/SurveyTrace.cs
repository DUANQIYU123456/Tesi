using UnityEngine;
namespace Xiaoduan.Survey {
 public sealed class SurveyTrace:MonoBehaviour {
  public bool hideAfterPortal;
  Renderer[] surfaces;float[] baseOpacity;bool town;float visibility,last=-1;
  MaterialPropertyBlock properties;
  static readonly int Opacity=Shader.PropertyToID("_Opacity");
  public float Visibility=>visibility;
  void Awake(){properties=new MaterialPropertyBlock();surfaces=GetComponentsInChildren<Renderer>(true);baseOpacity=new float[surfaces.Length];town=gameObject.scene.name=="ROOM1";for(int i=0;i<surfaces.Length;i++){var material=surfaces[i].sharedMaterial;baseOpacity[i]=material!=null&&material.HasProperty(Opacity)?material.GetFloat(Opacity):-1;}}
  void OnEnable(){last=-1;}
  void Update(){
   bool show=TownBlackout.RevealsResidues||(!TownBlackout.HidesSurvey&&SurveyHUD.Instance!=null&&SurveyHUD.Instance.Vision);
   if(town&&hideAfterPortal&&TownPrologue.Active!=null&&TownPrologue.Active.PortalVisible)show=false;
   bool blackout=town&&TownBlackout.Active!=null&&TownBlackout.Active.Triggered&&!TownBlackout.RevealsResidues;
   visibility=blackout?0:town?Mathf.MoveTowards(visibility,show?1:0,Time.deltaTime*5):(show?1:0);
   if(Mathf.Approximately(last,visibility))return;last=visibility;
   for(int i=0;i<surfaces.Length;i++){var surface=surfaces[i];if(surface==null)continue;surface.enabled=visibility>.001f;if(town&&baseOpacity[i]>=0){surface.GetPropertyBlock(properties);properties.SetFloat(Opacity,baseOpacity[i]*visibility);surface.SetPropertyBlock(properties);}}
  }
 }
}
