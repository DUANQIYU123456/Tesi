using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Xiaoduan.Room3;
namespace Xiaoduan.Survey
{
 [DefaultExecutionOrder(450)]
 public sealed class TownPrologue:MonoBehaviour
 {
  public static TownPrologue Active{get;private set;}
  public TransitionPoint portal;
  public GameObject rift,approachTrail,exitTrail;
  public Light riftLight;
  public Transform benchFocus;
  public Renderer[] domesticGlow;
  public Renderer riftSurface;
  public bool PortalVisible{get;private set;}
  public bool Revealing{get;private set;}
  public bool CanEnter=>PortalVisible&&!Revealing&&SurveyProgress.Has(0)&&!SurveyHUD.BlocksWorld&&!Ending();
  Renderer[] riftRenderers; Collider[] portalColliders;
  MaterialPropertyBlock block;
  float vision,shakeUntil;
  bool visorCreated;
  VolumeProfile volumeProfile;
  public Q_Vignette_Single SurveyVignette{get;private set;}
  Camera offsetCamera;Quaternion originalRotation;
  Room3StoryFlow endingStory;float storySearchAt;
  readonly System.Collections.Generic.List<UnityEngine.UI.Graphic> hiddenVisor=new System.Collections.Generic.List<UnityEngine.UI.Graphic>();
  readonly System.Collections.Generic.List<GameObject> visorParts=new System.Collections.Generic.List<GameObject>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){Active=null;Shader.SetGlobalFloat("_TownIsotope",0);}
  void Awake()
  {
   Active=this;block=new MaterialPropertyBlock();portalColliders=portal.GetComponents<Collider>();riftRenderers=rift.GetComponentsInChildren<Renderer>(true);
   portal.enabled=false;foreach(var c in portalColliders)c.enabled=false;portal.GetComponent<Renderer>().enabled=false;
   rift.SetActive(false);riftLight.enabled=false;exitTrail.SetActive(false);
   var volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=12;volumeProfile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=volumeProfile;
   var bloom=volumeProfile.Add<Bloom>();bloom.intensity.Override(.32f);bloom.threshold.Override(1.05f);bloom.scatter.Override(.62f);
   RenderPipelineManager.beginCameraRendering+=BeginCamera;RenderPipelineManager.endCameraRendering+=EndCamera;
  }
  bool Ending(){if(endingStory==null&&Time.unscaledTime>=storySearchAt){endingStory=FindFirstObjectByType<Room3StoryFlow>();storySearchAt=Time.unscaledTime+1;}return endingStory!=null&&endingStory.EndingChosen;}
  void Update()
  {
   var hud=SurveyHUD.Instance;if(hud==null)return;
   if(!visorCreated){CreateVisor(hud);visorCreated=true;}
   if(Ending()){portal.enabled=false;rift.SetActive(false);riftLight.enabled=false;return;}
   vision=Mathf.MoveTowards(vision,hud.Vision?1:0,Time.unscaledDeltaTime*5);Shader.SetGlobalFloat("_TownIsotope",TownBlackout.RevealsResidues?1:TownBlackout.HidesSurvey?0:vision);
   if(PortalVisible)portal.enabled=CanEnter;
   approachTrail.SetActive(!PortalVisible);exitTrail.SetActive(PortalVisible);
   foreach(var surface in domesticGlow)
   {
    if(surface==null)continue;surface.GetPropertyBlock(block);float glow=surface.name.StartsWith("Etched")?(hud.Vision?2.5f:1.4f):(hud.Vision?1.2f:.32f);if(TownBlackout.Active!=null&&TownBlackout.Active.Triggered)glow=TownBlackout.RevealsResidues?4:0;block.SetColor("_EmissionColor",new Color(.025f,1,.16f)*glow);surface.SetPropertyBlock(block);
   }
   if(riftSurface!=null){riftSurface.GetPropertyBlock(block);block.SetFloat("_Motion",SurveyProgress.ReducedMotion?0:1);if(riftSurface.sharedMaterial.HasProperty("_speed"))block.SetFloat("_speed",SurveyProgress.ReducedMotion?0:riftSurface.sharedMaterial.GetFloat("_speed"));riftSurface.SetPropertyBlock(block);}
   var eye=Camera.main;if(eye!=null){eye.allowHDR=true;eye.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
  }
  void CreateVisor(SurveyHUD hud)
  {
   var root=hud.visionFrame.transform;
   if(root.Find("Town isotope tint")!=null)return;
   foreach(var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))if(graphic.enabled){hiddenVisor.Add(graphic);graphic.enabled=false;}
   var tint=new GameObject("Town isotope tint",typeof(RectTransform),typeof(UnityEngine.UI.Image));tint.transform.SetParent(root,false);tint.transform.SetAsFirstSibling();
   var r=(RectTransform)tint.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
   var image=tint.GetComponent<UnityEngine.UI.Image>();image.color=Color.clear;image.raycastTarget=false;visorParts.Add(tint);
   var prefab=Resources.Load<Q_Vignette_Single>("Room1SurveyQVignette");
   if(prefab==null){Debug.LogError("Missing ROOM1 Q Vignette prefab",this);return;}
   SurveyVignette=Instantiate(prefab,root,false);SurveyVignette.name="ROOM1 / Q Vignette / isotope green";
   SurveyVignette.transform.SetAsFirstSibling();
   var frame=(RectTransform)SurveyVignette.transform;frame.anchorMin=Vector2.zero;frame.anchorMax=Vector2.one;frame.offsetMin=-((RectTransform)root).offsetMin;frame.offsetMax=-((RectTransform)root).offsetMax;
   SurveyVignette.mainScale=.95f;SurveyVignette.mainColor=new Color(.06f,1,.20f,.46f);
   SurveyVignette.CheckReferences();
   SurveyVignette.UpdateVignette();
   foreach(var corner in SurveyVignette.cornerImages)corner.raycastTarget=false;
   visorParts.Add(SurveyVignette.gameObject);
  }
  public void ArchiveClosed(int index)
  {
   // ROOM1 now advances from the advertisement blackout; the bench interaction is retired.
  }
  public void BlackoutCompleted(){if(Ending()||PortalVisible||Revealing)return;SurveyProgress.Collect(0);StartCoroutine(RevealPortal(false));}
  IEnumerator RevealPortal(bool withSound=true)
  {
   Revealing=true;rift.SetActive(true);riftLight.enabled=true;shakeUntil=withSound?Time.time+.65f:0;
   var hud=SurveyHUD.Instance;hud.Notify("ISOTOPE TRACE RETAINED / ANOMALY MATERIALIZING",3);if(withSound)hud.effects.PlayOneShot(hud.boot,.3f);
   for(float t=0;t<1.8f;t+=Time.deltaTime){float s=Mathf.SmoothStep(.025f,1,t/1.8f);rift.transform.localScale=Vector3.one*s;riftLight.intensity=s*.8f;yield return null;}
   rift.transform.localScale=Vector3.one;PortalVisible=true;Revealing=false;foreach(var c in portalColliders)if(c is BoxCollider){c.isTrigger=true;c.enabled=true;}
   portal.enabled=true;hud.Notify("ANOMALY STABLE / [E] PROCESS AND ENTER",4);
  }
  void BeginCamera(ScriptableRenderContext context,Camera camera)
  {
   if(camera!=Camera.main||Time.time>=shakeUntil||SurveyProgress.ReducedMotion||Time.timeScale==0)return;
   offsetCamera=camera;originalRotation=camera.transform.rotation;
   float amplitude=Mathf.Clamp01((shakeUntil-Time.time)/.65f)*.55f;
   camera.transform.rotation*=Quaternion.Euler(Mathf.Sin(Time.time*65)*amplitude,Mathf.Cos(Time.time*57)*amplitude,0);
  }
  void EndCamera(ScriptableRenderContext context,Camera camera){if(offsetCamera==camera){camera.transform.rotation=originalRotation;offsetCamera=null;}}
  void OnDestroy()
  {
   if(offsetCamera!=null)offsetCamera.transform.rotation=originalRotation;
   RenderPipelineManager.beginCameraRendering-=BeginCamera;RenderPipelineManager.endCameraRendering-=EndCamera;
   Shader.SetGlobalFloat("_TownIsotope",0);if(volumeProfile!=null)Destroy(volumeProfile);if(Active==this)Active=null;
   foreach(var graphic in hiddenVisor)if(graphic!=null)graphic.enabled=true;
   foreach(var part in visorParts)if(part!=null)Destroy(part);
  }
 }
}
