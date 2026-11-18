#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;
using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using UnityEngine.SceneManagement;
using Xiaoduan.Survey;using Xiaoduan.Clinical;using StarterAssets;
namespace Xiaoduan.PrisonTraces {
 [DefaultExecutionOrder(-230)]public sealed class HandprintValidation:MonoBehaviour {
  public string output,Phase,Error="";public bool Done;readonly List<string> checks=new List<string>();
  SurveyHUD hud;Transform player;PrisonHandprints traces;int firstFrame;bool oldMotion;
  public void Begin(){Directory.CreateDirectory(output);DontDestroyOnLoad(gameObject);StartCoroutine(Safe());}
  IEnumerator Safe(){firstFrame=Time.frameCount;var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool more=false;object value=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){Error=e.ToString();break;}if(!more){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());if(hud!=null)hud.SetReducedMotion(oldMotion);Done=true;File.WriteAllText(Path.Combine(output,"validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=Error=="",Error,Phase,checks,frames=Time.frameCount-firstFrame},Newtonsoft.Json.Formatting.Indented));}
  void Check(bool yes,string text){if(!yes)throw new Exception(text+" / "+Phase);checks.Add(text);}
  IEnumerator Until(Func<bool> c,float seconds=45){float end=Time.realtimeSinceStartup+seconds;while(!c()&&Time.realtimeSinceStartup<end)yield return null;Check(c(),"State reached: "+Phase);}
  void Place(Vector3 p){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.position=p;cc.enabled=true;Physics.SyncTransforms();}
  void Face(Vector3 p){var delta=p-Camera.main.transform.position;var flat=Vector3.ProjectOnPlane(delta,Vector3.up);player.rotation=Quaternion.LookRotation(flat);var fps=player.GetComponent<FirstPersonController>();float pitch=-Mathf.Atan2(delta.y,flat.magnitude)*Mathf.Rad2Deg;typeof(FirstPersonController).GetField("_cinemachineTargetPitch",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(fps,pitch);fps.CinemachineCameraTarget.transform.localRotation=Quaternion.Euler(pitch,0,0);}
  IEnumerator Right(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end){InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;}}
  IEnumerator Release(){InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.5f);}
  void Shot(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
  IEnumerator Run(){
   yield return new WaitForSecondsRealtime(1);hud=SurveyHUD.Instance;oldMotion=SurveyProgress.ReducedMotion;Check(Time.frameCount>5,"Game frames advance");if(hud.MenuVisible)hud.BeginInspection();
   var portal=FindObjectsByType<TransitionPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM2");SurveyProgress.Collect(0);SceneController.GetOrCreate().TransitionToDestination(portal);Phase="Transfer to prison";yield return Until(()=>SceneManager.GetActiveScene().name=="ROOM2"&&!SceneController.Instance.IsTransitioning);yield return new WaitForSecondsRealtime(1);
   player=GameObject.FindGameObjectWithTag("Player").transform;traces=PrisonHandprints.Active;var desk=ClinicalDesk.Active;desk.Retain();Place(new Vector3(-12.50f,37.04f,-147.98f));Face(new Vector3(-16.78f,38.25f,-147.98f));yield return new WaitForSecondsRealtime(.5f);
   Check(traces!=null&&traces.marks.Length==35,"35 scattered palms are installed");Check(traces.marks.All(r=>!r.enabled),"Normal view hides every handprint");
   Check(traces.marks.Count(r=>r.name.Contains("wall palm"))==17&&traces.marks.Count(r=>!r.name.Contains("wall palm"))==18,"17 wall palms and 18 gate palms are distributed across both floors");
   Check(traces.marks.Any(r=>r.bounds.center.y>45)&&traces.marks.Any(r=>r.bounds.center.y<39),"Handprints occupy ground and upper levels");
   Check(traces.marks.All(r=>r.GetComponent<Collider>()==null&&r.GetComponent<MeshFilter>().sharedMesh.vertexCount>0),"Decals have real projected geometry and introduce no blocking colliders");
   Check(traces.marks.Where(r=>!r.name.Contains("wall palm")).All(r=>r.transform.parent.GetComponent<MeshRenderer>()!=null&&r.transform.parent.name.EndsWith("LOD0")),"Iron gate palms are attached to the moving gate surfaces");
   Shot("01-normal-view-hidden");yield return null;yield return Right(.25f);Check(traces.Visibility>0,"Right-button input starts the handprint reveal");yield return Right(.5f);
   Check(hud.Vision&&traces.Visibility>.99f&&traces.marks.All(r=>r.enabled),"Holding actual RMB reveals all handprints");
   var reference=Resources.Load<Q_Vignette_Single>("Room1SurveyQVignette");Check(traces.Vignette!=null&&traces.Vignette.mainColor==reference.mainColor&&traces.Vignette.mainCornerType==reference.mainCornerType,"ROOM2 uses exactly the ROOM1 green vignette style");
   Check(traces.Vignette.cornerImages.Length==4&&traces.Vignette.cornerImages.All(i=>i.enabled&&!i.raycastTarget),"Four soft green corners display without blocking UI input");
   var corners=new Vector3[4];((RectTransform)traces.Vignette.transform).GetWorldCorners(corners);Check(corners[0].x<=.1f&&corners[0].y<=.1f&&corners[2].x>=Screen.width-.1f&&corners[2].y>=Screen.height-.1f,"Fluorescent green fills the outer screen edges");
   Shot("02-RMB-wall-and-bars");yield return Right(.15f);yield return Release();Check(!hud.Vision&&traces.Visibility==0&&traces.marks.All(r=>!r.enabled),"Releasing RMB hides all marks and the visor");
   var gate=GameObject.Find("SM_PrisonDoor15").GetComponent<ProximityPrisonDoor>();var mark=traces.marks.First(r=>r.name=="SM_PrisonDoor15 - hand on iron bars");Vector3 local=mark.transform.localPosition;var before=mark.transform.position;Place(new Vector3(-15.1f,37.04f,-147.98f));yield return new WaitForSecondsRealtime(1.2f);
   Check(gate.OpeningOrOpen&&Vector3.Distance(before,mark.transform.position)>.1f&&Vector3.Distance(local,mark.transform.localPosition)<.001f,"A palm stays fixed to its iron door while that door opens");
   Place(new Vector3(-5.6f,37.04f,-157));Face(new Vector3(-5.6f,40,-132));yield return Right(.5f);Shot("03-RMB-scattered-hall");yield return Right(.15f);
   hud.OpenClinicalArchive(desk);yield return Right(.5f);Check(hud.IsClinicalReading&&!hud.Vision&&traces.Visibility==0,"Opening the medical reader suppresses handprints and visor even with RMB held");hud.CloseArchive();yield return Release();
   hud.SetReducedMotion(true);Place(new Vector3(-12.5f,37.04f,-140.48f));Face(new Vector3(-16.78f,38.45f,-140.48f));yield return Right(.55f);Check(traces.Visibility>.99f&&hud.Vision,"Reduced-motion mode preserves RMB discovery");Shot("04-reduced-motion-palms");yield return Release();
   var pause=hud.GetComponent<SurveyPause>();pause.Open();yield return Right(.4f);Check(traces.Visibility==0&&!hud.Vision,"Pause hides the discovery layer");pause.Resume();yield return Release();
   Phase="Leave prison";var exit=FindObjectsByType<TransitionPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM3");SceneController.GetOrCreate().TransitionToDestination(exit);yield return Until(()=>SceneManager.GetActiveScene().name=="ROOM3"&&!SceneController.Instance.IsTransitioning);yield return new WaitForSecondsRealtime(.8f);
   Check(PrisonHandprints.Active==null&&hud.visionFrame.GetComponentsInChildren<Q_Vignette_Single>(true).Length==0,"Leaving ROOM2 removes its handprints and green vignette without leaking into ROOM3");
   Phase="Restart";hud.GetComponent<SurveyPause>().Restart();yield return new WaitForSecondsRealtime(2);
   Check(SceneManager.GetActiveScene().name=="ROOM1"&&TownPrologue.Active!=null,"Restart returns to the existing town");Check(hud.visionFrame.GetComponentsInChildren<Q_Vignette_Single>(true).Length==1,"Restart restores exactly one original ROOM1 vignette");Phase="Complete";
  }
 }
}
#endif
