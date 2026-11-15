#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Reflection;
using UnityEngine;using UnityEngine.InputSystem;using UnityEngine.InputSystem.LowLevel;using UnityEngine.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;using Xiaoduan.Narrative;using StarterAssets;
namespace Xiaoduan.Survey {
[DefaultExecutionOrder(-230)]public sealed class TownBlackoutValidation:MonoBehaviour {
 public string output,Phase,Error="";public bool Done,Accessibility;
 List<string> checks=new List<string>();SurveyHUD hud;TownAdvertisement ad;TownBlackout state;TownPrologueValidation walker;int firstFrame;bool oldMotion;
 public void Begin(){DontDestroyOnLoad(gameObject);StartCoroutine(Safe());}
 IEnumerator Safe(){firstFrame=Time.frameCount;var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool moved=false;object value=null;try{moved=stack.Peek().MoveNext();if(moved)value=stack.Peek().Current;}catch(Exception e){Error=e.ToString();break;}if(!moved){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());var player=GameObject.FindGameObjectWithTag("Player");if(player!=null){var input=player.GetComponent<StarterAssetsInputs>();if(input!=null){input.move=Vector2.zero;input.look=Vector2.zero;}}if(hud!=null)hud.SetReducedMotion(oldMotion);Done=true;File.WriteAllText(Path.Combine(output,"blackout-validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=Error=="",Error,Phase,checks,frames=Time.frameCount-firstFrame},Newtonsoft.Json.Formatting.Indented));}
 void Check(bool ok,string name){if(!ok)throw new Exception(name+" / "+Phase+" frame="+Time.frameCount+" elapsed="+(state!=null?state.Elapsed:-1));checks.Add(name);}
 object Invoke(string name,params object[] args)=>typeof(TownPrologueValidation).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(walker,args);
 IEnumerator Walk(Vector3 position)=> (IEnumerator)Invoke("Walk",position,30f);
 void Face(Vector3 target)=>Invoke("Face",target);
 IEnumerator Key(Key key,float duration=.09f){float end=Time.realtimeSinceStartup+duration;int frame=Time.frameCount+3;while(Time.realtimeSinceStartup<end||Time.frameCount<frame){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;}InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;}
 void Shot(string file){ScreenCapture.CaptureScreenshot(Path.Combine(output,file+".png"));}
 IEnumerator Until(Func<bool> condition,float timeout=35){float end=Time.realtimeSinceStartup+timeout;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Check(condition(),"Timed state reached");}
 IEnumerator Run(){
 yield return new WaitForSecondsRealtime(1);hud=SurveyHUD.Instance;ad=FindFirstObjectByType<TownAdvertisement>();state=ad.GetComponent<TownBlackout>();oldMotion=SurveyProgress.ReducedMotion;hud.SetReducedMotion(Accessibility);
 Check(state!=null&&!state.Triggered&&state.powerCut!=null&&state.geigerFracture!=null,"The one-shot sequence and both audio clips are configured");
 Check(FindObjectsByType<SurveyNode>(FindObjectsInactive.Include,FindObjectsSortMode.None).All(n=>n.kind!=SurveyNodeKind.TownVanity),"The old bench interaction has been removed");
 Check(!FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(t=>t.name=="Seat maker plate"),"The old bench plaque is removed");
 Check(FindObjectsByType<Light>(FindObjectsSortMode.None).Any(l=>l.type==LightType.Directional&&l.enabled),"The town begins with its sunlight intact");
 hud.BeginInspection();yield return new WaitForSecondsRealtime(.25f);Check(hud.objective.text.Contains("advertisements"),"The objective points to the advertising board rather than the removed bench");
 walker=gameObject.AddComponent<TownPrologueValidation>();Invoke("Find");Phase="Walk to board";
 foreach(var p in new[]{new Vector3(36,0,-2),new Vector3(41,0,-5),new Vector3(43,0,-9),new Vector3(45.7f,0,-15.5f)})yield return Walk(p);
 var inspect=ad.GetComponent<NarrativeInspectable>();Face(inspect.FocusBounds.center);yield return new WaitForSecondsRealtime(.45f);Check(NarrativeInteractor.Active.CurrentTarget==inspect,"Normal reach and facing select the historical board");Shot("01-town-before");yield return new WaitForSecondsRealtime(.15f);yield return Key(UnityEngine.InputSystem.Key.E);
 Check(hud.IsReading&&!state.Triggered&&!ad.Dissolving,"E opens the existing poster reader without starting the event");Check(hud.truthText.text==TownAdvertisement.Copy&&hud.archivePicture.texture==ad.advertisement,"The original advertisement text and picture are preserved");Shot("02-advertisement-reader");yield return new WaitForSecondsRealtime(.15f);
 Phase="Blackout";yield return Key(UnityEngine.InputSystem.Key.Escape,.025f);
 Check(state.Triggered&&ad.Dissolving&&!hud.IsReading,"Closing the reader starts blackout and the existing dissolve together");
 Check(FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).All(l=>!l.enabled),"Directional sunlight is switched off immediately");
 Check(!state.ResiduesVisible&&!state.XRayActive&&state.Elapsed<.5f,"The initial half-second stays dark before pigment or negative appears");
 Check(FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(s=>s.gameObject!=ad.gameObject).All(s=>s.mute),"Only the breaker sound can play during the blackout");
 Check(ad.posters.All(r=>r.sharedMaterial.shader.name.Contains("URP Dissolve")),"The imported paper dissolve shader remains unchanged");yield return new WaitForSeconds(.16f);Shot("03-dead-darkness");yield return null;
 yield return Until(()=>state.ResiduesVisible,5);Check(state.Elapsed>=.5f&&!state.XRayActive,"Fluorescent residue ignites after 0.5 seconds and before the negative");yield return null;
 Check(Shader.GetGlobalFloat("_TownResidualBoost")==1&&ad.footprintTrail.activeSelf&&ad.footprintTrail.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"The ground footprints glow without holding RMB");Shot("04-fluorescent-infection");yield return new WaitForSeconds(.04f);Face(new Vector3(44.2f,0,-14.4f));yield return new WaitForSeconds(.08f);Shot("04b-glowing-ground-trail");yield return new WaitForSeconds(.08f);Face(inspect.FocusBounds.center);
 if(!Accessibility){var pause=hud.GetComponent<SurveyPause>();pause.Open();float elapsed=state.Elapsed,progress=ad.DissolveProgress;yield return new WaitForSecondsRealtime(.4f);Check(state.Elapsed==elapsed&&ad.DissolveProgress==progress,"Pause freezes both blackout timing and paper dissolution");pause.Resume();}
 Phase="Radiograph";yield return Until(()=>state.XRayActive,5);yield return null;yield return null;
 Check(state.EffectCamera==Camera.main&&Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing,"The negative is limited to the active player camera");Check(RenderSettings.skybox==null&&Camera.main.clearFlags==CameraClearFlags.SolidColor,"The radiograph retains a deep dark sky");
 Check(ad.GetComponent<AudioSource>().clip==null,"The impact is played as a one-shot rather than a looping soundtrack");Shot("05-town-radiograph");yield return new WaitForSecondsRealtime(.2f);Face(new Vector3(40,2,-12));yield return new WaitForSecondsRealtime(.25f);Shot("05b-radiographic-street");yield return new WaitForSecondsRealtime(.2f);
 yield return Until(()=>!state.Performing&&TownPrologue.Active.PortalVisible,12);
 Check(ad.Dissolved&&ad.posters.All(r=>!r.enabled),"The existing dissolve completes with all four paper layers gone");Check(SurveyProgress.Has(0)&&TownPrologue.Active.CanEnter,"The new sequence unlocks the existing portal without the deleted bench");
 int count=state.TriggerCount;ad.ReaderClosed();yield return null;Check(state.TriggerCount==count&&count==1,"Closing or invoking the advertisement again cannot replay the sequence");Check(GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInput>().inputIsActive,"Player controls return after the reveal");
 Check(FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).All(l=>!l.enabled),"Sunlight remains off throughout the X-ray town");
 Phase="Walk to original portal";foreach(var p in new[]{new Vector3(50.3f,0,-17),new Vector3(50.3f,0,-21.5f),new Vector3(54.2f,0,-23.1f)})yield return Walk(p);Face(TownPrologue.Active.portal.transform.position);yield return new WaitForSecondsRealtime(.4f);Shot("06-radiograph-and-original-portal");
 Check(NarrativeInteractor.Active.CurrentTarget==TownPrologue.Active.portal,"The original portal is still reachable through normal movement");yield return Key(UnityEngine.InputSystem.Key.E);yield return Until(()=>SceneManager.GetActiveScene().name=="ROOM2",55);yield return new WaitForSecondsRealtime(1);
 Check(TownBlackout.Active==null&&Shader.GetGlobalFloat("_TownResidualBoost")==0,"ROOM2 does not inherit the town negative or residue state");Check(!SurveyHUD.BlocksWorld&&GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInput>().inputIsActive,"ROOM2 arrives with controls restored");
 Phase="Restart";hud.GetComponent<SurveyPause>().Restart();yield return new WaitForSecondsRealtime(3);hud=SurveyHUD.Instance;ad=FindFirstObjectByType<TownAdvertisement>();state=ad.GetComponent<TownBlackout>();
 Check(!state.Triggered&&!ad.Dissolved&&SurveyProgress.Count==0,"Restart resets the advertisement and its one-shot sequence");Check(FindObjectsByType<Light>(FindObjectsSortMode.None).Any(l=>l.type==LightType.Directional&&l.enabled),"Restart restores the original sunlight");Check(RenderSettings.skybox!=null,"Restart restores the original sky");Phase="Complete";
 }
}
}
#endif
