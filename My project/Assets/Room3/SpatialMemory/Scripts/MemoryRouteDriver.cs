#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StarterAssets;
using Xiaoduan.Survey;

namespace Xiaoduan.Room3
{
    // Editor-only route driver. Uses the production controller and plane-crossing logic.
    public static class MemoryRouteDriver
    {
        static void Face(Transform player,StarterAssetsInputs input,Vector3 target)
        {
            var d=target-Camera.main.transform.position;var flat=Vector3.ProjectOnPlane(d,Vector3.up);
            player.rotation=Quaternion.LookRotation(flat);float pitch=-Mathf.Atan2(d.y,flat.magnitude)*Mathf.Rad2Deg;
            var fps=player.GetComponent<FirstPersonController>();
            typeof(FirstPersonController).GetField("_cinemachineTargetPitch",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fps,pitch);
            fps.CinemachineCameraTarget.transform.localRotation=Quaternion.Euler(pitch,0,0);input.look=Vector2.zero;
        }
        static IEnumerator MoveUntil(Transform player,StarterAssetsInputs input,Vector3 facing,Func<bool> done,float timeout)
        {
            Face(player,input,facing);float deadline=Time.realtimeSinceStartup+timeout;
            while(!done()&&Time.realtimeSinceStartup<deadline){input.move=Vector2.up;input.look=Vector2.zero;input.jump=false;yield return null;}
            input.move=Vector2.zero;
            if(!done())throw new Exception("Recursive route stalled at "+player.position);
        }
        public static IEnumerator Run(Room3StoryFlow flow,Transform player,StarterAssetsInputs input,Action<bool,string> check=null)
        {
            var memory=flow.recursiveArchive;if(memory==null)yield break;
            void Verify(bool condition,string message){if(check!=null)check(condition,message);else if(!condition)throw new Exception(message);}
            flow.OpenArchive(2);Verify(!flow.Reading,"Labor ledger remains concealed before two repetitions");
            flow.TickCalibration(true,Vector3.forward,2);Verify(!flow.BridgeLocked,"Bridge waits for the retained labor record");
            var move=MoveUntil(player,input,new Vector3(0,1.5f,20),()=>memory.Inside,8);
            while(move.MoveNext())yield return move.Current;
            yield return new WaitForSecondsRealtime(.25f);
            Verify(Mathf.Abs(player.position.x-80)<.8f&&Vector3.Dot(player.forward,Vector3.forward)>.99f,"Entry maps position and forward direction into the repeated corridor");
            Verify(memory.RenderedViews>0&&!memory.RenderingFailed,"URP portal views render through the mapped opening");
            yield return new WaitForSecondsRealtime(2);
            Face(player,input,memory.repetition.transform.position);yield return new WaitForSecondsRealtime(.2f);
            int.TryParse(SurveyHUD.Instance.cpm.text.Split('\n')[0],out int facingReading);
            Face(player,input,new Vector3(80,1.5f,-45));yield return new WaitForSecondsRealtime(.2f);
            int.TryParse(SurveyHUD.Instance.cpm.text.Split('\n')[0],out int awayReading);
            Verify(facingReading>awayReading+50&&!SurveyHUD.Instance.cpm.text.Contains("OVERFLOW"),"Overflow expires and the ROOM3 meter responds to viewing direction");
            move=MoveUntil(player,input,new Vector3(80,1.5f,-30),()=>!memory.Inside,4);
            while(move.MoveNext())yield return move.Current;
            Verify(Mathf.Abs(player.position.x)<.8f&&player.position.z<0,"Turning back through the arrival threshold restores the original corridor");
            move=MoveUntil(player,input,new Vector3(0,1.5f,20),()=>memory.Inside,4);
            while(move.MoveNext())yield return move.Current;
            yield return new WaitForSecondsRealtime(.15f);
            string captures=UnityEditor.SessionState.GetString("XD.Memory.QAFolder","");
            if(captures!="")ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(captures,"Memory-corridor-clean.png"));
            for(int i=0;i<2;i++)
            {
                int expected=i+1;move=MoveUntil(player,input,new Vector3(80,1.5f,12),()=>memory.Passes>=expected,12);
                while(move.MoveNext())yield return move.Current;
                yield return new WaitForSecondsRealtime(.25f);
                Verify(player.position.z< -18&&memory.Inside,"Repetition "+expected+" returns through the same corridor without loading another scene");
            }
            int views=memory.RenderedViews,passes=memory.Traversals;var pause=UnityEngine.Object.FindFirstObjectByType<SurveyPause>();
            var witness=memory.GetComponentInChildren<RepeatedDialWitness>();
            Verify(witness!=null&&witness.firstCrack.activeSelf&&witness.secondCrack.activeSelf,"The fixed inspection dial visibly changes across both repetitions");
            pause.Open();yield return new WaitForSecondsRealtime(.25f);
            Verify(memory.RenderedViews==views&&memory.Traversals==passes,"Pause freezes portal rendering and traversal");pause.Resume();
            move=MoveUntil(player,input,new Vector3(80,1.5f,5),()=>player.position.z>=-10.1f,8);
            while(move.MoveNext())yield return move.Current;
            Face(player,input,memory.ledger.focus.position);yield return new WaitForSecondsRealtime(.3f);
            if(captures!="")ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(captures,"Memory-wall-uncovered.png"));
            yield return null;
            Verify(flow.ui.promptText.text.Contains("Examine"),"Workstation ledger is reachable through normal facing and distance checks");
            flow.Interact(memory.ledger);Verify(flow.Reading&&!player.GetComponent<PlayerInput>().inputIsActive,"Labor reader opens and blocks locomotion");
            Verify(SurveyHUD.Instance.hud.alpha==0&&!SurveyHUD.Instance.banner.activeSelf,"Archive opening immediately hides exploration HUD and warnings");
            float until=Time.realtimeSinceStartup+3.4f;
            while(!memory.RecordDecoded&&Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.F));yield return null;}
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            Verify(memory.RecordDecoded&&memory.repetition.linked==memory.release,"Holding F retains the comparison and changes the exit connection");
            Verify(UnityEngine.Object.FindFirstObjectByType<SurveyAudioFocus>().WorldGain<.4f,"World sound is reduced during archive reading");
            SurveyTypography.Instance.SetLargeText(true);yield return null;Canvas.ForceUpdateCanvases();
            Verify(!flow.ui.greenText.isTextOverflowing&&!flow.ui.archiveSource.isTextOverflowing,"Labor research and sources fit the large-text reader");
            if(captures!=""){ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(captures,"Memory-ledger-large.png"));yield return null;}
            SurveyTypography.Instance.SetLargeText(false);flow.CloseArchive();
            yield return new WaitForSecondsRealtime(.35f);
            Verify(UnityEngine.Object.FindFirstObjectByType<SurveyAudioFocus>().WorldGain>.95f,"Closing the reader restores the world sound mix");
            Verify(player.GetComponent<PlayerInput>().inputIsActive,"Closing the labor reader restores exploration input");
            move=MoveUntil(player,input,new Vector3(80,1.5f,12),()=>!memory.Inside,9);
            while(move.MoveNext())yield return move.Current;
            yield return new WaitForSecondsRealtime(.25f);
            Verify(Mathf.Abs(player.position.x)<.8f&&player.position.z>2&&player.position.z<4,"Released exit returns to the existing bridge landing");
            var block=new MaterialPropertyBlock();memory.skins[0].GetPropertyBlock(block);
            Verify(block.GetFloat("_Erosion")>.65f,"Repeated observation exposes the mineral skin and internal circuit layer");
        }
    }
}
#endif
