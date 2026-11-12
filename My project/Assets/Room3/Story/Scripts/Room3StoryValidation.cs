#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StarterAssets;

namespace Xiaoduan.Room3
{
    [DefaultExecutionOrder(-200)]
    public sealed class Room3StoryValidation : MonoBehaviour
    {
        public string reportPath,captureFolder;
        readonly List<string> checks=new List<string>();
        [Serializable]class Report{public bool passed;public string error;public string[] checks;public int firstFrame,lastFrame;public float minimumY;}
        Report result=new Report{minimumY=100};
        Room3StoryFlow flow;
        Transform player;
        StarterAssetsInputs input;
        public void Begin(){DontDestroyOnLoad(gameObject);StartCoroutine(Safe());}
        IEnumerator Safe()
        {
            var routine=Run();while(true)
            {
                object step;
                try{if(!routine.MoveNext())break;step=routine.Current;}
                catch(Exception e){Finish(e.ToString());yield break;}
                yield return step;
            }
            Finish("");
        }
        void Finish(string error)
        {
            if(input!=null){input.move=Vector2.zero;input.look=Vector2.zero;}
            result.passed=string.IsNullOrEmpty(error);result.error=error;result.checks=checks.ToArray();result.lastFrame=Time.frameCount;
            File.WriteAllText(reportPath,JsonUtility.ToJson(result,true));Debug.Log("ROOM3 STORY TEST: "+result.passed);Destroy(gameObject);
        }
        void Check(bool condition,string name){if(!condition)throw new Exception(name);checks.Add(name);}
        void Find(){flow=FindFirstObjectByType<Room3StoryFlow>();player=GameObject.FindGameObjectWithTag("Player").transform;input=player.GetComponent<StarterAssetsInputs>();}
        void Place(Vector3 p)
        {var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.position=p;cc.enabled=true;Physics.SyncTransforms();input.move=Vector2.zero;input.look=Vector2.zero;}
        void Face(Vector3 point)
        {
            var delta=point-player.position;delta.y=0;if(delta.sqrMagnitude>.01f)player.rotation=Quaternion.LookRotation(delta);
            var fps=player.GetComponent<FirstPersonController>();fps.CinemachineCameraTarget.transform.localRotation=Quaternion.identity;
        }
        IEnumerator Walk(Vector3 target,float timeout=15)
        {
            float deadline=Time.realtimeSinceStartup+timeout;
            while(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(target.x,target.z))>.28f&&Time.realtimeSinceStartup<deadline)
            {Face(target);input.move=Vector2.up;input.look=Vector2.zero;input.jump=false;input.sprint=false;result.minimumY=Mathf.Min(result.minimumY,player.position.y);yield return null;}
            input.move=Vector2.zero;
            if(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(target.x,target.z))>.6f)throw new Exception("Walking route blocked at "+player.position+" before "+target);
        }
        IEnumerator Run()
        {
            result.firstFrame=Time.frameCount;yield return new WaitForSecondsRealtime(.7f);Find();
            Check(flow!=null&&flow.nodes.Length==4,"Four story interactions are connected, including the labor ledger");
            flow.ChooseEnding(true);Check(!flow.EndingChosen,"Final choice is unavailable before the story is completed");
            flow.OpenArchive(1);Check(!flow.Reading,"Court record is unavailable before the bridge is solved");
            flow.TickCalibration(true,Vector3.forward,2);Check(!flow.BridgeLocked,"Bridge cannot unlock before the medical record is decoded");
            var walk=Walk(new Vector3(0,.1f,-15.5f));while(walk.MoveNext())yield return walk.Current;
            walk=Walk(new Vector3(5.5f,.1f,-15.5f));while(walk.MoveNext())yield return walk.Current;
            Check(flow.WorkerView&&flow.workerFigures.All(g=>g.activeSelf),"Crossing the glass breach reveals the hidden workers");
            walk=Walk(new Vector3(5.5f,.1f,-6));while(walk.MoveNext())yield return walk.Current;
            Face(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.MedicalRecord).focus.position);yield return new WaitForSecondsRealtime(.6f);
            Check(flow.ui.promptText.text.Contains("Examine"),"Medical record is reachable through normal facing and distance checks");
            flow.Interact(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.MedicalRecord));
            Check(flow.Reading&&!player.GetComponent<UnityEngine.InputSystem.PlayerInput>().inputIsActive,"Archive reader opens and blocks locomotion input");
            flow.AdvanceArchive(3.2f);Check(flow.MedicalDecoded&&flow.ui.greenRecord.alpha>.99f,"F reveal exposes and retains the medical research layer");
            yield return null;
            Check(!flow.ui.greenText.isTextOverflowing&&!flow.ui.archiveSource.isTextOverflowing,"Medical archive text fits its panel");
            Capture("ROOM3-story-archive.png"); yield return null;
            flow.ui.closeButton.onClick.Invoke();Check(!flow.Reading&&player.GetComponent<UnityEngine.InputSystem.PlayerInput>().inputIsActive,"Close button restores exploration input");
            walk=Walk(new Vector3(6.8f,.1f,-6));while(walk.MoveNext())yield return walk.Current;
            walk=Walk(new Vector3(6.8f,.1f,-2));while(walk.MoveNext())yield return walk.Current;
            walk=Walk(new Vector3(0,.1f,-2));while(walk.MoveNext())yield return walk.Current;
            var memoryRoute=MemoryRouteDriver.Run(flow,player,input,Check);while(memoryRoute.MoveNext())yield return memoryRoute.Current;
            walk=Walk(new Vector3(0,.1f,4));while(walk.MoveNext())yield return walk.Current;
            flow.TickCalibration(true,Vector3.back,2);Check(!flow.BridgeLocked,"Wrong viewing direction does not solve calibration");
            Face(flow.calibrationTarget.position);
            float calibrateUntil=Time.realtimeSinceStartup+4;
            while(!flow.BridgeLocked&&Time.realtimeSinceStartup<calibrateUntil){InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;}
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());
            yield return new WaitForSecondsRealtime(2.7f);
            Check(flow.BridgeLocked&&flow.bridgeColliders.All(c=>c.enabled)&&!flow.bridgeBarrier.enabled,"Aligned bridge settles and enables continuous walking collision");
            Face(new Vector3(0,2,29));yield return new WaitForSecondsRealtime(.2f);Capture("ROOM3-story-bridge.png"); yield return null;
            walk=Walk(new Vector3(0,.1f,29),18);while(walk.MoveNext())yield return walk.Current;
            Check(result.minimumY>-.5f,"No falls or mandatory jumps through the corridor and aligned bridge");
            walk=Walk(new Vector3(-3,.1f,31));while(walk.MoveNext())yield return walk.Current;
            Face(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.CourtRecord).focus.position);yield return new WaitForSecondsRealtime(.5f);
            flow.Interact(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.CourtRecord));flow.AdvanceArchive(3.2f);yield return null;
            Check(flow.CourtDecoded&&flow.ui.archivePicture.texture!=null,"Court archive reveals the actual thesis newspaper clipping");
            Check(!flow.ui.greenText.isTextOverflowing&&!flow.ui.archiveSource.isTextOverflowing,"Court archive chronology and provenance text fit");
            flow.CloseArchive();
            walk=Walk(new Vector3(0,.1f,31));while(walk.MoveNext())yield return walk.Current;
            walk=Walk(new Vector3(0,.1f,38));while(walk.MoveNext())yield return walk.Current;
            flow.TickOath(true,false,2);Check(!flow.OathOpened,"Holding without upward effort does not instantly bypass the oath");
            flow.SetReducedMotion(true);Check(flow.ReducedMotion,"Reduced motion option is available");
            Face(new Vector3(0,5,42));Capture("ROOM3-story-oath.png"); yield return null;
            float oathUntil=Time.realtimeSinceStartup+6;
            while(!flow.OathOpened&&Time.realtimeSinceStartup<oathUntil){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return null;}
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            yield return new WaitForSecondsRealtime(2);
            Check(flow.OathOpened&&!flow.oathBarrier.enabled&&flow.boneLights.All(g=>g.activeSelf),"Four-second testimony opens the door and lights the five silhouettes");
            walk=Walk(new Vector3(0,.1f,46));while(walk.MoveNext())yield return walk.Current;
            walk=Walk(new Vector3(0,.1f,93.5f),22);while(walk.MoveNext())yield return walk.Current;
            Face(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.Decision).focus.position);yield return new WaitForSecondsRealtime(.5f);
            flow.Interact(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.Decision));Check(flow.ui.DecisionVisible,"Core console opens the two functional ending buttons");
            Capture("ROOM3-story-choice.png"); yield return null;flow.ui.formatButton.onClick.Invoke();
            float until=Time.realtimeSinceStartup+45;while(SceneManager.GetActiveScene().name!="ROOM1"&&Time.realtimeSinceStartup<until)yield return null;yield return new WaitForSecondsRealtime(1.5f);
            Check(SceneManager.GetActiveScene().name=="ROOM1"&&flow.Ending=="format"&&flow.ui.endingPanel.activeSelf,"Format ending returns to the town and displays its ending");
            var arrival=FindObjectsByType<TransitionDestination>(FindObjectsSortMode.None).FirstOrDefault(d=>d.gameObject.scene.name=="ROOM1"&&d.destinationTag==TransitionDestination.DestinationTag.ENTER);
            var arrivedPosition=GameObject.FindGameObjectWithTag("Player").transform.position;
            // The capsule settles from the spawn marker to the street after arrival.
            Check(arrival!=null&&Vector2.Distance(new Vector2(arrivedPosition.x,arrivedPosition.z),new Vector2(arrival.transform.position.x,arrival.transform.position.z))<.5f&&Mathf.Abs(arrivedPosition.y-arrival.transform.position.y)<2f,"Format ending places the player at the active town ENTER destination");
            Check(FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).Where(p=>p.gameObject.scene.name=="ROOM1"&&p.sceneName=="ROOM2").All(p=>!p.enabled),"Format ending closes access to the town portal for this playthrough");
            Capture("ROOM3-story-format.png"); yield return null;flow.Restart();yield return new WaitForSecondsRealtime(1);
            Check(FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).Any(p=>p.gameObject.scene.name=="ROOM1"&&p.sceneName=="ROOM2"&&p.enabled),"Restart restores the original town portal");
            SceneManager.LoadScene("ROOM3");yield return new WaitForSecondsRealtime(1);Find();
            flow.OpenArchive(0);flow.AdvanceArchive(3.2f);flow.CloseArchive();Place(new Vector3(0,.15f,-3));yield return new WaitForSecondsRealtime(.3f);
            memoryRoute=MemoryRouteDriver.Run(flow,player,input,Check);while(memoryRoute.MoveNext())yield return memoryRoute.Current;
            Place(new Vector3(0,.15f,4));yield return new WaitForSecondsRealtime(.2f);
            Face(flow.calibrationTarget.position);calibrateUntil=Time.realtimeSinceStartup+4;
            while(!flow.BridgeLocked&&Time.realtimeSinceStartup<calibrateUntil){InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;}
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(2.7f);
            flow.OpenArchive(1);flow.AdvanceArchive(3.2f);flow.CloseArchive();Place(new Vector3(0,.15f,38));yield return new WaitForSecondsRealtime(.2f);
            oathUntil=Time.realtimeSinceStartup+6;
            while(!flow.OathOpened&&Time.realtimeSinceStartup<oathUntil){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.E));yield return null;}
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(2);
            Place(new Vector3(0,.15f,93.5f));flow.Interact(flow.nodes.First(n=>n.kind==Room3StoryNode.Kind.Decision));flow.ui.preserveButton.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
            Check(flow.Ending=="preserve"&&flow.ui.endingPanel.activeSelf,"Preserve ending displays the memorial in ROOM3");
            Check(FindObjectsByType<Room3Motion>(FindObjectsSortMode.None).All(m=>!m.enabled),"Preserve ending stops drifting and rotating architecture");
            Check(flow.memorialParticles.isPlaying&&flow.ui.portraitMollie.texture!=null&&flow.ui.portraitGrace.texture!=null,"Memorial particles and the two supplied historical portraits are present");
            Check(!flow.ui.endingBody.isTextOverflowing&&!flow.ui.endingCredits.isTextOverflowing,"Memorial names and source captions fit the ending panel");
            Capture("ROOM3-story-memorial.png"); yield return null;
            Check(GameObject.FindGameObjectsWithTag("Player").Length==1,"Both endings retain a single player");
            Check(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Both endings retain one active AudioListener");
            Check(Time.frameCount>result.firstFrame+100,"Game frames advanced throughout functional verification");
        }
        void Capture(string name)
        {
            // Screenshots are taken after a rendered frame by the caller where needed.
            ScreenCapture.CaptureScreenshot(Path.Combine(captureFolder,name));
        }
    }
}
#endif


