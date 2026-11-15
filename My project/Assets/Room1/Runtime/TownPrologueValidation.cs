#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using StarterAssets;
using Xiaoduan.Narrative;
namespace Xiaoduan.Survey
{
 [DefaultExecutionOrder(-200)]public sealed class TownPrologueValidation:MonoBehaviour
 {
  public string output;public string Phase;public bool Done;public string Error="";public bool Accessibility;
  readonly List<string> checks=new List<string>();SurveyHUD hud;Transform player;StarterAssetsInputs input;TownPrologue town;int firstFrame;bool oldMotion,oldLarge;
  public void Begin(){DontDestroyOnLoad(gameObject);StartCoroutine(Safe());}
  IEnumerator Safe()
  {
   firstFrame=Time.frameCount;var stack=new Stack<IEnumerator>();stack.Push(Run());
   while(stack.Count>0){object next=null;bool moved=false;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception ex){Error=ex.ToString();break;}if(!moved){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}
   if(input!=null){input.move=Vector2.zero;input.look=Vector2.zero;input.jump=false;}
   if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState());if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
   if(hud!=null){hud.SetReducedMotion(oldMotion);SurveyTypography.Instance.SetLargeText(oldLarge);}Done=true;
   File.WriteAllText(Path.Combine(output,"town-validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=Error=="",error=Error,checks,frames=Time.frameCount-firstFrame,phase=Phase},Newtonsoft.Json.Formatting.Indented));
  }
  void Check(bool ok,string name){if(!ok)throw new Exception(name+" | frame="+Time.frameCount+" | focused="+Application.isFocused+" | vision="+hud?.Vision+" | alpha="+hud?.visionFrame.alpha+" | modal="+SurveyHUD.BlocksWorld+" | mouse="+Mouse.current?.rightButton.isPressed+" | shown="+town?.approachTrail.GetComponentsInChildren<Renderer>().Count(r=>r.enabled));checks.Add(name);}
  void Find(){hud=SurveyHUD.Instance;player=GameObject.FindGameObjectWithTag("Player").transform;input=player.GetComponent<StarterAssetsInputs>();town=TownPrologue.Active;}
  void Face(Vector3 target)
  {
   var eye=Camera.main;var delta=target-eye.transform.position;var flat=Vector3.ProjectOnPlane(delta,Vector3.up);player.rotation=Quaternion.LookRotation(flat);
   var fps=player.GetComponent<FirstPersonController>();float pitch=-Mathf.Atan2(delta.y,flat.magnitude)*Mathf.Rad2Deg;typeof(FirstPersonController).GetField("_cinemachineTargetPitch",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(fps,pitch);fps.CinemachineCameraTarget.transform.localRotation=Quaternion.Euler(pitch,0,0);
  }
  IEnumerator KeyFor(Key key,float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;}InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
  IEnumerator Vision(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end){InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;}}
  void Release(){InputSystem.QueueStateEvent(Mouse.current,new MouseState());}
  void Capture(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
  IEnumerator Walk(Vector3 target,float seconds=22)
  {
   float end=Time.realtimeSinceStartup+seconds;while(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(target.x,target.z))>.32f&&Time.realtimeSinceStartup<end){Face(new Vector3(target.x,Camera.main.transform.position.y,target.z));input.move=Vector2.up;input.look=Vector2.zero;input.jump=false;input.sprint=false;yield return null;}
   input.move=Vector2.zero;Check(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(target.x,target.z))<.65f,"Walking route reaches "+target.ToString());yield return new WaitForSecondsRealtime(.15f);
  }
  IEnumerator Run()
  {
   yield return new WaitForSecondsRealtime(1);Find();oldMotion=SurveyProgress.ReducedMotion;oldLarge=SurveyTypography.Instance.LargeText;if(Accessibility){hud.SetReducedMotion(true);SurveyTypography.Instance.SetLargeText(true);}
   Check(hud.MenuVisible&&SurveyHUD.BlocksWorld,"Main menu opens with movement locked");Check(town.SurveyVignette!=null&&hud.visionFrame.alpha==0,"Imported Q Vignette starts hidden in the main menu");hud.BeginInspection();yield return null;
   Check(town!=null&&!town.PortalVisible&&!town.portal.enabled&&!town.rift.activeSelf,"Portal is completely absent before the diary");
   Check(town.portal.GetComponents<Collider>().All(c=>!c.enabled),"Hidden portal has no invisible collision");
   Check(!SurveyHUD.Eligible(town.portal.GetComponent<SurveyNode>(),"ROOM1"),"Radar does not guide toward the hidden portal");
   var surfaces=town.approachTrail.GetComponentsInChildren<Renderer>();Check(surfaces.Length>35&&surfaces.All(r=>!r.enabled),"Normal view hides shoe prints and spilled powder");
   Phase="Vision";Face(new Vector3(35,0,-2));yield return Vision(.8f);Check(hud.Vision&&hud.visionFrame.alpha>.9f&&surfaces.All(r=>r.enabled),"Real RMB input reveals shoe prints and the Q Vignette");Check(Shader.GetGlobalFloat("_TownIsotope")>.9f,"RMB also activates the two-layer poster shader");Check(town.SurveyVignette.cornerImages.Length==4&&town.SurveyVignette.cornerImages.All(i=>i.enabled&&!i.raycastTarget&&i.sprite!=null),"Four imported vignette sprites render without blocking clicks");Check(town.SurveyVignette.mainCornerType==0&&town.SurveyVignette.mainColor.g>.95f&&town.SurveyVignette.mainColor.r<.1f,"The smooth Q Vignette uses fluorescent green");Check(hud.visionFrame.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g=>!g.enabled||g.transform.IsChildOf(town.SurveyVignette.transform)||g.name=="Town isotope tint"),"The previous visor graphics are disabled rather than layered over Q Vignette");Capture("Town-isotope-view");Release();yield return new WaitForSecondsRealtime(.5f);Check(surfaces.All(r=>!r.enabled)&&Shader.GetGlobalFloat("_TownIsotope")<.1f,"Releasing RMB hides both layers without residue");
   Check(hud.visionFrame.alpha<.01f,"Releasing RMB fully fades the vignette");Capture("Town-normal-view");yield return new WaitForSecondsRealtime(.2f);Phase="Walk to telephone bench";foreach(var p in new[]{new Vector3(36,0,-2),new Vector3(41,0,-5),new Vector3(43,0,-9),new Vector3(48,0,-14),new Vector3(52.7f,0,-16.9f)})yield return Walk(p);
   var vanity=FindObjectsByType<SurveyNode>(FindObjectsSortMode.None).First(n=>n.kind==SurveyNodeKind.TownVanity);Face(vanity.GetComponent<NarrativeInspectable>().FocusBounds.center);yield return new WaitForSecondsRealtime(.8f);
   Check(NarrativeInteractor.Active.CurrentTarget==vanity.GetComponent<NarrativeInspectable>(),"Normal facing and obstruction checks select the compact and brush");Capture("Town-bench-gameplay");
   yield return KeyFor(Key.E,.1f);Check(hud.IsReading&&SurveyHUD.BlocksWorld&&!player.GetComponent<PlayerInput>().inputIsActive,"E opens the official layer and locks locomotion");
   yield return new WaitForSecondsRealtime(.25f);Check(!hud.Vision&&hud.visionFrame.alpha<.01f,"Opening an archive keeps the vignette hidden");hud.officialText.ForceMeshUpdate();Check(!hud.officialText.isTextOverflowing,"Official advertising text fits the reader");
   yield return KeyFor(Key.F,.6f);Check(hud.DecodeProgress<1&&!SurveyProgress.Has(0),"A partial F hold cannot retain XII");hud.CloseArchive();yield return new WaitForSecondsRealtime(.3f);Check(!town.Revealing&&!town.PortalVisible,"Closing an incomplete diary does not reveal the portal");
   hud.OpenArchive(0);yield return KeyFor(Key.F,2.3f);Check(SurveyProgress.Has(0)&&hud.DecodeProgress==1,"A real two-second F hold reveals the diary and retains XII");
   Check(!town.PortalVisible&&!town.Revealing,"The portal waits until the revealed diary is closed");hud.truthText.ForceMeshUpdate();hud.sourceText.ForceMeshUpdate();Check(!hud.truthText.isTextOverflowing&&!hud.sourceText.isTextOverflowing,"Diary and historical provenance fit the panel");Capture("Town-diary-revealed");yield return new WaitForSecondsRealtime(.2f);
   Phase="Portal materialization";hud.closeButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.25f);Check(town.Revealing&&town.rift.activeSelf&&!town.portal.enabled,"Closing the retained diary starts the visible materialization");var viewport=Camera.main.WorldToViewportPoint(town.portal.transform.position);Check(viewport.z>0&&viewport.x>.05f&&viewport.x<.95f&&viewport.y>.05f&&viewport.y<.95f,"The materializing anomaly is visible ahead from the diary viewing position");Capture("Town-anomaly-ahead");yield return new WaitForSecondsRealtime(.15f);
   var pause=hud.GetComponent<SurveyPause>();pause.Open();var scale=town.rift.transform.localScale;yield return new WaitForSecondsRealtime(.35f);Check(Time.timeScale==0&&town.rift.transform.localScale==scale,"Pause freezes materialization");pause.Resume();
   yield return new WaitForSecondsRealtime(2);Check(town.CanEnter&&town.portal.enabled&&town.portal.GetComponent<BoxCollider>().enabled,"The completed vortex becomes enterable");Check(town.exitTrail.activeSelf&&!town.approachTrail.activeSelf,"The retained diary switches guidance from bench to anomaly");
   Phase="Advertising contrast";yield return Walk(new Vector3(50.3f,0,-17));yield return Walk(new Vector3(50.3f,0,-21.5f));yield return Walk(new Vector3(52.1f,0,-23));Face(new Vector3(57.32f,3.22f,-27.748f));yield return new WaitForSecondsRealtime(.25f);Capture("Town-advertising-normal");yield return new WaitForSecondsRealtime(.2f);yield return Vision(.6f);Capture("Town-advertising-isotope");yield return new WaitForSecondsRealtime(.2f);Release();yield return new WaitForSecondsRealtime(.2f);
   if(Accessibility){var b=new MaterialPropertyBlock();town.riftSurface.GetPropertyBlock(b);Check(b.GetFloat("_Motion")==0,"Reduced motion stops vortex swirl while preserving all interactions");Check(SurveyTypography.Instance.LargeText,"Large reading mode remains enabled across the diary");}
   Phase="Walk to vortex";yield return Walk(new Vector3(54.2f,0,-23.1f));Face(town.portal.transform.position);yield return new WaitForSecondsRealtime(.6f);
   Check(town.portal.CanInteract&&NarrativeInteractor.Active.CurrentTarget==town.portal,"The real portal trigger and facing checks accept the player");Capture("Town-vortex");yield return KeyFor(Key.E,.1f);Check(SurveyHUD.BlocksWorld&&SceneManager.GetActiveScene().name=="ROOM1","Formatting first locks input and shows the contamination warning");yield return new WaitForSecondsRealtime(.25f);Capture("Town-system-warning");
   Phase="ROOM2 transfer";float until=Time.realtimeSinceStartup+55;while((SceneManager.GetActiveScene().name!="ROOM2"||SceneController.Instance.IsTransitioning)&&Time.realtimeSinceStartup<until)yield return null;yield return new WaitForSecondsRealtime(1);Find();
   Check(SceneManager.GetActiveScene().name=="ROOM2"&&SurveyProgress.Has(0),"The transition retains XII in ROOM2");Check(!hud.MenuVisible&&!SurveyHUD.BlocksWorld&&player.GetComponent<PlayerInput>().inputIsActive,"Prison arrival restores movement without reopening the menu");Check(FindObjectsByType<CharacterController>(FindObjectsSortMode.None).Count(c=>c.CompareTag("Player"))==1&&FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled)==1,"Transfer retains one player and one active listener");Check(Shader.GetGlobalFloat("_TownIsotope")==0,"Town poster vision state does not leak into the prison");
   Check(hud.visionFrame.GetComponentsInChildren<Q_Vignette_Single>(true).Length==0,"Leaving ROOM1 removes its Q Vignette from the persistent HUD");Phase="Restart";hud.GetComponent<SurveyPause>().Restart();yield return new WaitForSecondsRealtime(2);Find();Check(SceneManager.GetActiveScene().name=="ROOM1"&&SurveyProgress.Count==0&&town!=null&&!town.PortalVisible&&!town.portal.enabled,"Restart resets XII and hides the portal again");Check(hud.visionFrame.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Town isotope tint")==1,"Returning to town does not duplicate the visor");Phase="Complete";
  }
 }
}
#endif

