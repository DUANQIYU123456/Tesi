#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using StarterAssets;
using Xiaoduan.Narrative;
using Xiaoduan.Room3;
namespace Xiaoduan.Survey
{
 public sealed class SurveyAuditValidation:MonoBehaviour
 {
  public string outputDirectory;
  public bool formatEnding;
  readonly List<string> checks=new List<string>();
  SurveyHUD ui;Transform player;
  [Serializable]class Report{public bool passed;public string error;public string[] checks;public int frames;}
  int firstFrame;
  SurveyPause pause;SurveyTypography typography;
  bool oldMotion,oldLarge;float oldVolume,oldSensitivity;
  readonly Dictionary<string,bool> prefExists=new Dictionary<string,bool>();
  void RestoreSettings()
  {
   if(ui!=null)ui.SetReducedMotion(oldMotion);
   if(typography!=null)typography.SetLargeText(oldLarge);
   if(pause!=null){if(pause.Paused)pause.Resume();pause.volumeSlider.value=oldVolume;pause.sensitivitySlider.value=oldSensitivity;}
   foreach(var item in prefExists)if(!item.Value)PlayerPrefs.DeleteKey(item.Key);
   PlayerPrefs.Save();
  }
  public void Begin(){DontDestroyOnLoad(gameObject);StartCoroutine(Safe());}
  IEnumerator Safe()
  {
   firstFrame=Time.frameCount;var routine=Run();string error="";
   while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception e){error=e.ToString();break;}yield return current;}
   RestoreSettings();
   InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
   File.WriteAllText(Path.Combine(outputDirectory,formatEnding?"Survey-audit-format.json":"Survey-audit.json"),JsonUtility.ToJson(new Report{passed=error=="",error=error,checks=checks.ToArray(),frames=Time.frameCount-firstFrame},true));
   Debug.Log("SURVEY UI VALIDATION: "+(error==""));Destroy(gameObject);
  }
  void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add(name);}
  void Find(){ui=SurveyHUD.Instance;player=GameObject.FindGameObjectWithTag("Player").transform;}
  void Place(Vector3 position){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.position=position;cc.enabled=true;Physics.SyncTransforms();}
  void Face(Vector3 target)
  {
   var eye=Camera.main;Vector3 delta=target-eye.transform.position;Vector3 flat=Vector3.ProjectOnPlane(delta,Vector3.up);player.rotation=Quaternion.LookRotation(flat);
   var fps=player.GetComponent<FirstPersonController>();float pitch=-Mathf.Atan2(delta.y,flat.magnitude)*Mathf.Rad2Deg;
   typeof(FirstPersonController).GetField("_cinemachineTargetPitch",BindingFlags.NonPublic|BindingFlags.Instance)?.SetValue(fps,pitch);
   fps.CinemachineCameraTarget.transform.localRotation=Quaternion.Euler(pitch,0,0);
  }
  IEnumerator KeyFor(Key key,float seconds)
  {
   float until=Time.realtimeSinceStartup+seconds;
   while(Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;}
   InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
  }
  IEnumerator MouseFor(float seconds)
  {
   float until=Time.realtimeSinceStartup+seconds;
   while(Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;}
   InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
  }
  void Capture(string name){ScreenCapture.CaptureScreenshot(Path.Combine(outputDirectory,name));}
  IEnumerator Run()
  {
   yield return new WaitForSecondsRealtime(.8f);Find();
   pause=ui.GetComponent<SurveyPause>();typography=ui.GetComponent<SurveyTypography>();
   oldMotion=SurveyProgress.ReducedMotion;oldLarge=typography.LargeText;oldVolume=pause.volumeSlider.value;oldSensitivity=pause.sensitivitySlider.value;
   foreach(var key in new[]{"ReducedMotion","LargeText","Volume","Sensitivity"})prefExists["XD.Survey."+key]=PlayerPrefs.HasKey("XD.Survey."+key);
   Check(typography!=null&&ui.truthText.font==typography.bodyFont&&ui.cpm.font==typography.instrumentFont,"IBM Plex body and instrument fonts are connected");
   Check(typography.bodyFont.atlasPopulationMode==TMPro.AtlasPopulationMode.Static,"Primary font uses a prebuilt static SDF atlas");
   Check(ui!=null&&ui.MenuVisible&&SurveyProgress.Modal,"Town starts with the Bloodlines menu and locomotion locked");
   var data=new PointerEventData(EventSystem.current);ExecuteEvents.Execute(ui.startButton.gameObject,data,ExecuteEvents.pointerEnterHandler);
   Check(ui.startButton.GetComponent<UnityEngine.UI.Image>().color.g>.9f,"Menu hover switches the imported artwork to green");
   Capture("Survey-menu.png");yield return null;
   ExecuteEvents.Execute(ui.startButton.gameObject,data,ExecuteEvents.pointerClickHandler);yield return null;
   Check(!ui.MenuVisible&&!SurveyProgress.Modal&&player.GetComponent<PlayerInput>().inputIsActive,"Begin inspection restores first-person controls");
   var esc=KeyFor(Key.Escape,.1f);while(esc.MoveNext())yield return esc.Current;
   Check(pause.Paused&&Time.timeScale==0&&AudioListener.pause&&!player.GetComponent<PlayerInput>().inputIsActive,"Escape pauses world time, audio and first-person input");
   float frozenTime=Time.time;yield return new WaitForSecondsRealtime(.25f);Check(Time.time==frozenTime,"World time remains frozen while the settings panel is open");
   if(!SurveyProgress.ReducedMotion)pause.motionButton.onClick.Invoke();
   if(!typography.LargeText)pause.readingButton.onClick.Invoke();
   pause.volumeSlider.value=.6f;pause.sensitivitySlider.value=1.4f;
   Check(Mathf.Approximately(AudioListener.volume,.6f)&&typography.LargeText&&SurveyProgress.ReducedMotion,"Volume, large reading text and reduced-motion controls take effect");
   Capture("Survey-pause.png");yield return null;
   esc=KeyFor(Key.Escape,.1f);while(esc.MoveNext())yield return esc.Current;
   Check(!pause.Paused&&Time.timeScale==1&&!AudioListener.pause&&player.GetComponent<PlayerInput>().inputIsActive,"Escape resumes time, audio, cursor capture and movement");
   Check(FindObjectsByType<SurveyNode>(FindObjectsSortMode.None).All(n=>!SurveyHUD.Eligible(n,"ROOM2")||n.kind==SurveyNodeKind.Bottle),"Initial prison guidance excludes locked audio, records and portal targets");
   var traces=FindFirstObjectByType<SurveyTrace>();Check(traces!=null&&traces.GetComponentsInChildren<Renderer>().Length>=6,"Town has ground-aligned isotope footsteps");
   Check(traces.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Isotope footsteps are hidden in normal view");
   var hold=MouseFor(.25f);while(hold.MoveNext())yield return hold.Current;
   InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
   Check(ui.Vision&&traces.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"Holding RMB reveals the footprints and viewfinder");Capture("Survey-town-vision.png");yield return null;
   InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
   var portal=FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM2");
   SceneController.GetOrCreate().TransitionToDestination(portal);
   float until=Time.realtimeSinceStartup+50;while((SceneManager.GetActiveScene().name!="ROOM2"||SceneController.Instance.IsTransitioning)&&Time.realtimeSinceStartup<until)yield return null;
   yield return new WaitForSecondsRealtime(.7f);Find();
   Check(SceneManager.GetActiveScene().name=="ROOM2"&&SurveyProgress.Started&&!ui.MenuVisible,"Portal carries the same interface into ROOM2 without reopening the menu");
   Check(!FindFirstObjectByType<SpatialScanEvent>().screenShockEnabled,"Reduced motion carries from the town into the prison scan");
   Check(Mathf.Abs(player.GetComponent<FirstPersonController>().RotationSpeed-1.4f)<.01f,"Carried player sensitivity is not multiplied twice at the prison portal");
   var exit=FindObjectsByType<SurveyPortalGate>(FindObjectsSortMode.None).Single();Check(!exit.portal.enabled,"Archive exit is locked while three prison traces remain incomplete");
   Place(new Vector3(-5.5f,36.95f,-121));yield return new WaitForSecondsRealtime(.3f);Face(exit.portal.transform.position+Vector3.up*1.5f);yield return new WaitForSecondsRealtime(.4f);
   Check(ui.Guidance=="BOTTLE","At a locked exit the live compass still guides back to the missing bottle");
   Check(ui.GateHintVisible&&ui.decoderHint.text.Contains("XII"),"Locked portal displays its missing trace requirements instead of failing silently");
   Capture("Survey-gate-locked.png");yield return null;
   var bottle=FindObjectsByType<NarrativeInspectable>(FindObjectsSortMode.None).First(n=>n.GetComponent<SurveyNode>()?.kind==SurveyNodeKind.Bottle);
   Check(SurveyHUD.TryOpenInspectable(bottle),"Existing bottle inspection is routed to the new interactive reader");
   Check(ui.IsReading&&SurveyProgress.Modal&&!player.GetComponent<PlayerInput>().inputIsActive,"New reader blocks movement and unlocks the cursor");
   Check(ui.scratch.GetComponent<CanvasRenderer>()!=null&&ui.wave.GetComponent<CanvasRenderer>()!=null,"Scratch mask and waveform have real CanvasRenderers");
   yield return null;Check(ui.focusMarker.gameObject.activeSelf&&ui.focusSound!=null,"Bottle focus ring and original tactile sound are mounted");
   Check(!ui.officialText.isTextOverflowing,"Official archive layer fits in large reading mode before reveal");
   hold=KeyFor(Key.F,2.15f);while(hold.MoveNext())yield return hold.Current;
   Check(SurveyProgress.Has(0)&&ui.DecodeProgress>=1,"Real F hold reveals the lip-pointing research and retains XII");
   yield return null;Capture("Survey-bottle.png");yield return null;
   Check(!ui.truthText.isTextOverflowing&&!ui.sourceText.isTextOverflowing,"Bottle research and source text fit the archive panel");
   Check(ui.truthText.fontSize>29&&!ui.truthText.enableAutoSizing,"Large reading mode increases body size without silently shrinking it");
   Check(FindObjectsByType<SurveyNode>(FindObjectsSortMode.None).Where(n=>SurveyHUD.Eligible(n,"ROOM2")).All(n=>n.kind==SurveyNodeKind.Watches||n.kind==SurveyNodeKind.Stamping),"After XII the compass selects only unresolved labor echoes");
   esc=KeyFor(Key.Escape,.1f);while(esc.MoveNext())yield return esc.Current;
   Check(!pause.Paused,"Closing an archive with Escape does not also open pause settings");
   Check(!SurveyProgress.Modal&&player.GetComponent<PlayerInput>().inputIsActive,"Closing the reader restores input");
   foreach(bool left in new[]{true,false})
   {
    var sound=FindObjectsByType<SurveyNode>(FindObjectsSortMode.None).Single(n=>n.kind==(left?SurveyNodeKind.Watches:SurveyNodeKind.Stamping));
    Place(sound.Position+new Vector3(left?3.5f:-3.5f,-.75f,0));yield return new WaitForSecondsRealtime(.25f);Face(sound.Position);yield return new WaitForSecondsRealtime(.25f);
    hold=MouseFor(2.25f);while(hold.MoveNext())yield return hold.Current;
    Check(left?SurveyProgress.LeftTuned:SurveyProgress.RightTuned,(left?"Left clocks":"Right stamping")+" decodes after real RMB alignment");
   }
   Check(SurveyProgress.Has(1),"Decoding both distinct cells retains III");Capture("Survey-decoder.png");yield return null;
   Place(new Vector3(-5.5f,36.95f,-155));yield return new WaitForSecondsRealtime(.25f);Face(new Vector3(-5.5f,40,-120));yield return new WaitForSecondsRealtime(.25f);Capture("Survey-prison-HUD.png");yield return null;
   Place(new Vector3(-5.5f,36.95f,-158));yield return new WaitForSecondsRealtime(.25f);
   var scanner=FindFirstObjectByType<SpatialScanEvent>();scanner.BeginScan();yield return new WaitForSecondsRealtime(3.25f);
   var medical=FindFirstObjectByType<SurveyScanReveal>();Check(medical.surface.enabled&&medical.inspectable.enabled,"Scan reveals the concealed medical file");
   Place(new Vector3(-10.8f,36.95f,-125));yield return new WaitForSecondsRealtime(.3f);Face(medical.transform.position);yield return new WaitForSecondsRealtime(.4f);
   Check(NarrativeInteractor.Active.CurrentTarget==medical.inspectable,"Medical file can be selected through normal distance, facing and obstruction checks");
   ui.OpenArchive(2);yield return null;
   Check(!ui.officialText.isTextOverflowing,"Medical official layer fits before scratching");
   Check(ui.scratch.canvasRenderer!=null&&ui.scratch.gameObject.activeInHierarchy,"Medical redaction is visible and accepts pointer input");
   yield return null;Check(ui.brushCursor.gameObject.activeSelf&&!Cursor.visible,"Medical archive replaces the pointer with its green brush");
   var probe=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,ui.scratch.rectTransform.position)};
   var raycasts=new List<RaycastResult>();EventSystem.current.RaycastAll(probe,raycasts);
   Check(raycasts.Count>0&&raycasts[0].gameObject==ui.scratch.gameObject,"Real UI raycasts reach the scratch coating before the modal backdrop");
   Capture("Survey-redaction.png");yield return null;
   var rect=ui.scratch.rectTransform;var r=rect.rect;
   for(int stroke=0;stroke<3;stroke++)
   {
    float y=Mathf.Lerp(r.yMin+30,r.yMax-30,(stroke+.5f)/3);
    var p=new PointerEventData(EventSystem.current);p.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(r.xMin+10,y)));ui.scratch.OnPointerDown(p);
    for(int i=1;i<=24;i++){p.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector3(Mathf.Lerp(r.xMin+10,r.xMax-10,i/24f),y)));ui.scratch.OnDrag(p);}
    ui.scratch.OnPointerUp(p);
   }
   Check(SurveyProgress.Has(2)&&ui.DecodeProgress>=1,"Three actual brush strokes remove the redaction and retain VI");
   yield return null;Capture("Survey-medical.png");yield return null;
   Check(!ui.truthText.isTextOverflowing&&!ui.sourceText.isTextOverflowing,"Medical truth and provenance fit the panel");ui.CloseArchive();yield return null;
   Check(exit.portal.enabled,"Three prison traces unlock the real ROOM3 portal");
   ui.OpenArchive(2);yield return null;Check(ui.archiveHint.text.Contains("TRACE RETAINED")&&!ui.scratch.gameObject.activeSelf,"Reopening a decoded medical record uses retained-state instructions");ui.CloseArchive();
   SceneController.GetOrCreate().TransitionToDestination(exit.portal);
   until=Time.realtimeSinceStartup+50;while((SceneManager.GetActiveScene().name!="ROOM3"||SceneController.Instance.IsTransitioning)&&Time.realtimeSinceStartup<until)yield return null;
   yield return new WaitForSecondsRealtime(.8f);Find();
   pause=ui.GetComponent<SurveyPause>();typography=ui.GetComponent<SurveyTypography>();
   oldMotion=SurveyProgress.ReducedMotion;oldLarge=typography.LargeText;oldVolume=pause.volumeSlider.value;oldSensitivity=pause.sensitivitySlider.value;
   foreach(var key in new[]{"ReducedMotion","LargeText","Volume","Sensitivity"})prefExists["XD.Survey."+key]=PlayerPrefs.HasKey("XD.Survey."+key);
   Check(typography!=null&&ui.truthText.font==typography.bodyFont&&ui.cpm.font==typography.instrumentFont,"IBM Plex body and instrument fonts are connected");
   Check(typography.bodyFont.atlasPopulationMode==TMPro.AtlasPopulationMode.Static,"Primary font uses a prebuilt static SDF atlas");var flow=FindFirstObjectByType<Room3StoryFlow>();
   Check(Mathf.Abs(player.GetComponent<FirstPersonController>().RotationSpeed-1.4f)<.01f,"New ROOM3 player receives the selected sensitivity once");
   esc=KeyFor(Key.Escape,.1f);while(esc.MoveNext())yield return esc.Current;
   Check(pause.Paused,"Pause is available inside ROOM3");float before=flow.CalibrationProgress;
   var pausedHold=MouseFor(.25f);while(pausedHold.MoveNext())yield return pausedHold.Current;
   Check(flow.CalibrationProgress==before&&!flow.BridgeLocked,"Paused input does not advance ROOM3 interaction progress");
   pause.Resume();
   Check(flow!=null&&flow.MedicalDecoded&&SurveyProgress.Count==3,"ROOM3 inherits the three retained traces and medical unlock");
   Check(FindObjectsByType<SurveyHUD>(FindObjectsSortMode.None).Length==1,"Scene changes retain exactly one global UI");
   Check(GameObject.FindGameObjectsWithTag("Player").Length==1&&FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1,"Portals retain one player and one listener");
   Place(new Vector3(0,.15f,4));yield return new WaitForSecondsRealtime(.3f);Face(flow.calibrationTarget.position);yield return new WaitForSecondsRealtime(.2f);
   hold=MouseFor(1.5f);while(hold.MoveNext())yield return hold.Current;yield return new WaitForSecondsRealtime(2.7f);
   Check(flow.BridgeLocked&&flow.bridgeColliders.All(x=>x.enabled),"Existing bridge calibration remains playable after UI replacement");
   flow.OpenArchive(1);hold=KeyFor(Key.F,3.2f);while(hold.MoveNext())yield return hold.Current;
   Check(flow.CourtDecoded&&SurveyProgress.Has(3)&&SurveyProgress.Count==4,"Court archive retains IX in the shared four-fragment watch");
   yield return null;Check(!flow.ui.greenText.isTextOverflowing,"Court reader text still fits after reskin");flow.CloseArchive();
   Place(new Vector3(0,.15f,38));yield return new WaitForSecondsRealtime(.3f);
   hold=KeyFor(Key.E,4.3f);while(hold.MoveNext())yield return hold.Current;yield return new WaitForSecondsRealtime(2);
   Check(flow.OathOpened&&!flow.oathBarrier.enabled,"Existing accessible oath opens its gate after UI replacement");
   flow.Interact(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.Decision));yield return null;
   Check(flow.ui.DecisionVisible,"Original final-choice panel remains functional");
   ExecuteEvents.Execute(flow.ui.formatButton.gameObject,data,ExecuteEvents.pointerEnterHandler);
   Check(ui.Preview==0&&ui.previewAudio.isPlaying,"Restore-order hover plays its false music-box preview");
   ExecuteEvents.Execute(flow.ui.preserveButton.gameObject,data,ExecuteEvents.pointerEnterHandler);
   Check(ui.Preview==1&&ui.previewAudio.isPlaying,"Guard-ruins hover plays its watch-chime preview");
   Capture("Survey-choice.png");yield return null;
   if(formatEnding)
   {
    flow.ui.formatButton.onClick.Invoke();until=Time.realtimeSinceStartup+45;
    while((SceneManager.GetActiveScene().name!="ROOM1"||!flow.ui.endingPanel.activeSelf)&&Time.realtimeSinceStartup<until)yield return null;
    yield return new WaitForSecondsRealtime(.5f);
    Check(flow.Ending=="format"&&flow.ui.endingPanel.activeSelf,"Reskinned restore-order button completes the town ending");
    Check(FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).Where(p=>p.sceneName=="ROOM2").All(p=>!p.enabled),"Restore-order ending actually closes the town portal");Capture("Survey-format.png");yield return null;
   }
   else
   {
    flow.ui.preserveButton.onClick.Invoke();yield return new WaitForSecondsRealtime(2.2f);
    Check(flow.Ending=="preserve"&&flow.ui.endingPanel.activeSelf,"Reskinned preserve button completes the memorial ending");Capture("Survey-memorial.png");yield return null;
    Check(FindObjectsByType<Room3Motion>(FindObjectsSortMode.None).All(x=>!x.enabled),"Memorial ending still stops the architecture");
   }
   flow.Restart();yield return new WaitForSecondsRealtime(1.2f);Find();
   Check(SurveyProgress.Count==0&&SurveyProgress.Started&&!ui.MenuVisible&&!SurveyProgress.Modal,"Restart clears shared traces and restores exploration without an input lock");
   Check(FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).Any(p=>p.sceneName=="ROOM2"&&p.enabled),"New playthrough restores the town portal");
  }
 }
}
#endif
