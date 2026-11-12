#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;
using Xiaoduan.Narrative;

namespace Xiaoduan.Room3
{
    /// <summary>Editor-only functional checks. Never included in a player build.</summary>
    public sealed class Room3ValidationDriver : MonoBehaviour
    {
        public string outputPath;
        readonly List<string> checks=new List<string>();
        [Serializable] class Result { public bool passed;public string error;public string[] checks;public int startFrame,endFrame;public float walkSeconds,minPlayerY; }
        Result report=new Result();
        void Assert(bool condition,string message){if(!condition)throw new Exception(message);checks.Add(message);}
        public void Begin(){DontDestroyOnLoad(gameObject);StartCoroutine(RunSafe());}
        IEnumerator RunSafe()
        {
            var routine=Run();
            while(true)
            {
                object current;
                try { if(!routine.MoveNext())break;current=routine.Current; }
                catch(Exception e){report.error=e.ToString();Finish(false);yield break;}
                yield return current;
            }
            Finish(true);
        }
        void Finish(bool success)
        {
            var p=GameObject.FindGameObjectWithTag("Player");if(p!=null){var input=p.GetComponent<StarterAssetsInputs>();if(input!=null){input.move=Vector2.zero;input.look=Vector2.zero;input.sprint=false;input.jump=false;}}
            report.passed=success;report.checks=checks.ToArray();report.endFrame=Time.frameCount;
            File.WriteAllText(outputPath,JsonUtility.ToJson(report,true));Debug.Log("ROOM3 functional verification: "+success);
            Destroy(gameObject);
        }
        void Place(Vector3 position,Quaternion rotation)
        {
            var player=GameObject.FindGameObjectWithTag("Player");var cc=player.GetComponent<CharacterController>();
            cc.enabled=false;player.transform.SetPositionAndRotation(position,rotation);cc.enabled=true;Physics.SyncTransforms();
        }
        IEnumerator WaitScene(string name)
        {
            float until=Time.realtimeSinceStartup+45;
            while((SceneManager.GetActiveScene().name!=name||SceneController.Instance.IsTransitioning)&&Time.realtimeSinceStartup<until)yield return null;
        }
        IEnumerator Run()
        {
            report.startFrame=Time.frameCount;yield return new WaitForSecondsRealtime(.6f);
            Assert(SceneManager.GetActiveScene().name=="ROOM3","Started in ROOM3");
            var player=GameObject.FindGameObjectWithTag("Player");var input=player.GetComponent<StarterAssetsInputs>();
            float start=Time.realtimeSinceStartup;report.minPlayerY=player.transform.position.y;
            while(player.transform.position.z<37&&Time.realtimeSinceStartup-start<32)
            {
                input.move=Vector2.up;input.look=Vector2.zero;input.jump=false;input.sprint=false;
                report.minPlayerY=Mathf.Min(report.minPlayerY,player.transform.position.y);yield return null;
            }
            input.move=Vector2.zero;report.walkSeconds=Time.realtimeSinceStartup-start;
            Assert(player.transform.position.z>=37,"Walked continuously from entry corridor to core without jumping");
            Assert(report.minPlayerY>-.5f,"No holes or falls along the main walking route");
            Place(new Vector3(-3.5f,.12f,38),Quaternion.identity);yield return new WaitForSecondsRealtime(.8f);
            var interactor=NarrativeInteractor.Active;
            Assert(interactor.CurrentTarget is NarrativeInspectable,"Archive terminal selected through normal facing and distance checks");
            interactor.TryInteract();Assert(interactor.IsReading,"E interaction opens the archive reader");
            yield return new WaitForSecondsRealtime(.3f);interactor.CloseEntry();Assert(!interactor.IsReading,"Closing the reader restores exploration");
            Assert(player.GetComponent<UnityEngine.InputSystem.PlayerInput>().inputIsActive,"Player input restored after reading");
            Place(new Vector3(0,-21,40),Quaternion.identity);yield return new WaitForSecondsRealtime(.5f);
            Assert(player.transform.position.y>-.5f&&player.transform.position.z>34,"Falling returns to the core checkpoint");
            Place(new Vector3(0,.12f,-24.8f),Quaternion.Euler(0,180,0));yield return new WaitForSecondsRealtime(.7f);
            var back=FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM2");
            Assert(back.CanInteract,"ROOM3 return portal detects the player collider");
            SceneController.GetOrCreate().TransitionToDestination(back);
            var wait=WaitScene("ROOM2");while(wait.MoveNext())yield return wait.Current;
            Assert(SceneManager.GetActiveScene().name=="ROOM2","ROOM3 to ROOM2 portal loaded the prison");
            player=GameObject.FindGameObjectWithTag("Player");
            Assert(Vector3.Distance(player.transform.position,new Vector3(-5.5f,37.05f,-123))<1,"Return arrival B is on the prison hall floor");
            Assert(GameObject.FindGameObjectsWithTag("Player").Length==1,"Only one player after the scene transition");
            var cameraData=Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            Assert(new UnityEditor.SerializedObject(cameraData).FindProperty("m_RendererIndex").intValue==-1,"Returning to ROOM2 restores its default scanner renderer");
            Assert(FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Only one active AudioListener after the scene transition");
            Place(new Vector3(-5.5f,37.05f,-118.2f),Quaternion.identity);yield return new WaitForSecondsRealtime(.7f);
            var enter=FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM3");
            Assert(enter.CanInteract,"New prison portal detects the player collider");
            SceneController.GetOrCreate().TransitionToDestination(enter);
            wait=WaitScene("ROOM3");while(wait.MoveNext())yield return wait.Current;
            Assert(SceneManager.GetActiveScene().name=="ROOM3","ROOM2 to ROOM3 portal loads the new scene");
            player=GameObject.FindGameObjectWithTag("Player");
            Assert(Vector3.Distance(player.transform.position,new Vector3(0,.15f,-21))<1,"ROOM3 arrival ENTER is at the corridor entrance");
            Assert(GameObject.FindGameObjectsWithTag("Player").Length==1,"Only one player on re-entering ROOM3");
            Place(new Vector3(0,.15f,48.8f),Quaternion.identity);yield return new WaitForSecondsRealtime(.7f);
            var town=FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM1");
            Assert(town.CanInteract,"Core exit portal detects the player collider");
            SceneController.GetOrCreate().TransitionToDestination(town);
            wait=WaitScene("ROOM1");while(wait.MoveNext())yield return wait.Current;
            Assert(SceneManager.GetActiveScene().name=="ROOM1","ROOM3 core exit returns to ROOM1");
            Assert(GameObject.FindGameObjectsWithTag("Player").Length==1,"Town return has one player");
            Assert(Time.frameCount>report.startFrame+100,"Runtime frames advanced throughout the verification");
        }
    }
}
#endif
