using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using StarterAssets;
using Xiaoduan.Narrative;

namespace Xiaoduan.Room3
{
    [DefaultExecutionOrder(200)]
    public sealed class Room3StoryFlow : MonoBehaviour
    {
        public Room3StoryUI ui;
        public Room3RecursiveArchive recursiveArchive;
        public Room3StoryNode[] nodes;
        public Transform calibrationPoint,calibrationTarget,oathPoint;
        public Transform[] bridgeModules;
        public Vector3[] lockedBridgePositions;
        public Collider[] bridgeColliders;
        public Collider bridgeBarrier,oathBarrier;
        public Transform[] gateLeaves;
        public GameObject[] workerFigures,boneLights;
        public GameObject returnPortal;
        public Transform monument;
        public Room3Environment environment;
        public Transform landingCheckpoint,postGateCheckpoint,coreCheckpoint;
        public Room3CameraInterference cameraInterference;
        public ParticleSystem memorialParticles;
        public Volume volume;
        public AudioSource clockSource,clickSource,geigerSource,heartbeatSource,effectsSource;
        public AudioClip bridgeSound,oathSound,revealSound;
        public bool WorkerView {get;private set;}
        public bool MedicalDecoded {get;private set;}
        public bool CourtDecoded {get;private set;}
        public bool BridgeLocked {get;private set;}
        public bool OathOpened {get;private set;}
        public bool EndingChosen {get;private set;}
        public bool Reading {get;private set;}
        public bool ReducedMotion {get;private set;}
        public int SelectedArchive {get;private set;}=-1;
        public float CalibrationProgress {get;private set;}
        public float OathProgress {get;private set;}
        public string Ending {get;private set;}="";
        public int DecodedCount=>(MedicalDecoded?1:0)+(CourtDecoded?1:0);
        Transform player;
        Camera eye;
        FirstPersonController movement;
        StarterAssetsInputs inputs;
        PlayerInput playerInput;
        NarrativeInteractor oldInteractor;
        bool modal,restorePlayerInput,restoreLook,restoreCursor,restoreInteractor;
        CursorLockMode cursorMode;
        bool cursorVisible;
        readonly Dictionary<TransitionPoint,bool> portalStates=new Dictionary<TransitionPoint,bool>();
        Vector3[] moduleOrigins;
        Quaternion[] moduleRotations;
        float bridgeAnimation,gateAnimation,shock,decodeProgress,nextFind,upwardUntil;
        ChromaticAberration chromatic;
        FilmGrain grain;
        Room3StoryNode target;
        bool loadedEnding;
        bool oathPitchLimited;
        float originalBottomClamp;
        string objective="";
        public string Objective=>objective;
        void Awake()
        {
            moduleOrigins=new Vector3[bridgeModules.Length];moduleRotations=new Quaternion[bridgeModules.Length];
            for(int i=0;i<bridgeModules.Length;i++){moduleOrigins[i]=bridgeModules[i].localPosition;moduleRotations[i]=bridgeModules[i].localRotation;}
            foreach(var collider in bridgeColliders)collider.enabled=false;
            foreach(var figure in workerFigures)figure.SetActive(false);
            foreach(var glow in boneLights)glow.SetActive(false);
            if(returnPortal!=null)returnPortal.SetActive(false);
            var profile=volume.profile;
            if(!profile.TryGet(out chromatic))chromatic=profile.Add<ChromaticAberration>(true);
            if(!profile.TryGet(out grain))grain=profile.Add<FilmGrain>(true);
            chromatic.intensity.Override(0);
            SceneManager.sceneLoaded+=SceneLoaded;
        }
        void Start()
        {
            MedicalDecoded=Xiaoduan.Survey.SurveyProgress.Has(2);CourtDecoded=Xiaoduan.Survey.SurveyProgress.Has(3);
            SetReducedMotion(Xiaoduan.Survey.SurveyProgress.ReducedMotion);
        }
        void FindPlayer()
        {
            nextFind=Time.unscaledTime+.5f;
            var go=GameObject.FindGameObjectWithTag("Player");
            if(go!=null){player=go.transform;movement=go.GetComponent<FirstPersonController>();inputs=go.GetComponent<StarterAssetsInputs>();playerInput=go.GetComponent<PlayerInput>();}
            eye=Camera.main;
            if(oldInteractor==null)oldInteractor=FindFirstObjectByType<NarrativeInteractor>();
        }
        void Update()
        {
            if(Time.unscaledTime>=nextFind||player==null||eye==null)FindPlayer();
            if(Xiaoduan.Survey.SurveyHUD.BlocksWorld)return;
            if(Keyboard.current!=null&&Keyboard.current.mKey.wasPressedThisFrame)SetReducedMotion(!ReducedMotion);
            if(EndingChosen)
            {
                if(loadedEnding&&Keyboard.current!=null&&Keyboard.current.rKey.wasPressedThisFrame)Restart();
                return;
            }
            if(player==null||eye==null||player.gameObject.scene.name!="ROOM3")return;
            if(BridgeLocked&&player.position.z>27){environment.checkpoint=landingCheckpoint;environment.reachedCore=true;}
            if(OathOpened&&player.position.z>44)environment.checkpoint=postGateCheckpoint;
            if(OathOpened&&player.position.z>89)environment.checkpoint=coreCheckpoint;
            if(!WorkerView&&player.position.x>4.2f&&player.position.z>-19&&player.position.z<1)EnterWorkerView();
            shock=Mathf.MoveTowards(shock,0,Time.unscaledDeltaTime*2);
            bool vision=Mouse.current!=null&&Mouse.current.rightButton.isPressed&&!Reading&&!ui.DecisionVisible;
            Shader.SetGlobalFloat("_Room3Reveal",vision?1:WorkerView? .42f:0);
            chromatic.intensity.value=ReducedMotion?0:shock*.35f;
            grain.intensity.value=.12f+shock*.3f;
            cameraInterference.reducedMotion=ReducedMotion;
            cameraInterference.strength=shock;
            cameraInterference.downwardDegrees=0;
            UpdateSound();
            if(Reading)
            {
                bool reveal=Keyboard.current!=null&&(Keyboard.current.fKey.isPressed||Keyboard.current.spaceKey.isPressed);
                if(reveal)AdvanceArchive(Time.unscaledDeltaTime);
                if(Keyboard.current!=null&&(Keyboard.current.escapeKey.wasPressedThisFrame||Keyboard.current.eKey.wasPressedThisFrame))CloseArchive();
                return;
            }
            if(ui.DecisionVisible)
            {
                if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){ui.HideDecision();SetModal(false);}
                return;
            }
            TickBridgeAnimation(Time.deltaTime);
            TickGateAnimation(Time.deltaTime);
            if(!BridgeLocked&&Vector3.Distance(player.position,calibrationPoint.position)<2.8f)
                TickCalibration(vision,eye.transform.forward,Time.deltaTime);
            else if(!BridgeLocked)CalibrationProgress=Mathf.MoveTowards(CalibrationProgress,0,Time.deltaTime*.6f);
            if(!OathOpened&&Vector3.Distance(player.position,oathPoint.position)<4.2f)
            {
                bool accessible=Keyboard.current!=null&&Keyboard.current.eKey.isPressed;
                bool held=Mouse.current!=null&&Mouse.current.leftButton.isPressed;
                bool upward=Mouse.current!=null&&Mouse.current.delta.ReadValue().y>1;
                if(upward)upwardUntil=Time.unscaledTime+.35f;
                TickOath(accessible||held,accessible||ReducedMotion||Time.unscaledTime<upwardUntil,Time.deltaTime);
                LimitOathPitch(held&&!accessible&&!ReducedMotion&&!OathOpened);
                if(held&&!accessible&&!ReducedMotion&&!OathOpened){cameraInterference.downwardDegrees=4*(1-OathProgress*.4f);cameraInterference.strength=.2f;}
            }
            else LimitOathPitch(false);
            target=FindTarget();
            if(target!=null&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)Interact(target);
            UpdateObjective();
        }
        void LateUpdate()
        {
            if(EndingChosen)return;
            if(ui==null||player==null)return;
            ui.SetStatus(objective,DecodedCount,ReducedMotion);
            if(oldInteractor!=null&&oldInteractor.view!=null)
            {
                oldInteractor.view.roomText.gameObject.SetActive(false);
                if(Reading||ui.DecisionVisible){oldInteractor.view.SetPrompt("","");oldInteractor.view.SetFocus(false);}
            }
            string action="",title="";
            float progress=0;
            if(!Reading&&!ui.DecisionVisible)
            {
                if(target!=null){action="[E]  "+(target.kind==Room3StoryNode.Kind.Decision?"Open core console":"Examine archive");title=target.label;}
                else if(!BridgeLocked&&Vector3.Distance(player.position,calibrationPoint.position)<2.8f)
                {action=MedicalDecoded?"HOLD RMB  /  ALIGN THE TWO RINGS":"READ THE ARCHIVE BEFORE CALIBRATING";title="Stand on the marker and look at the suspended target";progress=CalibrationProgress;}
                else if(!OathOpened&&Vector3.Distance(player.position,oathPoint.position)<4.2f)
                {action=CourtDecoded?"HOLD LMB + PUSH UP   /   OR HOLD E":"READ THE COURT RECORD";title="Prestare giuramento  /  symbolic testimony";progress=OathProgress;}
            }
            ui.SetPrompt(action,title,progress);
            bool calibration=!Reading&&!BridgeLocked&&MedicalDecoded&&Vector3.Distance(player.position,calibrationPoint.position)<2.8f;
            ui.SetCalibration(calibration,calibrationTarget.position,eye,CalibrationProgress);
        }
        void UpdateObjective()
        {
            if(recursiveArchive!=null&&recursiveArchive.Inside)objective=recursiveArchive.Objective;
            else if(!WorkerView)objective="Cross the breach in the observation glass.";
            else if(!MedicalDecoded)objective="Examine the disputed medical record. Hold F to reveal its other layer.";
            else if(recursiveArchive!=null&&!recursiveArchive.RecordDecoded)objective="Continue into the repeated archive threshold.";
            else if(!BridgeLocked)objective="Stand on the calibration mark. Align the suspended bridge.";
            else if(!CourtDecoded)objective="Cross the bridge and read the court archive.";
            else if(!OathOpened)objective="Approach the five figures. Hold E, or hold LMB and push upward.";
            else objective="Pass through the testimony gate. Decide what the archive should retain.";
        }
        void UpdateSound()
        {
            float mix=WorkerView?1:0;
            clockSource.volume=Mathf.MoveTowards(clockSource.volume,WorkerView? .045f:.09f,Time.deltaTime*.15f);
            clickSource.volume=Mathf.MoveTowards(clickSource.volume,mix*.11f,Time.deltaTime*.2f);
            geigerSource.volume=Mathf.MoveTowards(geigerSource.volume,mix*.055f,Time.deltaTime*.2f);
            heartbeatSource.volume=Mathf.MoveTowards(heartbeatSource.volume,OathOpened? .13f:0,Time.deltaTime*.18f);
        }
        public void SetReducedMotion(bool value){ReducedMotion=value;if(cameraInterference!=null)cameraInterference.reducedMotion=value;}
        void LimitOathPitch(bool limit)
        {
            if(movement==null)return;
            if(limit&&!oathPitchLimited){originalBottomClamp=movement.BottomClamp;movement.BottomClamp=Mathf.Max(originalBottomClamp,-55);oathPitchLimited=true;}
            if(!limit&&oathPitchLimited){movement.BottomClamp=originalBottomClamp;oathPitchLimited=false;}
        }
        public void EnterWorkerView()
        {
            if(WorkerView)return;WorkerView=true;shock=1;
            foreach(var figure in workerFigures)figure.SetActive(true);
            effectsSource.PlayOneShot(revealSound,.25f);ui.Notice("ACCESS BOUNDARY CROSSED",3);
        }
        Room3StoryNode FindTarget()
        {
            if(eye==null)return null;
            Room3StoryNode best=null;float score=float.PositiveInfinity;
            foreach(var node in nodes)
            {
                if(node==null||!node.isActiveAndEnabled)continue;
                if(node.kind==Room3StoryNode.Kind.CourtRecord&&!BridgeLocked)continue;
                if(node.kind==Room3StoryNode.Kind.LaborLedger&&(recursiveArchive==null||!recursiveArchive.CanRead))continue;
                if(node.kind==Room3StoryNode.Kind.Decision&&!OathOpened)continue;
                var delta=node.focus.position-eye.transform.position;
                if(delta.magnitude>3.5f||Vector3.Dot(eye.transform.forward,delta.normalized)<.88f)continue;
                if(Physics.Raycast(eye.transform.position,delta.normalized,out var hit,delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                    &&hit.transform!=node.transform&&!hit.transform.IsChildOf(node.transform))continue;
                if(delta.magnitude<score){score=delta.magnitude;best=node;}
            }
            return best;
        }
        public void Interact(Room3StoryNode node)
        {
            if(node==null||EndingChosen)return;
            if(node.kind==Room3StoryNode.Kind.Decision)
            {if(!OathOpened||!MedicalDecoded||!CourtDecoded)return;ui.ShowDecision();SetModal(true);return;}
            if(node.kind==Room3StoryNode.Kind.CourtRecord&&!BridgeLocked)return;
            OpenArchive(node.kind==Room3StoryNode.Kind.MedicalRecord?0:node.kind==Room3StoryNode.Kind.CourtRecord?1:2);
        }
        public void OpenArchive(int index)
        {
            if(index==1&&!BridgeLocked)return;
            if(index==2&&(recursiveArchive==null||!recursiveArchive.CanRead))return;
            SelectedArchive=index;Reading=true;decodeProgress=(index==0?MedicalDecoded:index==1?CourtDecoded:recursiveArchive.RecordDecoded)?1:0;
            ui.ShowArchive(index,decodeProgress);SetModal(true);
        }
        public void AdvanceArchive(float delta)
        {
            if(!Reading)return;
            bool wasDone=decodeProgress>=1;decodeProgress=Mathf.Min(1,decodeProgress+delta/3f);
            ui.Reveal(decodeProgress);
            if(!wasDone&&decodeProgress>=1)
            {
                if(SelectedArchive==2)recursiveArchive.RetainRecord();
                else {if(SelectedArchive==0)MedicalDecoded=true;else CourtDecoded=true;
                    Xiaoduan.Survey.SurveyProgress.Collect(SelectedArchive==0?2:3); }
                shock=.35f;effectsSource.PlayOneShot(revealSound,.22f);
            }
        }
        public void CloseArchive(){Reading=false;SelectedArchive=-1;ui.HideArchive();SetModal(false);UpdateObjective();}
        public void TickCalibration(bool held,Vector3 forward,float delta)
        {
            if(BridgeLocked||!MedicalDecoded||player==null||(recursiveArchive!=null&&!recursiveArchive.RecordDecoded))return;
            bool near=Vector3.Distance(player.position,calibrationPoint.position)<2.8f;
            float alignment=Vector3.Dot(forward.normalized,(calibrationTarget.position-eye.transform.position).normalized);
            if(held&&near&&alignment>.995f)CalibrationProgress=Mathf.Min(1,CalibrationProgress+delta/1.15f);
            else CalibrationProgress=Mathf.MoveTowards(CalibrationProgress,0,delta*.65f);
            if(CalibrationProgress>=1)
            {BridgeLocked=true;effectsSource.PlayOneShot(bridgeSound,.45f);ui.Notice("ALIGNMENT ACCEPTED / BRIDGE LOCKING",3);}
        }
        void TickBridgeAnimation(float delta)
        {
            if(!BridgeLocked||bridgeAnimation>=1)return;
            bridgeAnimation=Mathf.Min(1,bridgeAnimation+delta/2.5f);float t=Mathf.SmoothStep(0,1,bridgeAnimation);
            for(int i=0;i<bridgeModules.Length;i++)
            {bridgeModules[i].localPosition=Vector3.Lerp(moduleOrigins[i],lockedBridgePositions[i],t);bridgeModules[i].localRotation=Quaternion.Slerp(moduleRotations[i],Quaternion.identity,t);}
            if(bridgeAnimation>=1){foreach(var c in bridgeColliders)c.enabled=true;bridgeBarrier.enabled=false;}
        }
        public void TickOath(bool held,bool upward,float delta)
        {
            if(OathOpened||!CourtDecoded||!MedicalDecoded||!BridgeLocked||player==null)return;
            if(Vector3.Distance(player.position,oathPoint.position)>4.2f)return;
            if(held&&upward)OathProgress=Mathf.Min(1,OathProgress+delta/4);
            else OathProgress=Mathf.MoveTowards(OathProgress,0,delta*.04f);
            if(held)ui.Notice(OathProgress<.5f?"HEARING ADJOURNED / ACCESS WITHHELD":"TESTIMONY RETAINED / REQUEST PENDING",.15f);
            if(OathProgress>=1)
            {
                OathOpened=true;foreach(var g in boneLights)g.SetActive(true);
                effectsSource.PlayOneShot(oathSound,.5f);shock=ReducedMotion?0:.45f;ui.Notice("TESTIMONY RECEIVED",3);
            }
        }
        void TickGateAnimation(float delta)
        {
            if(!OathOpened||gateAnimation>=1)return;
            gateAnimation=Mathf.Min(1,gateAnimation+delta/1.8f);float t=Mathf.SmoothStep(0,1,gateAnimation);
            gateLeaves[0].localPosition=new Vector3(-1.3f-3*t,3,42);gateLeaves[1].localPosition=new Vector3(1.3f+3*t,3,42);
            if(gateAnimation>=1)oathBarrier.enabled=false;
        }
        void SetModal(bool value)
        {
            if(modal==value)return;
            modal=value;
            if(value)
            {
                Xiaoduan.Survey.SurveyHUD.Instance?.SuppressWorldOverlay();
                restorePlayerInput=playerInput!=null&&playerInput.inputIsActive;
                restoreLook=inputs!=null&&inputs.cursorInputForLook;restoreCursor=inputs!=null&&inputs.cursorLocked;
                cursorMode=Cursor.lockState;cursorVisible=Cursor.visible;
                restoreInteractor=oldInteractor!=null&&oldInteractor.enabled;
                if(oldInteractor!=null)oldInteractor.enabled=false;
                if(playerInput!=null)playerInput.DeactivateInput();
                if(inputs!=null){inputs.move=Vector2.zero;inputs.look=Vector2.zero;inputs.jump=false;inputs.sprint=false;inputs.cursorInputForLook=false;inputs.cursorLocked=false;}
                foreach(var portal in FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None)){portalStates[portal]=portal.enabled;portal.enabled=false;}
                Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            }
            else
            {
                if(inputs!=null){inputs.move=Vector2.zero;inputs.look=Vector2.zero;inputs.jump=false;inputs.cursorInputForLook=restoreLook;inputs.cursorLocked=restoreCursor;}
                if(playerInput!=null&&restorePlayerInput)playerInput.ActivateInput();
                if(oldInteractor!=null&&restoreInteractor)oldInteractor.enabled=true;
                foreach(var p in portalStates)if(p.Key!=null)p.Key.enabled=p.Value;portalStates.Clear();
                Cursor.lockState=cursorMode;Cursor.visible=cursorVisible;
            }
        }
        public void ChooseEnding(bool preserve)
        {
            if(EndingChosen||!OathOpened||!CourtDecoded||!MedicalDecoded)return;
            EndingChosen=true;Ending=preserve?"preserve":"format";SetModal(true);ui.HideDecision();
            cameraInterference.strength=0;cameraInterference.downwardDegrees=0;chromatic.intensity.value=0;
            if(preserve)StartCoroutine(Preserve());else StartCoroutine(Format());
        }
        IEnumerator Format()
        {
            clockSource.Stop();clickSource.Stop();geigerSource.Stop();heartbeatSource.Stop();effectsSource.Stop();
            ui.SetFade(0,Color.white);
            for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){ui.SetFade(t/.7f,Color.white);yield return null;}
            ui.SetFade(1,Color.white);SetModal(false);
            transform.SetParent(null);DontDestroyOnLoad(gameObject);
            var point=gameObject.AddComponent<TransitionPoint>();point.sceneName="ROOM1";point.transitionType=TransitionPoint.TransitionType.DifferentScene;point.destinationTag=TransitionDestination.DestinationTag.ENTER;point.enabled=false;
            SceneController.GetOrCreate().TransitionToDestination(point);
            while(SceneManager.GetActiveScene().name!="ROOM1"||SceneController.Instance.IsTransitioning)yield return null;
            FindPlayer();SetModal(true);
            ui.ShowEnding(false);ui.SetFade(0,Color.white);loadedEnding=true;
        }
        IEnumerator Preserve()
        {
            foreach(var motion in FindObjectsByType<Room3Motion>(FindObjectsSortMode.None))if(motion.gameObject.scene.name=="ROOM3")motion.enabled=false;
            foreach(var pulse in FindObjectsByType<Room3CorePulse>(FindObjectsSortMode.None))pulse.enabled=false;
            if(memorialParticles!=null)memorialParticles.Play();
            clockSource.Stop();clickSource.Stop();geigerSource.Stop();heartbeatSource.volume=.12f;
            for(float t=0;t<1.8f;t+=Time.unscaledDeltaTime){ui.SetFade(t/1.8f*.7f,Color.black);yield return null;}
            ui.ShowEnding(true);loadedEnding=true;
        }
        void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(!EndingChosen||Ending!="format"||scene.name!="ROOM1")return;
            foreach(var point in FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None))
            {
                if(point.gameObject.scene!=scene||point.sceneName!="ROOM2")continue;
                Transform portal=point.transform;
                for(Transform t=point.transform;t!=null;t=t.parent)if(t.name=="Potal"||t.name=="Portal"){portal=t;break;}
                point.enabled=false;
                foreach(var r in portal.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                foreach(var c in portal.GetComponentsInChildren<Collider>(true))c.enabled=false;
                foreach(var light in portal.GetComponentsInChildren<Light>(true))light.enabled=false;
                foreach(var particles in portal.GetComponentsInChildren<ParticleSystem>(true))particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        public void Restart()
        {
            if(!loadedEnding)return;
            Xiaoduan.Survey.SurveyProgress.NewGame();
            SetModal(false);SceneManager.sceneLoaded-=SceneLoaded;SceneManager.LoadScene("ROOM1",LoadSceneMode.Single);Destroy(gameObject);
        }
        void OnDisable()
        {
            LimitOathPitch(false);
            SetModal(false);Shader.SetGlobalFloat("_Room3Reveal",0);
            if(cameraInterference!=null){cameraInterference.strength=0;cameraInterference.downwardDegrees=0;}
        }
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;}
    }
}


