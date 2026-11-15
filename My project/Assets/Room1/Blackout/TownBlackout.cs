using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using StarterAssets;
namespace Xiaoduan.Survey {
 [DefaultExecutionOrder(520)]
 public sealed class TownBlackout:MonoBehaviour {
  public static TownBlackout Active{get;private set;}
  public AudioClip powerCut,geigerFracture;
  public bool Triggered{get;private set;}
  public bool ResiduesVisible{get;private set;}
  public bool XRayActive{get;private set;}
  public bool Performing{get;private set;}
  public float Elapsed{get;private set;}
  public Camera EffectCamera{get;private set;}
  public static bool HidesSurvey=>Active!=null&&Active.Triggered&&Active.Elapsed<1.7f;
  public static bool RevealsResidues=>Active!=null&&Active.ResiduesVisible;
  public float NegativeStrength=>XRayActive?1:0;
  public float AftermathBlend=>XRayActive?Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.9f,3.15f,Elapsed)):0;
  public bool AftermathSettled=>Triggered&&Elapsed>=3.15f;
  public int TriggerCount{get;private set;}
  Material oldSky;Color oldAmbient,oldFog;float oldAmbientIntensity,oldReflection;AmbientMode oldAmbientMode;bool oldFogEnabled;
  readonly List<(Light light,bool enabled)> lights=new List<(Light,bool)>();
  readonly List<(AudioSource source,bool mute)> audioStates=new List<(AudioSource,bool)>();
  readonly List<(GameObject trace,bool active)> traceStates=new List<(GameObject,bool)>();
  readonly List<(Canvas canvas,bool enabled)> canvases=new List<(Canvas,bool)>();Canvas surveyCanvas;bool oldCanvasEnabled;GameObject radiographFill,statusBacking;AudioSource effects;Volume volume;VolumeProfile profile;PlayerInput playerInput;StarterAssetsInputs inputs;bool inputWasActive,environmentCaptured;
  CameraClearFlags oldClear;Color oldBackground;bool cameraCaptured;
  bool canvasesReleased,inputRestored,restoreRequested;float sequenceStartedAt;SurveyPause pause;Light textureFill;Bloom residueBloom;UnityEngine.UI.Image statusImage;Material statusMaterial;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){Active=null;Shader.SetGlobalFloat("_TownResidualBoost",0);}
  void Awake(){Active=this;effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.spatialBlend=0;effects.volume=.78f;effects.priority=0;}
  void Update(){if(Triggered)Elapsed=Mathf.Max(0,Time.time-sequenceStartedAt);}
  public void Begin(){if(Triggered)return;Triggered=true;Performing=true;sequenceStartedAt=Time.time;TriggerCount++;EffectCamera=Camera.main;CaptureAndCut();StartCoroutine(Sequence());}
  void CaptureAndCut(){
   environmentCaptured=true;oldSky=RenderSettings.skybox;oldAmbient=RenderSettings.ambientLight;oldAmbientMode=RenderSettings.ambientMode;oldAmbientIntensity=RenderSettings.ambientIntensity;oldReflection=RenderSettings.reflectionIntensity;oldFog=RenderSettings.fogColor;oldFogEnabled=RenderSettings.fog;
   RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.ambientIntensity=0;RenderSettings.reflectionIntensity=0;RenderSettings.fog=false;
   foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.gameObject.scene==gameObject.scene)){lights.Add((light,light.enabled));light.enabled=false;}
   foreach(var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None)){if(source==effects)continue;audioStates.Add((source,source.mute));source.mute=true;}
   foreach(var trace in FindObjectsByType<SurveyTrace>(FindObjectsSortMode.None)){traceStates.Add((trace.gameObject,trace.gameObject.activeSelf));foreach(var renderer in trace.GetComponentsInChildren<Renderer>())renderer.enabled=false;}
   if(EffectCamera!=null){cameraCaptured=true;oldClear=EffectCamera.clearFlags;oldBackground=EffectCamera.backgroundColor;EffectCamera.clearFlags=CameraClearFlags.SolidColor;EffectCamera.backgroundColor=Color.black;EffectCamera.allowHDR=true;EffectCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
   var player=GameObject.FindGameObjectWithTag("Player");if(player!=null){playerInput=player.GetComponent<PlayerInput>();inputs=player.GetComponent<StarterAssetsInputs>();if(playerInput!=null){inputWasActive=playerInput.inputIsActive;playerInput.DeactivateInput();}if(inputs!=null){inputs.move=Vector2.zero;inputs.look=Vector2.zero;}}
   volume=gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=90;profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
   var bloom=profile.Add<Bloom>(true);residueBloom=bloom;bloom.threshold.Override(.65f);bloom.intensity.Override(1.15f);bloom.scatter.Override(.67f);bloom.clamp.Override(8f);
   SurveyHUD.Instance?.effects.Stop();SurveyHUD.Instance?.detector.Stop();if(SurveyHUD.Instance!=null){pause=SurveyHUD.Instance.GetComponent<SurveyPause>();surveyCanvas=SurveyHUD.Instance.GetComponent<Canvas>();if(surveyCanvas!=null){oldCanvasEnabled=surveyCanvas.enabled;surveyCanvas.enabled=false;}}foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvases.Add((canvas,canvas==surveyCanvas?oldCanvasEnabled:canvas.enabled));canvas.enabled=false;}
   if(powerCut!=null)effects.PlayOneShot(powerCut,.95f);
  }
  IEnumerator Sequence(){
   // The first half-second contains only the short breaker crack and silence.
   while(Elapsed<.5f)yield return null;
   ResiduesVisible=true;foreach(var trace in traceStates)if(trace.trace!=null)trace.trace.SetActive(true);Shader.SetGlobalFloat("_TownResidualBoost",1);
   while(Elapsed<1.15f)yield return null;
   XRayActive=true;CreateRadiographFill();if(geigerFracture!=null)effects.PlayOneShot(geigerFracture,.8f);
   // A small diffuse fill retains masonry texture in the negative; the sun remains off.
   RenderSettings.ambientLight=new Color(.15f,.17f,.19f);RenderSettings.ambientIntensity=.35f;
   restoreRequested=true;ReleaseInput();
   var advertisement=GetComponent<TownAdvertisement>();while(advertisement!=null&&!advertisement.Dissolved)yield return null;
   RestoreAudio();Performing=false;TownPrologue.Active?.BlackoutCompleted();
  }
  void CreateRadiographFill(){
   if(EffectCamera!=null){radiographFill=new GameObject("Radiograph / diffuse texture fill");radiographFill.transform.SetParent(EffectCamera.transform,false);var lamp=radiographFill.AddComponent<Light>();textureFill=lamp;lamp.type=LightType.Point;lamp.range=65;lamp.intensity=2.2f;lamp.color=new Color(.78f,.87f,1);lamp.shadows=LightShadows.None;lamp.cullingMask=EffectCamera.cullingMask;}
   var hud=SurveyHUD.Instance;if(hud!=null){statusBacking=new GameObject("Radiograph / status backing",typeof(RectTransform),typeof(UnityEngine.UI.Image));statusBacking.transform.SetParent(hud.hudRoot,false);statusBacking.transform.SetAsFirstSibling();var rect=(RectTransform)statusBacking.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-20);rect.sizeDelta=new Vector2(590,318);statusImage=statusBacking.GetComponent<UnityEngine.UI.Image>();statusImage.color=new Color(.002f,.009f,.009f,.82f);statusImage.raycastTarget=false;var template=Resources.Load<Material>("TownStatusBacking");if(template!=null){statusMaterial=new Material(template);statusImage.material=statusMaterial;}}
  }
  void LateUpdate(){if(!Triggered)return;if(restoreRequested&&!inputRestored)ReleaseInput();float blend=AftermathBlend;if(textureFill!=null){textureFill.intensity=Mathf.Lerp(2.2f,4.5f,blend);textureFill.color=Color.Lerp(new Color(.78f,.87f,1),new Color(.57f,.83f,.86f),blend);}if(residueBloom!=null)residueBloom.intensity.value=Mathf.Lerp(1.15f,.65f,blend);if(statusImage!=null)statusImage.color=new Color(.002f,.009f,.009f,Mathf.Lerp(.82f,.18f,blend));if(HidesSurvey){foreach(var entry in canvases)if(entry.canvas!=null)entry.canvas.enabled=entry.canvas==surveyCanvas&&pause!=null&&pause.Paused;}else RestoreCanvases();if(HidesSurvey&&SurveyHUD.Instance!=null){SurveyHUD.Instance.hud.alpha=0;SurveyHUD.Instance.visionFrame.alpha=0;SurveyHUD.Instance.banner.SetActive(false);}}
  void ReleaseInput(){if(inputRestored||SurveyHUD.BlocksWorld)return;if(playerInput!=null&&inputWasActive)playerInput.ActivateInput();if(inputs!=null){inputs.move=Vector2.zero;inputs.look=Vector2.zero;}inputRestored=true;}
  void RestoreCanvases(){if(canvasesReleased)return;foreach(var entry in canvases)if(entry.canvas!=null)entry.canvas.enabled=entry.enabled;canvasesReleased=true;}
  void RestoreAudio(){foreach(var entry in audioStates)if(entry.source!=null)entry.source.mute=entry.mute;audioStates.Clear();}
  void OnDestroy(){StopAllCoroutines();RestoreCanvases();if(radiographFill!=null)Destroy(radiographFill);if(statusBacking!=null)Destroy(statusBacking);if(statusMaterial!=null)Destroy(statusMaterial);ReleaseInput();RestoreAudio();Shader.SetGlobalFloat("_TownResidualBoost",0);if(profile!=null)Destroy(profile);if(EffectCamera!=null&&cameraCaptured){EffectCamera.clearFlags=oldClear;EffectCamera.backgroundColor=oldBackground;}
   if(environmentCaptured&&SceneManager.GetActiveScene()==gameObject.scene){RenderSettings.skybox=oldSky;RenderSettings.ambientLight=oldAmbient;RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientIntensity=oldAmbientIntensity;RenderSettings.reflectionIntensity=oldReflection;RenderSettings.fogColor=oldFog;RenderSettings.fog=oldFogEnabled;foreach(var entry in lights)if(entry.light!=null)entry.light.enabled=entry.enabled;}
   if(Active==this)Active=null;
  }
 }
}
