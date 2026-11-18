using System;using System.Collections;using System.Collections.Generic;using System.Linq;
using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.UI;using TMPro;
using Xiaoduan.Survey;using Xiaoduan.Narrative;
namespace Xiaoduan.Panopticon {
 [DefaultExecutionOrder(-150)] public sealed class PanopticonHall:MonoBehaviour {
  [Serializable]public sealed class Memory {public string label;public Transform anchor;public Renderer[] surfaces;public GameObject projection;public bool captured;[NonSerialized]public bool visible;[NonSerialized]public float remaining;[NonSerialized]public bool wasVisible;[NonSerialized]public float dissolveOutAt=-1;}
  public Memory[] memories;public SpatialScanEvent scanner;public Transform grid,doorLeft,doorRight;public Renderer membrane;public Collider gateBlock;
  public Light[] hallLights;public Material dissolveMaterial;public AudioSource sound;public AudioClip shutter,fracture;
  public Xiaoduan.Room2.Room2Presentation presentation;
  public int LastRetainedIndex{get;private set;}=-1;
  public bool centralProjection=true;public float memoryTransitionSeconds=.5f;public Material memoryDissolveTemplate;public Shader memoryContrastShader;
  public int ActiveMemoryIndex{get;private set;}=-1;int nextMemory;
  readonly Dictionary<Renderer,Renderer> contrastSurfaces=new Dictionary<Renderer,Renderer>();
  readonly Dictionary<Renderer,Material> hologramMaterials=new Dictionary<Renderer,Material>();readonly Dictionary<Renderer,Material> memoryDissolveMaterials=new Dictionary<Renderer,Material>();
  public bool useDissolveGrid=false;public float revealSeconds=5,reach=5.5f;
  public static PanopticonHall Active{get;private set;} public bool Entered{get;private set;}public bool GateReleased{get;private set;}public bool Busy{get;private set;}
  public int Count=>memories.Count(m=>m.captured);public int AimedIndex{get;private set;}=-1;public bool Freezing{get;private set;}
  public string Objective=>GateReleased?"ARCHIVE RETAINED / 3 RECORDS\nFollow the cold light. Enter ROOM3.":"RETAIN THE EXPOSED RECORDS / "+Count+" OF 3\n[E] Capture while the scan reveals them.";
  public int GuidedIndex{get;private set;}=-1;
  public float ScanVisualStrength{get;private set;}=1;
  public Vector3 GuidePosition=>GuidedIndex>=0?memories[GuidedIndex].anchor.position:new Vector3(-5.5f,39,-117);
  public float SecondsUntil(int index){if(index<0||memories[index].captured)return float.PositiveInfinity;if(memories[index].visible)return 0;float speed=(scanner.scanDistance+scanner.scanTrailLength)/Mathf.Max(.1f,scanner.scanDuration);float arrival=Coordinate(memories[index].anchor.position)/speed;
   if(centralProjection){float elapsed=scanner.IsScanning?Shader.GetGlobalFloat("_XDScanTravel")/speed:0;if(scanner.IsScanning&&index==ActiveMemoryIndex&&arrival>=elapsed)return arrival-elapsed;
    float wait=scanner.IsScanning?Mathf.Max(0,scanner.scanDuration-elapsed)+scanner.repeatDelay:Mathf.Max(0,(lastScanStart>-900?lastScanStart+scanner.scanDuration+scanner.repeatDelay:enteredAt+scanner.startDelay)-Time.time);int turns=0;for(int offset=0;offset<memories.Length;offset++){int k=(nextMemory+offset)%memories.Length;if(memories[k].captured)continue;if(k==index)break;turns++;}return wait+arrival+turns*(scanner.scanDuration+scanner.repeatDelay);
   }
   if(scanner.IsScanning){float elapsed=Shader.GetGlobalFloat("_XDScanTravel")/speed;return arrival>=elapsed?arrival-elapsed:Mathf.Max(0,scanner.scanDuration-elapsed)+scanner.repeatDelay+arrival;}
   return Mathf.Max(0,(lastScanStart>-900?lastScanStart+scanner.scanDuration+scanner.repeatDelay:enteredAt+scanner.startDelay)-Time.time)+arrival;
  }
  int NextUncaptured(){for(int offset=0;offset<memories.Length;offset++){int k=(nextMemory+offset)%memories.Length;if(!memories[k].captured)return k;}return -1;}
  void ChooseGuide(){if(centralProjection){GuidedIndex=ActiveMemoryIndex>=0&&!memories[ActiveMemoryIndex].captured&&(memories[ActiveMemoryIndex].visible||(scanner.IsScanning&&Shader.GetGlobalFloat("_XDScanTravel")<Coordinate(memories[ActiveMemoryIndex].anchor.position)))?ActiveMemoryIndex:NextUncaptured();return;}GuidedIndex=-1;float best=float.PositiveInfinity;for(int i=0;i<memories.Length;i++){if(memories[i].captured)continue;float score=memories[i].visible?Vector3.Distance(eye.transform.position,memories[i].anchor.position)*.05f:20+SecondsUntil(i);if(score<best){best=score;GuidedIndex=i;}}}
  Camera eye;Transform player;SurveyHUD hud;bool shuttingDown,ownsModal;float oldTime=1,findAt,pulseUntil,glitchUntil;float[] initialLight;MaterialPropertyBlock block;RectTransform overlay;TMP_Text[] labels;List<GameObject> flying=new List<GameObject>();Color oldRoleColor;bool savedRole;
  float enteredAt,lastScanStart=-999;bool scanWasRunning;Material configuredDissolve;
  Vector3 leftClosed,rightClosed;
  void Awake(){Active=this;configuredDissolve=dissolveMaterial;dissolveMaterial=new Material(configuredDissolve){name="Radar lattice (runtime instance)"};grid.GetComponent<Renderer>().sharedMaterial=dissolveMaterial;block=new MaterialPropertyBlock();leftClosed=doorLeft.localPosition;rightClosed=doorRight.localPosition;initialLight=hallLights.Select(l=>l!=null?l.intensity:0).ToArray();foreach(var m in memories){foreach(var r in m.surfaces)r.enabled=false;if(m.projection!=null)m.projection.SetActive(false);}if(grid!=null)grid.gameObject.SetActive(false);foreach(var m in memories)foreach(var r in m.surfaces){hologramMaterials[r]=r.sharedMaterial;var mat=new Material(memoryDissolveTemplate!=null?memoryDissolveTemplate:configuredDissolve){name="Memory dissolve (runtime)"};mat.SetTexture("_BaseMap",r.sharedMaterial.GetTexture("_Hologram_Texture"));float strength=m.surfaces.Length>1&&r!=m.surfaces[m.surfaces.Length-1]?.3f:1;mat.SetColor("_BaseColor",new Color(.10f,.60f,.30f)*strength);mat.SetColor("_EdgeColor",new Color(.26f,1,.52f));mat.SetFloat("_EdgeColorIntensity",1.15f*strength);mat.SetFloat("_EdgeWidth",.010f);mat.SetFloat("_NoiseScale",36);mat.SetVector("_NoiseUVSpeed",new Vector4(.05f,-.04f,0,0));memoryDissolveMaterials[r]=mat;if(memoryContrastShader!=null){var go=new GameObject("Hologram highlight contrast",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(r.transform,false);go.transform.localPosition=Vector3.forward*.012f;go.GetComponent<MeshFilter>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;var mask=go.GetComponent<MeshRenderer>();mask.sharedMaterial=new Material(memoryContrastShader){name="Memory contrast (runtime)"};mask.sharedMaterial.SetTexture("_MainTex",r.sharedMaterial.GetTexture("_Hologram_Texture"));mask.sharedMaterial.SetFloat("_Opacity",strength<1?.12f:.80f);mask.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mask.receiveShadows=false;mask.enabled=false;contrastSurfaces[r]=mask;}}}
  void Find(){eye=Camera.main;hud=SurveyHUD.Instance;var p=GameObject.FindGameObjectWithTag("Player");player=p!=null?p.transform:null;findAt=Time.unscaledTime+.4f;}
  public float Coordinate(Vector3 p)=>Vector3.Dot(p-(scanner.transform.position-scanner.transform.forward*scanner.scanStartsBehindEntrance),scanner.transform.forward);
  public bool CanCapture(int index){return index>=0&&index<memories.Length&&memories[index].visible&&!memories[index].captured&&!Busy&&!SurveyHUD.BlocksWorld&&Time.timeScale>0;}
  public bool InSight(int index){if(eye==null||index<0||index>=memories.Length)return false;var ray=new Ray(eye.transform.position,eye.transform.forward);var surfaces=memories[index].surfaces;var bounds=surfaces[surfaces.Length-1].bounds;bounds.Expand(.10f);if(!bounds.IntersectRay(ray,out float distance)||distance>reach)return false;return !Physics.Raycast(ray,Mathf.Max(0,distance-.08f),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);}

  void Update(){if(Time.unscaledTime>=findAt||eye==null||hud==null)Find();if(eye==null||player==null||hud==null)return;
   if(!Entered&&player.position.z>-166&&player.position.z<-113&&player.position.x>-18&&player.position.x<7){Entered=true;enteredAt=Time.time;scanner.screenShockEnabled=!SurveyProgress.ReducedMotion;scanner.BeginScan();hud.Notify("ARCHIVE CHAMBER / EXPOSURE CYCLE ACTIVE",2.5f);CreateLabels();}
   if(!Entered)return;scanner.screenShockEnabled=!SurveyProgress.ReducedMotion;
   bool scanning=scanner!=null&&scanner.isActiveAndEnabled&&scanner.IsScanning&&!shuttingDown;float travel=Shader.GetGlobalFloat("_XDScanTravel");float speed=Mathf.Max(.1f,scanner.scanSpeed);
   if(scanning&&!scanWasRunning){lastScanStart=Time.time;if(centralProjection){ActiveMemoryIndex=NextUncaptured();if(ActiveMemoryIndex>=0)nextMemory=(ActiveMemoryIndex+1)%memories.Length;}}scanWasRunning=scanning;
   AimedIndex=-1;
   for(int i=0;i<memories.Length;i++){var m=memories[i];float age=(travel-Coordinate(m.anchor.position))/speed;m.remaining=revealSeconds-age;bool show=scanning&&!m.captured&&age>=0&&age<revealSeconds&&(!centralProjection||i==ActiveMemoryIndex);
    m.visible=show;RenderMemory(i,show,age);if(m.projection!=null&&m.projection.activeSelf!=show)m.projection.SetActive(show);
    if(show&&InSight(i))AimedIndex=i;
   }
   if(centralProjection){foreach(var m in memories){var direction=m.anchor.position-eye.transform.position;direction.y=0;if(direction.sqrMagnitude>.2f)m.anchor.rotation=Quaternion.Slerp(m.anchor.rotation,Quaternion.LookRotation(direction),1-Mathf.Exp(-Time.deltaTime*12));}}
   ChooseGuide();ScanVisualStrength=Mathf.MoveTowards(ScanVisualStrength,hud.SoundFocusActive?.32f:AimedIndex>=0?.42f:1,Time.unscaledDeltaTime*2.5f);
   if(scanning){Shader.SetGlobalColor("_XDScanColor",scanner.scanColor*ScanVisualStrength);dissolveMaterial.SetFloat("_EdgeColorIntensity",Mathf.Lerp(.65f,1.65f,ScanVisualStrength));dissolveMaterial.SetColor("_BaseColor",new Color(.015f,.24f,.055f)*ScanVisualStrength);}
   if(grid!=null){grid.gameObject.SetActive(scanning&&useDissolveGrid);if(scanning)grid.position=scanner.transform.position+scanner.transform.forward*(travel-scanner.scanStartsBehindEntrance)+Vector3.up*4.7f;}
   if(dissolveMaterial!=null&&!shuttingDown)dissolveMaterial.SetFloat("_Dissolve",.08f+.035f*Mathf.Sin(Time.time*2));
   if(AimedIndex>=0&&CanCapture(AimedIndex)&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)StartCoroutine(Capture(AimedIndex));
  }
  void ApplyDissolve(Memory m,float threshold){foreach(var r in m.surfaces){r.enabled=true;var mat=memoryDissolveMaterials[r];mat.SetFloat("_Dissolve",Mathf.Clamp01(threshold));if(r.sharedMaterial!=mat)r.sharedMaterial=mat;if(contrastSurfaces.TryGetValue(r,out var contrast))contrast.enabled=false;}}
  void StartMemoryExit(int index){var m=memories[index];m.wasVisible=false;m.dissolveOutAt=Time.time;ApplyDissolve(m,0);}
  void RenderMemory(int index,bool show,float age){var m=memories[index];float duration=Mathf.Max(.05f,memoryTransitionSeconds);
   if(show){m.dissolveOutAt=-1;if(age<duration)ApplyDissolve(m,1-age/duration);else foreach(var r in m.surfaces){r.enabled=true;if(r.sharedMaterial!=hologramMaterials[r])r.sharedMaterial=hologramMaterials[r];}}
   else {if(m.wasVisible)StartMemoryExit(index);float elapsed=m.dissolveOutAt>=0?Time.time-m.dissolveOutAt:duration;if(elapsed<duration)ApplyDissolve(m,elapsed/duration);else foreach(var r in m.surfaces)r.enabled=false;}
   foreach(var r in m.surfaces)if(contrastSurfaces.TryGetValue(r,out var contrast))contrast.enabled=r.enabled&&r.sharedMaterial==hologramMaterials[r];
   m.wasVisible=show;
  }
  void CreateLabels(){if(labels!=null)return;labels=new TMP_Text[memories.Length];for(int i=0;i<labels.Length;i++){var t=new GameObject("Memory exposure label",typeof(TextMeshPro)).GetComponent<TextMeshPro>();t.transform.SetParent(transform);t.transform.position=memories[i].surfaces.Last().bounds.center+Vector3.down*(memories[i].surfaces.Last().bounds.extents.y+.20f);t.transform.rotation=memories[i].anchor.rotation;t.font=hud.objective.font;t.fontSize=.48f;t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.NoWrap;t.rectTransform.sizeDelta=new Vector2(4,.4f);t.color=new Color(.78f,.88f,.80f);t.outlineColor=new Color32(2,10,8,255);t.outlineWidth=.20f;t.raycastTarget=false;labels[i]=t;}}
  void LateUpdate(){if(!Entered||hud==null)return;
   if(!savedRole){oldRoleColor=hud.roleLabel.color;savedRole=true;}
   bool modal=SurveyHUD.BlocksWorld&&!Freezing;
   if(labels!=null)for(int i=0;i<labels.Length;i++){labels[i].transform.rotation=memories[i].anchor.rotation;labels[i].gameObject.SetActive(memories[i].visible&&!modal);labels[i].text=i==0?"WORKSHOP PHOTOGRAPH":i==1?"SUPPLIED CLINICAL REFERENCE":"SCENE RECONSTRUCTION";}
   if(modal)return;
   hud.objective.text=hud.SoundFocusActive?(hud.SoundFocusRetained?"SIDE CELL / TRACE RETAINED\nReturn to the exposed records when ready.":"SIDE CELL / OPTIONAL LABOR TRACE\nHold RMB to retain an audible signal."):Objective;hud.traceCount.text="RECORDS  "+Count+" / 3";
   for(int i=0;i<3;i++){hud.fragments[i].color=memories[i].captured?new Color(.4f,1,.58f):new Color(.32f,.43f,.37f);hud.fragments[i].transform.localScale=Vector3.one*(Time.unscaledTime<pulseUntil&&memories[i].captured?1.15f:1);}
   hud.roleLabel.text=Count==0?"PANOPTICON / MEMORY EXTRACTION":Count==3?"SYSTEM FAILURE // ACCESS CONTROL LOST":"WARNING: UNAUTHORIZED MEMORY ACCESS  "+Count+" / 3";
   hud.roleLabel.color=Color.Lerp(new Color(.55f,.86f,.66f),new Color(1,.38f,.33f),Count/3f);
   if(!SurveyProgress.ReducedMotion&&Time.unscaledTime<glitchUntil)hud.hudRoot.anchoredPosition+=new Vector2(Mathf.Sin(Time.unscaledTime*73),Mathf.Cos(Time.unscaledTime*61))*Count*1.4f;
   hud.integrity.text=GateReleased?"SYSTEM OFFLINE":"SYSTEM INTEGRITY  "+(100-Count*32)+"%";hud.integrityFill.fillAmount=GateReleased?0:1-Count*.32f;
   if(hud.SoundFocusActive&&!Busy)return;
   if(!GateReleased){hud.decoder.alpha=1;hud.wave.energy=AimedIndex>=0?.3f:.06f;var visible=memories.FirstOrDefault(m=>m.visible&&!m.captured);float next=GuidedIndex>=0?SecondsUntil(GuidedIndex):0;
    bool tooFar=visible!=null&&Vector3.Distance(eye.transform.position,visible.surfaces.Last().bounds.ClosestPoint(eye.transform.position))>reach;
    hud.decoderLabel.text=Busy&&LastRetainedIndex>=0?"RECORD RETAINED / "+Count+" OF 3":AimedIndex>=0?"[E] RETAIN RECORD  /  "+memories[AimedIndex].remaining.ToString("0.0")+"s":visible!=null?(tooFar?"RECORD EXPOSED / MOVE CLOSER":"RECORD EXPOSED / ALIGN YOUR VIEW"):"NEXT EXPOSURE / "+(GuidedIndex+1).ToString("00")+"   "+Mathf.CeilToInt(next)+"s";
    hud.decoderHint.text=Busy&&LastRetainedIndex>=0?memories[LastRetainedIndex].label:AimedIndex>=0?"Press [E] to keep this record.":visible!=null?(tooFar?"Approach the projection. Its image is within the scan.":"Aim at the exposed image."):"Search the hall. Records appear only as the scan reaches them.";hud.decoderFill.fillAmount=AimedIndex>=0?memories[AimedIndex].remaining/revealSeconds:0;
   }
  }
  IEnumerator Capture(int index){if(!CanCapture(index)||!InSight(index))yield break;Busy=true;LastRetainedIndex=index;glitchUntil=Time.unscaledTime+.25f;var m=memories[index];m.captured=true;m.visible=false;StartMemoryExit(index);if(m.projection!=null)m.projection.SetActive(false);
   if(sound!=null&&shutter!=null)sound.PlayOneShot(shutter,.7f);oldTime=Time.timeScale;Freezing=true;ownsModal=true;hud.SetExternalModal(true);Time.timeScale=0;
   float clock=0;
   while(clock<.18f){clock+=Time.unscaledDeltaTime;if(hud.flash!=null)hud.flash.color=new Color(.6f,.82f,.69f,(SurveyProgress.ReducedMotion?.015f:.045f)*(1-clock/.18f));yield return null;}
   ReleaseFreeze();yield return FlyToDial(index,m.anchor.position);pulseUntil=Time.unscaledTime+.8f;hud.ButtonFeedback(true);Busy=false;
   if(Count==3)yield return Collapse();
  }
  IEnumerator FlyToDial(int index,Vector3 world){if(overlay==null){overlay=new GameObject("Memory absorption particles",typeof(RectTransform)).GetComponent<RectTransform>();overlay.SetParent(hud.safeArea,false);overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;}
   Vector2 start,end;RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay,eye.WorldToScreenPoint(world),null,out start);RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay,hud.fragments[index].rectTransform.position,null,out end);
   int count=SurveyProgress.ReducedMotion?4:10;var dots=new RectTransform[count];var seed=new System.Random(53+index);var offsets=new Vector2[count];
   for(int i=0;i<count;i++){var d=new GameObject("Record trace",typeof(RectTransform),typeof(UnityEngine.UI.Image));d.transform.SetParent(overlay,false);var rt=(RectTransform)d.transform;float size=2+(float)seed.NextDouble()*3;rt.sizeDelta=new Vector2(size,1);d.GetComponent<UnityEngine.UI.Image>().color=new Color(.61f,.82f,.69f,.65f);d.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;offsets[i]=new Vector2((float)seed.NextDouble()-.5f,(float)seed.NextDouble()-.5f)*32;dots[i]=rt;flying.Add(d);}
   float t=0;while(t<1){if(SurveyHUD.BlocksWorld){yield return null;continue;}t+=Time.unscaledDeltaTime/.65f;for(int i=0;i<count;i++){float u=Mathf.SmoothStep(0,1,Mathf.Clamp01(t-i*.003f));dots[i].anchoredPosition=Vector2.Lerp(start+offsets[i],end,u)+Vector2.up*Mathf.Sin(u*Mathf.PI)*(28+i%3*5);dots[i].localScale=Vector3.one*Mathf.Lerp(1,.25f,u);}yield return null;}
   foreach(var d in dots)if(d!=null){flying.Remove(d.gameObject);Destroy(d.gameObject);}
  }
  IEnumerator Collapse(){shuttingDown=true;Busy=true;if(scanner!=null)scanner.enabled=false;hud.Notify("ARCHIVE RETAINED / SCAN TERMINATED",3);if(sound!=null&&fracture!=null)sound.PlayOneShot(fracture,.3f);
   if(presentation!=null)presentation.ReleaseArchive();yield return new WaitForSeconds(.35f);
   if(membrane!=null)membrane.enabled=false;float t=0;while(t<1){if(SurveyHUD.BlocksWorld){yield return null;continue;}t+=Time.unscaledDeltaTime/1.8f;float u=Mathf.SmoothStep(0,1,t);doorLeft.localPosition=leftClosed+Vector3.left*u*1.6f;doorRight.localPosition=rightClosed+Vector3.right*u*1.6f;for(int i=0;i<hallLights.Length;i++)if(hallLights[i]!=null){float stagger=Mathf.InverseLerp(-165,-115,hallLights[i].transform.position.z)*.22f;float dim=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-stagger)/.78f));hallLights[i].intensity=initialLight[i]*Mathf.Lerp(1,.32f,dim);}if(dissolveMaterial!=null)dissolveMaterial.SetFloat("_Dissolve",Mathf.Lerp(.1f,1,u));yield return null;}
   gateBlock.enabled=false;if(grid!=null)grid.gameObject.SetActive(false);SurveyProgress.Collect(1);SurveyProgress.Collect(2);GateReleased=true;Busy=false;hud.Notify("ARCHIVE GATE RELEASED / ENTER ROOM3 [E]",5);
  }
  void ReleaseFreeze(){if(Freezing){Time.timeScale=oldTime;Freezing=false;}if(ownsModal){ownsModal=false;if(hud!=null)hud.SetExternalModal(false);}}
  void OnDestroy(){ReleaseFreeze();foreach(var contrast in contrastSurfaces.Values)if(contrast!=null){if(contrast.sharedMaterial!=null)Destroy(contrast.sharedMaterial);Destroy(contrast.gameObject);}foreach(var pair in hologramMaterials)if(pair.Key!=null)pair.Key.sharedMaterial=pair.Value;foreach(var material in memoryDissolveMaterials.Values)if(material!=null)Destroy(material);if(dissolveMaterial!=null&&dissolveMaterial!=configuredDissolve)Destroy(dissolveMaterial);if(Active==this)Active=null;if(overlay!=null)Destroy(overlay.gameObject);if(hud!=null){foreach(var f in hud.fragments)if(f!=null)f.transform.localScale=Vector3.one;if(savedRole)hud.roleLabel.color=oldRoleColor;}}
 }
}
