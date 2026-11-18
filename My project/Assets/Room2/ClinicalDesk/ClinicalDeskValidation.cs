#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Xiaoduan.Narrative;
using Xiaoduan.Survey;
using StarterAssets;
using TMPro;

namespace Xiaoduan.Clinical
{
    [DefaultExecutionOrder(-230)]
    public sealed class ClinicalDeskValidation:MonoBehaviour
    {
        public string output,Phase,Error="";public bool Done;
        readonly List<string> checks=new List<string>();
        Transform player;StarterAssetsInputs input;SurveyHUD hud;ClinicalDesk desk;
        bool oldMotion,oldLarge;int firstFrame;
        public void Begin(){DontDestroyOnLoad(gameObject);Directory.CreateDirectory(output);StartCoroutine(Safe());}
        IEnumerator Safe()
        {
            firstFrame=Time.frameCount;var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool more=false;object next=null;
                try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){Error=e.ToString();break;}
                if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            if(input!=null){input.move=Vector2.zero;input.look=Vector2.zero;}
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            if(hud!=null){hud.SetReducedMotion(oldMotion);SurveyTypography.Instance.SetLargeText(oldLarge);}
            Done=true;File.WriteAllText(Path.Combine(output,"validation.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=Error=="",Error,Phase,checks,frames=Time.frameCount-firstFrame},Newtonsoft.Json.Formatting.Indented));
        }
        void Check(bool ok,string message){if(!ok)throw new Exception(message+" / "+Phase);checks.Add(message);}
        IEnumerator Until(Func<bool> condition,float seconds=35){float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Check(condition(),"State reached: "+Phase);}
        IEnumerator KeyFor(Key key,float duration=.09f){float end=Time.realtimeSinceStartup+duration;int frame=Time.frameCount+3;while(Time.realtimeSinceStartup<end||Time.frameCount<frame){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;}InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(.15f);}
        void Place(Vector3 p){var c=player.GetComponent<CharacterController>();c.enabled=false;player.position=p;c.enabled=true;Physics.SyncTransforms();}
        void Face(Vector3 target)
        {
            var delta=target-Camera.main.transform.position;var flat=Vector3.ProjectOnPlane(delta,Vector3.up);player.rotation=Quaternion.LookRotation(flat);
            var fps=player.GetComponent<FirstPersonController>();float pitch=-Mathf.Atan2(delta.y,flat.magnitude)*Mathf.Rad2Deg;
            typeof(FirstPersonController).GetField("_cinemachineTargetPitch",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(fps,pitch);fps.CinemachineCameraTarget.transform.localRotation=Quaternion.Euler(pitch,0,0);
        }
        IEnumerator Walk(Vector3 destination)
        {
            float end=Time.realtimeSinceStartup+22;
            while(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(destination.x,destination.z))>.28f&&Time.realtimeSinceStartup<end)
            {Face(new Vector3(destination.x,Camera.main.transform.position.y,destination.z));input.move=Vector2.up;input.look=Vector2.zero;input.jump=false;yield return null;}
            input.move=Vector2.zero;Check(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(destination.x,destination.z))<.6f,"Physical walking reaches "+destination);
            yield return new WaitForSecondsRealtime(.3f);
        }
        void Shot(string name){ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
        Vector2 ScreenPoint(RectTransform rect,Vector2 p)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(p));
        IEnumerator Stroke(ClinicalCoating coat,float y)
        {
            var start=ScreenPoint(coat.rectTransform,new Vector2(-475,y));
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=start}.WithButton(MouseButton.Left));yield return null;
            for(int i=0;i<=70&&!desk.Retained;i++)
            {
                var p=ScreenPoint(coat.rectTransform,new Vector2(Mathf.Lerp(-475,475,(float)i/70),y));
                InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;
            }
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=ScreenPoint(coat.rectTransform,new Vector2(475,y))});yield return new WaitForSecondsRealtime(.1f);
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);hud=SurveyHUD.Instance;oldMotion=SurveyProgress.ReducedMotion;oldLarge=SurveyTypography.Instance.LargeText;
            Check(Time.frameCount>5,"The player loop is advancing");
            if(hud.MenuVisible)hud.BeginInspection();
            // Isolate this feature: carry the existing player through the real scene loader,
            // then position outside the first gate. This does not claim a whole-game playthrough.
            if(SceneManager.GetActiveScene().name=="ROOM1")
            {
                SurveyProgress.Collect(0);
                var portal=FindObjectsByType<TransitionPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM2");
                SceneController.GetOrCreate().TransitionToDestination(portal);
                Phase="Scene transfer";yield return Until(()=>SceneManager.GetActiveScene().name=="ROOM2"&&!SceneController.Instance.IsTransitioning,50);
            }
            yield return new WaitForSecondsRealtime(1);
            player=GameObject.FindGameObjectWithTag("Player").transform;input=player.GetComponent<StarterAssetsInputs>();desk=FindFirstObjectByType<ClinicalDesk>();
            Check(desk!=null&&desk.bottles.Length==5,"The desk and five existing liquid bottles are installed");
            Check(desk.clinicalImage!=null&&desk.paperScratch!=null&&desk.boneFriction!=null&&desk.geigerBurst!=null,"Original thesis image and all three scratch feedback clips are assigned");
            Check(desk.bottles.All(b=>!b.GetComponent<NarrativeInspectable>().enabled),"Legacy bottle inspection is disabled");
            var props=desk.table.parent;
            Check(new[]{"Book09c","ClutterGlass02","ClutterBowl01","Skull01b","Skull02"}.All(n=>props.Find(n).gameObject.activeSelf),"All original book, skull, cup and bowl props remain visible");
            Place(new Vector3(-5.68f,36.96f,-222));yield return new WaitForSecondsRealtime(.5f);
            var sun=FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);float sunlight=sun.intensity;
            Check(!desk.Inspecting&&desk.Lift==0,"Bottles begin resting on the table");
            Phase="Cross first gate";yield return Walk(new Vector3(-5.68f,36.96f,-216));yield return new WaitForSecondsRealtime(.7f);
            Check(desk.Entered&&!sun.enabled&&sun.intensity<.001f,"Walking through the first gate darkens the gallery and switches off sunlight");
            Phase="Reach desk";yield return Walk(new Vector3(-5.72f,36.96f,-196.12f));
            var target=FindFirstObjectByType<ClinicalDeskTarget>().GetComponent<NarrativeInspectable>();Face(target.FocusBounds.center);yield return new WaitForSecondsRealtime(.7f);
            Check(hud.objective.text.Contains("candlelit medical record")&&SurveyHUD.Eligible(target.GetComponent<SurveyNode>(),"ROOM2"),"The objective and radar prioritize the concealed desk record even after XII");
            Check(NarrativeInteractor.Active.CurrentTarget==target,"Normal distance, facing and visibility select the table record");Shot("01-candlelit-table");yield return new WaitForSecondsRealtime(.2f);
            yield return KeyFor(Key.E);Check(hud.IsClinicalReading&&desk.Inspecting&&SurveyHUD.BlocksWorld,"E opens the shared Bloodlines clinical reader and locks world input");
            Check(!desk.Retained&&hud.DecodeProgress<1,"The new record stays concealed even when XII was collected in ROOM1");
            yield return new WaitForSecondsRealtime(1);Check(desk.Lift>.98f,"All bottles rise only after examination begins");Shot("02-redacted-record");
            var reader=hud.GetComponent<ClinicalDeskReader>();var coat=reader.Coating;
            yield return Stroke(coat,-110);Check(coat.Coverage>.01f&&coat.Coverage<.5f&&!desk.Retained,"Real left-button dragging partially removes ink without prematurely retaining the record");Shot("03-partial-scrape");
            yield return KeyFor(Key.Escape);Check(!hud.IsReading&&!desk.Inspecting&&!SurveyHUD.BlocksWorld,"Escape closes a partly scraped record and releases the modal lock");yield return new WaitForSecondsRealtime(1);Check(desk.Lift<.01f,"Closing returns the bottles to the tabletop");
            Face(target.FocusBounds.center);yield return new WaitForSecondsRealtime(.3f);yield return KeyFor(Key.E);yield return new WaitForSecondsRealtime(.3f);
            Check(hud.IsClinicalReading&&coat.Coverage==0,"Reopening an unfinished record resets its ink coating");
            Phase="Reveal by real mouse strokes";
            foreach(float y in new[]{-130f,-87f,-44f,0,44,87,130}){yield return Stroke(coat,y);if(desk.Retained)break;}
            yield return Until(()=>desk.Retained,8);Check(hud.DecodeProgress==1&&coat.Coverage==1,"Sufficient scraping reveals the clinical evidence and retains the record");
            Shot("04-truth-revealed");yield return KeyFor(Key.Escape);
            Check(player.GetComponent<PlayerInput>().inputIsActive&&input.cursorInputForLook,"Player input and mouse-look return after closing");
            hud.GetComponent<SurveyPause>().Open();yield return null;Check(Time.timeScale==0,"The existing pause flow still freezes the world");hud.GetComponent<SurveyPause>().Resume();
            hud.SetReducedMotion(true);SurveyTypography.Instance.SetLargeText(true);
            Face(target.FocusBounds.center);yield return new WaitForSecondsRealtime(.3f);yield return KeyFor(Key.E);yield return new WaitForSecondsRealtime(.8f);
            Check(hud.IsClinicalReading&&desk.Retained,"A retained record can be reread with large text and reduced motion");
            foreach(var text in reader.GetComponentsInChildren<TMP_Text>(true))text.ForceMeshUpdate();
            Shot("05-large-text-reread");yield return KeyFor(Key.Escape);
            Place(new Vector3(-5.68f,36.96f,-222));yield return new WaitForSecondsRealtime(.8f);Check(sun.enabled&&Mathf.Abs(sun.intensity-sunlight)<.01f,"Leaving the entrance gallery restores its original lighting");
            Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled)==1,"There is one active audio listener");
            Phase="Complete";
        }
    }
}
#endif
