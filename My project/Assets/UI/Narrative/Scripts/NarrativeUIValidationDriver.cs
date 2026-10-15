#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Xiaoduan.Narrative;

/// <summary>Editor-only integration check. Never created in normal play or player builds.</summary>
public sealed class NarrativeUIValidationDriver : MonoBehaviour
{
    [Serializable] sealed class Result
    {
        public string status,message,portalTarget,observedObject;
        public string version="english-v3";
        public bool portalPrompt,sceneTransition,oneUI,readingOpened,layoutFits,inputBlocked,inputRestored,closedByE,closedByEscape,closedByButton,throughWallBlocked;
        public string[] screenshots;
    }
    Result report=new Result {status="running"};
    float deadline;
    bool done;
    void Awake() { DontDestroyOnLoad(gameObject); deadline=Time.realtimeSinceStartup+100; }
    void Update() { if (!done && Time.realtimeSinceStartup>deadline) Finish("error","Integration test timed out."); }
    IEnumerator Start()
    {
        Directory.CreateDirectory("Library/NarrativeUIQA");
        yield return new WaitForSecondsRealtime(2);
        var player=GameObject.FindGameObjectWithTag("Player");
        if (player==null) { Finish("error","ROOM1 player missing."); yield break; }
        var portal=FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).FirstOrDefault(p=>p.sceneName=="ROOM2" && p.isActiveAndEnabled);
        if (portal==null) { Finish("error","ROOM1 portal to ROOM2 missing."); yield break; }
        PrepareRig(player);
        var bounds=portal.GetComponent<Collider>().bounds;
        Vector3 feet=bounds.center-portal.transform.forward*.9f-Vector3.up;
        Warp(player,feet,feet+Vector3.up*1.6f,bounds.center);
        yield return new WaitForSecondsRealtime(.8f);
        var ui=NarrativeInteractor.Active;
        if (ui==null) { Finish("error","Narrative interface not active."); yield break; }
        report.portalPrompt=ui.CurrentTarget==portal && ui.view.promptGroup.alpha>.95f;
        report.portalTarget=portal.name;
        if (!report.portalPrompt) { Finish("error","Standing in portal did not produce its prompt."); yield break; }
        ScreenCapture.CaptureScreenshot("Library/NarrativeUIQA/01_PortalPrompt.png");
        yield return new WaitForSecondsRealtime(.5f);
        yield return Press(Key.E);
        float until=Time.realtimeSinceStartup+40;
        while (SceneManager.GetActiveScene().name!="ROOM2" && Time.realtimeSinceStartup<until) yield return null;
        yield return new WaitForSecondsRealtime(1.5f);
        report.sceneTransition=SceneManager.GetActiveScene().name=="ROOM2";
        if (!report.sceneTransition) { Finish("error","E did not reach ROOM2."); yield break; }
        player=GameObject.FindGameObjectWithTag("Player");
        PrepareRig(player);
        report.oneUI=FindObjectsByType<NarrativeInteractor>(FindObjectsSortMode.None).Length==1 &&
            FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length==1;
        var bottles=FindObjectsByType<NarrativeInspectable>(FindObjectsSortMode.None);
        NarrativeInspectable selected=null;
        foreach (var bottle in bottles)
        {
            foreach (var side in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})
            {
                Vector3 target=bottle.FocusBounds.center,eye=target+side*1.8f;
                Warp(player,eye-Vector3.up*1.6f,eye,target);
                yield return new WaitForSecondsRealtime(.35f);
                ui=NarrativeInteractor.Active;
                selected=ui.CurrentTarget as NarrativeInspectable;
                if (selected!=null) break;
            }
            if (selected!=null) break;
        }
        if (selected==null) { Finish("error","No bottle could be focused from nearby clear positions."); yield break; }
        report.observedObject=selected.name;
        ScreenCapture.CaptureScreenshot("Library/NarrativeUIQA/02_ExaminePrompt.png");
        yield return new WaitForSecondsRealtime(.5f);
        yield return Press(Key.E);
        yield return new WaitForSecondsRealtime(.4f);
        report.readingOpened=ui.IsReading && ui.view.readingGroup.alpha>.95f && ui.view.cardBody.text==selected.entry.body;
        ui.view.cardBody.ForceMeshUpdate();
        report.layoutFits=ui.view.cardBody.textBounds.size.y<=ui.view.cardBody.rectTransform.rect.height;
        report.inputBlocked=!player.GetComponent<PlayerInput>().inputIsActive && !player.GetComponent<StarterAssetsInputs>().cursorInputForLook;
        ScreenCapture.CaptureScreenshot("Library/NarrativeUIQA/03_ObservationCard.png");
        yield return new WaitForSecondsRealtime(.5f);
        yield return Press(Key.E);
        report.closedByE=!ui.IsReading;
        report.inputRestored=player.GetComponent<PlayerInput>().inputIsActive && player.GetComponent<StarterAssetsInputs>().cursorInputForLook;
        yield return new WaitForSecondsRealtime(.3f);
        yield return Press(Key.E); yield return new WaitForSecondsRealtime(.3f); yield return Press(Key.Escape);
        report.closedByEscape=!ui.IsReading;
        yield return new WaitForSecondsRealtime(.3f);
        yield return Press(Key.E); yield return new WaitForSecondsRealtime(.3f);
        bool wasOpen=ui.IsReading;
        var buttonRect=(RectTransform)ui.view.closeButton.transform;
        Vector2 buttonPosition=RectTransformUtility.WorldToScreenPoint(null,buttonRect.TransformPoint(buttonRect.rect.center));
        InputSystem.QueueStateEvent(Mouse.current,new MouseState {position=buttonPosition});
        yield return null; yield return null;
        InputSystem.QueueStateEvent(Mouse.current,new MouseState {position=buttonPosition}.WithButton(MouseButton.Left));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(Mouse.current,new MouseState {position=buttonPosition});
        yield return null; yield return null;
        report.closedByButton=wasOpen && !ui.IsReading;
        // Put a temporary opaque collider between the eye and the current bottle.
        var eyePosition=Camera.main.transform.position;
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name="Narrative QA temporary blocker";
        blocker.transform.position=Vector3.Lerp(eyePosition,selected.FocusBounds.center,.5f);
        blocker.transform.localScale=new Vector3(3,3,.4f);
        blocker.transform.rotation=Quaternion.LookRotation(selected.FocusBounds.center-eyePosition);
        Physics.SyncTransforms();
        yield return new WaitForSecondsRealtime(.3f);
        report.throughWallBlocked=ui.CurrentTarget==null;
        Destroy(blocker);
        report.screenshots=new[]{"Library/NarrativeUIQA/01_PortalPrompt.png","Library/NarrativeUIQA/02_ExaminePrompt.png","Library/NarrativeUIQA/03_ObservationCard.png"};
        bool passed=report.portalPrompt && report.sceneTransition && report.oneUI && report.readingOpened && report.layoutFits && report.inputBlocked &&
            report.inputRestored && report.closedByE && report.closedByEscape && report.closedByButton && report.throughWallBlocked;
        Finish(passed ? "passed" : "failed","Actual ROOM1 to ROOM2 interaction test. Test-only navigation overrides are discarded on leaving Play.");
    }
    static void PrepareRig(GameObject player)
    {
        var controller=player.GetComponent<FirstPersonController>(); if (controller!=null) controller.enabled=false;
        var brain=Camera.main.GetComponent<CinemachineBrain>(); if (brain!=null) brain.enabled=false;
    }
    static void Warp(GameObject player,Vector3 feet,Vector3 eye,Vector3 target)
    {
        var cc=player.GetComponent<CharacterController>(); cc.enabled=false;
        player.transform.position=feet; cc.enabled=true; cc.Move(Vector3.down*.001f);
        Camera.main.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
        Physics.SyncTransforms();
    }
    static IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));
        yield return null; yield return null;
        InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        yield return null; yield return null;
    }
    void Finish(string status,string message)
    {
        if (done) return;
        done=true; report.status=status; report.message=message;
        File.WriteAllText("Library/XiaoduanNarrativeUIRuntime.json",JsonUtility.ToJson(report,true));
        Debug.Log("[NarrativeUI QA] "+status+": "+message);
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
