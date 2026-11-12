#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StarterAssets;
namespace Xiaoduan.Room3
{
    public sealed class Room3MouseValidation : MonoBehaviour
    {
        public string reportPath;
        public void Begin(){StartCoroutine(Check());}
        IEnumerator Check()
        {
            yield return new WaitForSecondsRealtime(.7f);
            var f=FindFirstObjectByType<Room3StoryFlow>();var player=GameObject.FindGameObjectWithTag("Player");
            void Place(Vector3 p){var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.SetPositionAndRotation(p,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();}
            f.OpenArchive(0);f.AdvanceArchive(3.2f);f.CloseArchive();Place(new Vector3(0,.15f,-3));yield return new WaitForSecondsRealtime(.3f);
            var memory=MemoryRouteDriver.Run(f,player.transform,player.GetComponent<StarterAssetsInputs>());while(memory.MoveNext())yield return memory.Current;
            Place(new Vector3(0,.15f,4));yield return new WaitForSecondsRealtime(.2f);
            // Calibration requires facing the suspended ring before holding RMB.
            // Reset the controller's stored pitch as well as the visible target.
            var eye=Camera.main;var delta=f.calibrationTarget.position-eye.transform.position;
            var flat=Vector3.ProjectOnPlane(delta,Vector3.up);player.transform.rotation=Quaternion.LookRotation(flat);
            var fps=player.GetComponent<FirstPersonController>();float pitch=-Mathf.Atan2(delta.y,flat.magnitude)*Mathf.Rad2Deg;
            typeof(FirstPersonController).GetField("_cinemachineTargetPitch",BindingFlags.NonPublic|BindingFlags.Instance)?.SetValue(fps,pitch);
            fps.CinemachineCameraTarget.transform.localRotation=Quaternion.Euler(pitch,0,0);
            player.GetComponent<StarterAssetsInputs>().look=Vector2.zero;
            yield return new WaitForSecondsRealtime(.2f);
            float until=Time.realtimeSinceStartup+4;float bestDot=-1;int rightFrames=0,modalFrames=0;float nearest=100;
            while(!f.BridgeLocked&&Time.realtimeSinceStartup<until)
            {
                InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;
                bestDot=Mathf.Max(bestDot,Vector3.Dot(Camera.main.transform.forward,(f.calibrationTarget.position-Camera.main.transform.position).normalized));
                nearest=Mathf.Min(nearest,Vector3.Distance(player.transform.position,f.calibrationPoint.position));
                if(Mouse.current.rightButton.isPressed)rightFrames++;
                if(Xiaoduan.Survey.SurveyHUD.BlocksWorld)modalFrames++;
            }
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(2.8f);
            f.OpenArchive(1);f.AdvanceArchive(3.2f);f.CloseArchive();Place(new Vector3(0,.15f,38));yield return new WaitForSecondsRealtime(.3f);
            f.SetReducedMotion(false);until=Time.realtimeSinceStartup+6;float maxOffset=0;
            while(!f.OathOpened&&Time.realtimeSinceStartup<until)
            {
                var state=new MouseState().WithButton(MouseButton.Left);state.delta=new Vector2(0,2);InputSystem.QueueStateEvent(Mouse.current,state);
                maxOffset=Mathf.Max(maxOffset,f.cameraInterference.downwardDegrees);yield return null;
            }
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(2);
            bool clampRestored=player.GetComponent<FirstPersonController>().BottomClamp<=-89;
            bool passed=f.BridgeLocked&&f.OathOpened&&!f.oathBarrier.enabled&&maxOffset>0&&clampRestored;
            Place(new Vector3(0,.15f,93.5f));
            f.Interact(f.nodes.First(n=>n.kind==Room3StoryNode.Kind.Decision));f.ui.preserveButton.onClick.Invoke();yield return new WaitForSecondsRealtime(2);
            bool stable=FindObjectsByType<Room3Motion>(FindObjectsSortMode.None).All(m=>!m.enabled)&&FindObjectsByType<Room3CorePulse>(FindObjectsSortMode.None).All(m=>!m.enabled);
            passed&=stable&&!f.ui.endingBody.isTextOverflowing&&!f.ui.endingCredits.isTextOverflowing;
            File.WriteAllText(reportPath,JsonUtility.ToJson(new Result{passed=passed,bridge=f.BridgeLocked,mouseOath=f.OathOpened,maxDownwardDegrees=maxOffset,frame=Time.frameCount,clampRestored=clampRestored,monumentStable=stable,bestCalibrationDot=bestDot,rightFrames=rightFrames,modalFrames=modalFrames,nearest=nearest},true));
            Debug.Log("ROOM3 mouse oath check: "+passed);Destroy(gameObject);
        }
        [Serializable]class Result{public bool passed,bridge,mouseOath,clampRestored,monumentStable;public float maxDownwardDegrees,bestCalibrationDot,nearest;public int frame,rightFrames,modalFrames;}
    }
}
#endif

