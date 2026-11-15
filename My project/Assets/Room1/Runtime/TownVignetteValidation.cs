#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace Xiaoduan.Survey {
[DefaultExecutionOrder(-220)] public sealed class TownVignetteValidation:MonoBehaviour {
 public string output;public bool Done;public string Error="",Phase="";
 List<string> checks=new List<string>();SurveyHUD hud;TownPrologue town;int start;
 public void Begin(){DontDestroyOnLoad(gameObject);StartCoroutine(Safe());}
 void Check(bool ok,string name){if(!ok)throw new System.Exception(name);checks.Add(name);}
 IEnumerator Safe(){start=Time.frameCount;var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool next=false;object value=null;try{next=stack.Peek().MoveNext();if(next)value=stack.Peek().Current;}catch(System.Exception ex){Error=ex.ToString();break;}if(!next){stack.Pop();continue;}if(value is IEnumerator nested)stack.Push(nested);else yield return value;}Release();Done=true;File.WriteAllText(Path.Combine(output,"q-vignette-validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=Error=="",Error,Phase,checks,frames=Time.frameCount-start},Newtonsoft.Json.Formatting.Indented));}
 void Release(){InputSystem.QueueStateEvent(Mouse.current,new MouseState());}
 IEnumerator Hold(float seconds){float end=Time.realtimeSinceStartup+seconds;int frames=Time.frameCount+20;while(Time.realtimeSinceStartup<end||Time.frameCount<frames){InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;}}
 void Shot(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
 IEnumerator Run(){
 yield return new WaitForSecondsRealtime(1);hud=SurveyHUD.Instance;town=TownPrologue.Active;var q=town.SurveyVignette;Phase="Initial";
 Check(hud.MenuVisible&&hud.visionFrame.alpha==0,"Main menu starts without a green overlay");
 Check(q!=null&&q.cornerImages.Length==4,"The imported Q Vignette is instantiated once with four corners");
 Check(q.mainCornerType==0&&q.mainColor.g==1&&q.mainColor.a==.60f&&q.mainScale==1.3f,"The soft gradient uses the saved fluorescent green preset");
 Check(q.cornerImages.All(i=>i.sprite!=null&&!i.raycastTarget),"Every corner has an imported sprite and cannot intercept clicks");
 hud.BeginInspection();yield return new WaitForSecondsRealtime(.2f);
 Phase="RMB";yield return Hold(.65f);Check(hud.Vision&&hud.visionFrame.alpha>.99f,"Real RMB input smoothly reveals the vignette");
 Check(town.approachTrail.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"RMB still reveals the footprint trail");
 Check(Shader.GetGlobalFloat("_TownIsotope")>.99f,"The poster reveal shader still follows RMB");
 Check(hud.visionFrame.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(g=>!g.enabled||g.transform.IsChildOf(q.transform)||g.name=="Town isotope tint"),"The old visor graphics do not overlap the new gradient");
 Shot("ROOM1-RMB-fluorescent-green");yield return Hold(.15f);Release();yield return new WaitForSecondsRealtime(.45f);
 Check(!hud.Vision&&hud.visionFrame.alpha==0,"Releasing RMB fully removes the gradient");Shot("ROOM1-normal");yield return new WaitForSecondsRealtime(.15f);
 Phase="Pause";yield return Hold(.3f);hud.GetComponent<SurveyPause>().Open();yield return Hold(.5f);Check(!hud.Vision&&hud.visionFrame.alpha==0,"Pause suppresses the effect even while RMB stays held");hud.GetComponent<SurveyPause>().Resume();Release();yield return new WaitForSecondsRealtime(.2f);
 Phase="Archive";hud.OpenArchive(0);yield return Hold(.4f);Check(hud.IsReading&&!hud.Vision&&hud.visionFrame.alpha==0,"Archive text remains clear while RMB is held");Release();hud.CloseArchive();yield return new WaitForSecondsRealtime(.2f);
 Phase="Room exit";SceneManager.LoadScene("ROOM2");yield return new WaitForSecondsRealtime(2);
 Check(TownPrologue.Active==null,"ROOM1 releases its vignette owner on scene exit");Check(hud.visionFrame.GetComponentsInChildren<Q_Vignette_Single>(true).Length==0,"The persistent HUD contains no town vignette in ROOM2");
 Check(Shader.GetGlobalFloat("_TownIsotope")==0,"The town reveal shader is reset on scene exit");
 Phase="Return";SceneManager.LoadScene("ROOM1");yield return new WaitForSecondsRealtime(2);
 Check(hud.visionFrame.GetComponentsInChildren<Q_Vignette_Single>(true).Length==1,"Returning to ROOM1 creates exactly one vignette");
 yield return Hold(.5f);Check(hud.Vision&&hud.visionFrame.alpha==1,"The effect works after returning to ROOM1");Release();yield return new WaitForSecondsRealtime(.4f);
 Check(hud.visionFrame.alpha==0,"The returning effect also fades away cleanly");Phase="Complete";
 }
}
}
#endif
