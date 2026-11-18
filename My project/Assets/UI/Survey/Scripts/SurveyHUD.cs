using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using StarterAssets;
using TMPro;
using Xiaoduan.Narrative;
using Xiaoduan.Room3;
using Xiaoduan.Clinical;
using Xiaoduan.Panopticon;

namespace Xiaoduan.Survey
{
    [DefaultExecutionOrder(300)]
    public sealed class SurveyHUD : MonoBehaviour
    {
        public static SurveyHUD Instance {get;private set;}
        public static bool BlocksWorld => SurveyProgress.Modal;
        public RectTransform safeArea,hudRoot,needle,fracture,menuSlit,brushCursor,focusMarker;
        public CanvasGroup hud,decoder,visionFrame;
        public TMP_Text roomLabel,roleLabel,objective,integrity,cpm,traceCount,decoderLabel,decoderHint,bannerText;
        public TMP_Text[] fragments;
        public UnityEngine.UI.Image integrityFill,decoderFill,oathFill,flash;
        public SurveyWave wave;
        public GameObject banner,oath,menu,archive;
        public TMP_Text archiveTitle,officialText,truthText,sourceText,revealLabel,archiveHint;
        public CanvasGroup officialLayer,truthLayer;
        public RawImage archivePicture;
        public SurveyScratch scratch;
        public UnityEngine.UI.Button startButton,closeButton,reducedButton,menuMotionButton;
        public TMP_Text reducedText;
        public Texture workshop,clinical,mollie;
        public AudioSource effects,detector,previewAudio;
        public AudioClip tick,click,reveal,boot,chime,musicBox,focusSound;
        public Material artwork;
        public Sprite buttonSprite,frameSprite;
        public bool Vision {get;private set;}
        public bool IsReading=>archive!=null&&archive.activeSelf;
        public bool IsAdvertisementReading=>IsReading&&advertisementSource!=null;
        public bool IsClinicalReading=>IsReading&&clinicalDesk!=null;
        ClinicalDesk clinicalDesk;
        ClinicalDeskReader clinicalReader;
        public bool MenuVisible=>menu!=null&&menu.activeSelf;
        public float DecodeProgress=>revealProgress;
        public float TunerProgress=>tuning;
        public int Preview {get;private set;}=-1;
        SurveyNode[] nodes=new SurveyNode[0];
        SurveyNode soundTarget;
        public LaborEchoEmitter FocusedSoundEmitter=>soundTarget!=null?soundTarget.GetComponent<LaborEchoEmitter>():null;
        public bool SoundFocusRetained=>soundTarget!=null&&soundTarget.Complete;
        public bool SoundFocusActive=>scene=="ROOM2"&&soundTarget!=null&&eye!=null&&!IsReading&&
            Vector3.Dot(eye.transform.forward,(soundTarget.Position-eye.transform.position).normalized)>.85f;
        Room3StoryFlow story;
        NarrativeHUD oldHUD;
        Camera eye;
        Transform player;
        PlayerInput lockedPlayer;
        StarterAssetsInputs lockedInputs;
        bool inputActive,lookActive,cursorActive,cursorVisible,ownsLock;
        CursorLockMode cursorMode;
        string scene;
        float refreshAt,tuning,revealProgress,nextTick,bannerUntil,shock,closedUntil;
        int readingIndex;
        TownAdvertisement advertisementSource;
        Color advertisementTextColor;
        Vector2 lastScreen;
        Rect lastSafe;
        bool scanWasActive;
        bool menuConsidered;
        bool focusing;
        int identityMask=-1;
        float nextRadar;
        bool workerObserved;
        float workerOverflowUntil;
        string guidance="";
        public string Guidance=>guidance;
        public bool GateHintVisible {get;private set;}
        int gateHintMask=-1;
        string gateHint="";
        string bannerMessage="";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetInstance(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Instance!=null)return;
            var prefab=Resources.Load<GameObject>("SurveyInterface");
            if(prefab!=null)Instantiate(prefab);
        }
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}
            Instance=this;DontDestroyOnLoad(gameObject);
            startButton.onClick.AddListener(BeginInspection);
            closeButton.onClick.AddListener(CloseArchive);
            reducedButton.onClick.AddListener(()=>{SurveyProgress.ReducedMotion=!SurveyProgress.ReducedMotion;ApplyReducedMotion();});
            menuMotionButton.onClick.AddListener(()=>{SurveyProgress.ReducedMotion=!SurveyProgress.ReducedMotion;ApplyReducedMotion();});
            scratch.Completed=CompleteArchive;
            menu.SetActive(false);archive.SetActive(false);banner.SetActive(false);oath.SetActive(false);
            SceneManager.sceneLoaded+=SceneLoaded;SpatialScanEvent.CameraCrossed+=ScanCrossed;SurveyProgress.Changed+=ProgressChanged;
            RefreshScene();
        }
        void OnDestroy()
        {
            if(Instance!=this)return;
            Unlock();SceneManager.sceneLoaded-=SceneLoaded;SpatialScanEvent.CameraCrossed-=ScanCrossed;SurveyProgress.Changed-=ProgressChanged;Instance=null;
        }
        void SceneLoaded(Scene _,LoadSceneMode __){StartCoroutine(AfterSceneLoaded());}
        IEnumerator AfterSceneLoaded(){yield return null;RefreshScene();}
        void RefreshScene()
        {
            scene=SceneManager.GetActiveScene().name;refreshAt=0;nodes=FindObjectsByType<SurveyNode>(FindObjectsSortMode.None);
            oldHUD=FindFirstObjectByType<NarrativeHUD>();story=FindFirstObjectByType<Room3StoryFlow>();
            if(oldHUD!=null){oldHUD.roomText.gameObject.SetActive(false);if(oldHUD.introGroup!=null)oldHUD.introGroup.gameObject.SetActive(false);}
            if(story!=null)
            {
                story.ui.objectiveText.transform.parent.gameObject.SetActive(false);story.ui.settingsText.gameObject.SetActive(false);
                StyleStory(story.ui);
            }
            ApplyReducedMotion();
            tuning=0;soundTarget=null;scanWasActive=false;identityMask=-1;nextRadar=0;workerObserved=false;workerOverflowUntil=0;
            roomLabel.text=scene=="ROOM1"?"01 / THE TOWN":scene=="ROOM2"?"02 / THE PRISON":"03 / THE BLACK BOX ARCHIVE";
            if(scene!="ROOM1")SurveyProgress.Started=true;
            menuConsidered=false;
        }
        void FindPlayer()
        {
            refreshAt=Time.unscaledTime+.5f;eye=Camera.main;
            var go=GameObject.FindGameObjectWithTag("Player");player=go!=null?go.transform:null;
        }
        void Update()
        {
            if(Time.unscaledTime>=refreshAt||eye==null||player==null)FindPlayer();
            if(!menuConsidered&&player!=null)
            {
                menuConsidered=true;
                if(scene=="ROOM1"&&!SurveyProgress.Started){menu.SetActive(true);Lock();}
            }
            if(lastScreen!=new Vector2(Screen.width,Screen.height)||lastSafe!=Screen.safeArea)LayoutSafeArea();
            bool keyMotion=Keyboard.current!=null&&Keyboard.current.mKey.wasPressedThisFrame;
            if(keyMotion&&(story==null||BlocksWorld)){SurveyProgress.ReducedMotion=!SurveyProgress.ReducedMotion;ApplyReducedMotion();}
            if(story!=null)SurveyProgress.ReducedMotion=story.ReducedMotion;
            reducedText.text="[M]  REDUCED MOTION  "+(SurveyProgress.ReducedMotion?"ON":"OFF");
            menuMotionButton.GetComponentInChildren<TMP_Text>().text="REDUCED MOTION  "+(SurveyProgress.ReducedMotion?"ON":"OFF")+"  [M]";
            Vision=!TownBlackout.HidesSurvey&&!BlocksWorld&&!(story!=null&&(story.Reading||story.ui.DecisionVisible||story.EndingChosen||(story.embodied!=null&&story.embodied.Gripping)))&&Mouse.current!=null&&Mouse.current.rightButton.isPressed;
            visionFrame.alpha=Mathf.MoveTowards(visionFrame.alpha,scene!="ROOM3"&&Vision?1:0,Time.unscaledDeltaTime*7);
            if(MenuVisible){float pulse=SurveyProgress.ReducedMotion?1:.85f+.15f*Mathf.Sin(Time.unscaledTime*1.4f);menuSlit.localScale=new Vector3(pulse,1,1);}
            if(IsReading)
            {
                bool held=Keyboard.current!=null&&(Keyboard.current.fKey.isPressed||Keyboard.current.spaceKey.isPressed);
                if(held&&!focusing&&readingIndex==0&&revealProgress<1&&focusSound!=null)effects.PlayOneShot(focusSound,.2f);
                focusing=held;
                if(held)AdvanceArchive(Time.unscaledDeltaTime);
                if(Time.unscaledTime>closedUntil&&Keyboard.current!=null&&(Keyboard.current.escapeKey.wasPressedThisFrame||Keyboard.current.eKey.wasPressedThisFrame))CloseArchive();
                if(readingIndex==2&&revealProgress<1)revealLabel.text="EXPOSED  "+Mathf.RoundToInt(scratch.Coverage*100)+"%   /   STROKES  "+scratch.Strokes+" / 3";
            }
            bool brushing=IsReading&&!IsClinicalReading&&readingIndex==2&&revealProgress<1;
            if(brushCursor!=null)
            {
                brushCursor.gameObject.SetActive(brushing);
                if(brushing&&Mouse.current!=null&&RectTransformUtility.ScreenPointToLocalPointInRectangle(safeArea,Mouse.current.position.ReadValue(),null,out var p))brushCursor.anchoredPosition=p;
            }
            if(IsReading)Cursor.visible=!brushing&&!IsClinicalReading;
            if(focusMarker!=null)
            {
                focusMarker.gameObject.SetActive(IsReading&&readingIndex==0&&revealProgress<1);
                float jitter=SurveyProgress.ReducedMotion?0:(1-revealProgress)*5;
                focusMarker.anchoredPosition=new Vector2(-176,-192)+new Vector2(Mathf.Sin(Time.unscaledTime*27),Mathf.Cos(Time.unscaledTime*31))*jitter;
            }
            bool storyModal=story!=null&&(story.Reading||story.ui.DecisionVisible||story.EndingChosen||(story.embodied!=null&&story.embodied.Gripping));
            hud.alpha=MenuVisible||IsReading||storyModal?0:1;
            hud.blocksRaycasts=hud.alpha>.5f;hud.interactable=hud.alpha>.5f;
            if(oldHUD!=null&&BlocksWorld){oldHUD.SetPrompt("","");oldHUD.SetFocus(false);oldHUD.dot.enabled=false;}
            UpdateIdentity();
            if(!BlocksWorld&&!storyModal){UpdateRadar();UpdateDecoder();UpdateObjectives();}
            else {decoder.alpha=0;detector.Stop();}
            if(story!=null)
            {
                bool show=!storyModal&&story.BridgeLocked&&!story.OathOpened&&player!=null&&Vector3.Distance(player.position,story.oathPoint.position)<4.2f;
                oath.SetActive(show);oathFill.fillAmount=story.OathProgress;
                if(story.WorkerView&&!workerObserved){workerObserved=true;workerOverflowUntil=Time.time+1.8f;}
                if(!storyModal&&Time.time<workerOverflowUntil)cpm.text="9999\n<color=#70F7A2>OVERFLOW</color>";
            }
            UpdateScanner();
            shock=Mathf.MoveTowards(shock,0,Time.unscaledDeltaTime*2);
            float a=SurveyProgress.ReducedMotion?0:shock*.16f;flash.color=new Color(.32f,1,.55f,a);
            hudRoot.anchoredPosition=SurveyProgress.ReducedMotion?Vector2.zero:new Vector2(Mathf.Sin(Time.unscaledTime*85),Mathf.Cos(Time.unscaledTime*69))*shock*3;
            banner.SetActive(!MenuVisible&&!IsReading&&!storyModal&&Time.unscaledTime<bannerUntil);
            bannerText.text=bannerMessage;
        }
        void LayoutSafeArea()
        {
            lastScreen=new Vector2(Screen.width,Screen.height);lastSafe=Screen.safeArea;
            if(Screen.width<1||Screen.height<1)return;
            safeArea.anchorMin=lastSafe.min/lastScreen;safeArea.anchorMax=lastSafe.max/lastScreen;safeArea.offsetMin=safeArea.offsetMax=Vector2.zero;
        }
        void UpdateIdentity()
        {
            if(identityMask==SurveyProgress.Mask)return;identityMask=SurveyProgress.Mask;
            int n=SurveyProgress.Count;int health=100-n*20;
            integrity.text="INTEGRITY  "+health+"%";integrityFill.fillAmount=health/100f;
            roleLabel.text=n==0?"ROLE: SYSTEM SURVEYOR":n<3?"ROLE: SURVEYOR / ACCESS ALTERED":"ROLE: <s>SURVEYOR</s> > INFECTED WITNESS";
            if(scene=="ROOM1")roleLabel.text=n==0?"ROLE: SYSTEM SURVEYOR":n<3?"ROLE: SURVEYOR / ACCESS ALTERED":"ROLE: <s>SURVEYOR</s> > INFECTED WITNESS";
            roleLabel.color=n==0?new Color(.85f,.9f,.88f):new Color(.57f,.82f,.68f);
            fracture.gameObject.SetActive(n>0);fracture.localScale=new Vector3(.5f+n*.13f,.5f+n*.13f,1);
            fracture.GetComponent<UnityEngine.UI.Image>().color=new Color(.13f,.50f,.29f,.08f+n*.045f);
            string[] numerals={"XII","III","VI","IX"};
            for(int i=0;i<4;i++){fragments[i].text=numerals[i];fragments[i].color=SurveyProgress.Has(i)?new Color(.4f,1,.62f):new Color(.38f,.44f,.42f);}
            traceCount.text="TRACES  "+n+" / 4";
        }
        void UpdateObjectives()
        {
            if(scene=="ROOM1"){var state=TownBlackout.Active;var town=TownPrologue.Active;objective.text=town!=null&&town.PortalVisible?"Follow the glowing footprints.\n[E] Process the anomaly and enter.":SurveyProgress.Has(0)?"Trace retained. The anomaly is materializing.":state!=null&&state.Triggered?"Signal failure. Observe the exposed residue.":"Hold RMB to trace the footprints.\n[E] Inspect the radium advertisements.";}
            else if(scene=="ROOM2"&&PanopticonHall.Active!=null&&PanopticonHall.Active.Entered)objective.text=PanopticonHall.Active.Objective;
            else if(scene=="ROOM2")objective.text=ClinicalDesk.Active!=null&&!ClinicalDesk.Active.Retained?"Find the candlelit medical record. [E] inspect; hold LMB and scrape the ink.":"Continue into the archive hall.\nOptional: investigate the sounds in the side cells.";
            else if(story!=null)objective.text=story.Objective;
        }
        public static bool Eligible(SurveyNode node,string room)
        {
            if(node==null||!node.isActiveAndEnabled)return false;
            if(room=="ROOM2"&&ClinicalDesk.Active!=null&&!ClinicalDesk.Active.Retained)return node.GetComponent<ClinicalDeskTarget>()!=null;
            if(node.Complete)return false;
            if(room=="ROOM1")return node.kind==SurveyNodeKind.Advertisement&&!node.Complete&&(TownBlackout.Active==null||!TownBlackout.Active.Triggered)||node.kind==SurveyNodeKind.Portal&&TownPrologue.Active!=null&&TownPrologue.Active.CanEnter;
            if(room!="ROOM2")return true;
            if(PanopticonHall.Active!=null&&PanopticonHall.Active.Entered)return node.kind==SurveyNodeKind.Portal&&PanopticonHall.Active.GateReleased;
            if(!SurveyProgress.Has(0))return node.kind==SurveyNodeKind.Bottle;
            if(!SurveyProgress.Has(1))return node.kind==SurveyNodeKind.Watches||node.kind==SurveyNodeKind.Stamping;
            if(!SurveyProgress.Has(2))return node.kind==SurveyNodeKind.Medical;
            return node.kind==SurveyNodeKind.Portal;
        }
        void UpdateRadar()
        {
            if(eye==null||Time.unscaledTime<nextRadar)return;nextRadar=Time.unscaledTime+.1f;
            SurveyNode nearest=null;float best=float.PositiveInfinity;Vector3 position=Vector3.zero;bool found=false;
            if(story!=null&&!story.EndingChosen)
            {
                found=true;
                if(story.recursiveArchive!=null&&story.recursiveArchive.Inside){position=story.recursiveArchive.RecordDecoded?story.recursiveArchive.repetition.transform.position:story.recursiveArchive.GuidePosition;guidance=story.recursiveArchive.RecordDecoded?"RELEASED THRESHOLD":story.recursiveArchive.Guidance;}
                else if(!story.WorkerView){position=new Vector3(5.5f,1.5f,-15.5f);guidance="OBSERVATION BREACH";}
                else if(!story.MedicalDecoded){position=story.nodes.First(n=>n.kind==Room3StoryNode.Kind.MedicalRecord).focus.position;guidance="MEDICAL TRACE";}
                
                else if(story.recursiveArchive!=null&&!story.recursiveArchive.RecordDecoded){position=story.recursiveArchive.portals[0].transform.position;guidance="REPEATED ARCHIVE";}
                else if(!story.BridgeLocked){position=story.calibrationPoint.position+Vector3.up;guidance="BRIDGE CALIBRATION";}
                else if(!story.CourtDecoded){position=story.nodes.First(n=>n.kind==Room3StoryNode.Kind.CourtRecord).focus.position;guidance="COURT ARCHIVE";}
                else if(!story.OathOpened){position=story.oathPoint.position+Vector3.up;guidance="TESTIMONY GATE";}
                else {position=story.nodes.First(n=>n.kind==Room3StoryNode.Kind.Decision).focus.position;guidance="CORE CONSOLE";}
            }
            else if(SoundFocusActive){found=true;position=soundTarget.Position;guidance="LABOR SIGNAL";}
            else if(scene=="ROOM2"&&PanopticonHall.Active!=null&&PanopticonHall.Active.Entered&&!PanopticonHall.Active.GateReleased)
            {found=true;position=PanopticonHall.Active.GuidePosition;guidance="ISOTOPE MEMORY / SCAN WINDOW";}
            else
            {
                foreach(var node in nodes)
                {
                    if(!Eligible(node,scene))continue;
                    float distance=Vector3.Distance(eye.transform.position,node.Position);
                    if(distance<best){best=distance;nearest=node;}
                }
                found=nearest!=null;
                if(found){position=nearest.Position;guidance=nearest.kind.ToString().ToUpperInvariant();}
            }
            if(!found){guidance="";cpm.text="0015\nSIMULATED CPM";return;}
            Vector3 delta=position-eye.transform.position;best=delta.magnitude;
            float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(eye.transform.forward,Vector3.up),Vector3.ProjectOnPlane(delta,Vector3.up),Vector3.up);
            float alignment=Mathf.Pow(Mathf.Clamp01(Vector3.Dot(eye.transform.forward,delta.normalized)),6);
            float heat=Mathf.Clamp01(1-best/38)*(.08f+.92f*alignment);
            float jitter=SurveyProgress.ReducedMotion?0:Mathf.Sin(Time.unscaledTime*47)*heat*6;
            needle.localRotation=Quaternion.Euler(0,0,-angle+jitter);
            cpm.text=Mathf.RoundToInt(15+heat*875+Mathf.Sin(Time.unscaledTime*17)*heat*12).ToString("0000")+"\nSIMULATED CPM";
            if(heat>.08f&&(scene!="ROOM1"||Vision)&&Time.unscaledTime>=nextTick){detector.PlayOneShot(tick,Mathf.Lerp(.015f,.10f,heat));nextTick=Time.unscaledTime+Mathf.Lerp(1.8f,.13f,heat);}
        }
        void UpdateDecoder()
        {
            if(eye==null||scene=="ROOM3"){decoder.alpha=0;return;}
            SurveyNode candidate=null;float nearest=100;
            foreach(var node in nodes)
            {
                if(node==null||!node.isActiveAndEnabled||(node.kind!=SurveyNodeKind.Watches&&node.kind!=SurveyNodeKind.Stamping))continue;
                float distance=Vector3.Distance(eye.transform.position,node.Position);
                if(distance<node.reach&&distance<nearest){candidate=node;nearest=distance;}
            }
            if(candidate!=soundTarget){soundTarget=candidate;tuning=0;}
            GateHintVisible=false;
            if(candidate==null){if(!ShowGateRequirement())decoder.alpha=Mathf.MoveTowards(decoder.alpha,0,Time.unscaledDeltaTime*5);return;}
            decoder.alpha=Mathf.MoveTowards(decoder.alpha,1,Time.unscaledDeltaTime*5);
            float dot=Vector3.Dot(eye.transform.forward,(candidate.Position-eye.transform.position).normalized);
            bool aligned=dot>.94f;
            var emitter=candidate.GetComponent<LaborEchoEmitter>();
            bool audible=emitter==null||emitter.SignalStrength>.065f;
            wave.energy=Vision?Mathf.Lerp(.3f,1,Mathf.Clamp01(dot)): .18f;
            if(!candidate.Complete)
            {
                if(Vision&&aligned&&audible)tuning=Mathf.Clamp01(tuning+Time.unscaledDeltaTime/2);
                else tuning=Mathf.MoveTowards(tuning,0,Time.unscaledDeltaTime*.5f);
                decoderLabel.text="FREQUENCY LOCK  "+Mathf.RoundToInt(tuning*100)+"%";
                decoderHint.text=!audible?"MOVE CLOSER / SIGNAL TOO FAINT":aligned?"HOLD RMB / RETAIN THE SIGNAL":"Aim into the cell. Hold RMB for 2 seconds.";
                if(tuning>=1){SurveyProgress.Tune(candidate.kind==SurveyNodeKind.Watches);ButtonFeedback(true);Notify("LABOR TRACE RETAINED / "+(candidate.kind==SurveyNodeKind.Watches?"MEASURED TIME":"REPEATED IMPACT"),4);}
            }
            if(candidate.Complete)
            {
                decoderLabel.text=candidate.kind==SurveyNodeKind.Watches?"DIAL WORK / CONTEMPORARY SCREEN WORK":"REPEATED IMPACT / THE BODY AS MACHINE";
                decoderHint.text=candidate.kind==SurveyNodeKind.Watches?"Timed gestures, across different workplaces and different harms.":"A reconstructed industrial rhythm. The room remains empty.";
                tuning=1;
            }
            decoderFill.fillAmount=tuning;
        }
        bool ShowGateRequirement()
        {
            if(scene=="ROOM2"&&PanopticonHall.Active!=null)return false;
            if(scene!="ROOM2"||(SurveyProgress.Has(0)&&SurveyProgress.Has(1)&&SurveyProgress.Has(2)))return false;
            foreach(var node in nodes)
            {
                if(node==null||node.kind!=SurveyNodeKind.Portal)continue;
                var delta=node.Position+Vector3.up*1.5f-eye.transform.position;
                if(delta.magnitude>7||Vector3.Dot(eye.transform.forward,delta.normalized)<.5f)continue;
                GateHintVisible=true;decoder.alpha=Mathf.MoveTowards(decoder.alpha,1,Time.unscaledDeltaTime*5);wave.energy=.05f;
                decoderLabel.text="ARCHIVE GATE LOCKED";
                if(gateHintMask!=SurveyProgress.Mask)
                {
                    gateHintMask=SurveyProgress.Mask;
                    gateHint="Missing: "+string.Join(" / ",new[]{0,1,2}.Where(i=>!SurveyProgress.Has(i)).Select(i=>new[]{"XII","III","VI"}[i]))+". Follow the dial to the next trace.";
                }
                decoderHint.text=gateHint;decoderFill.fillAmount=((SurveyProgress.Has(0)?1:0)+(SurveyProgress.Has(1)?1:0)+(SurveyProgress.Has(2)?1:0))/3f;
                return true;
            }
            return false;
        }
        void UpdateScanner()
        {
            bool scanning=SpatialScanEvent.Active!=null&&SpatialScanEvent.Active.IsScanning;
            if(scanning&&!scanWasActive&&PanopticonHall.Active==null)Notify("PANOPTICON SCAN",3);
            scanWasActive=scanning;
        }
        void ScanCrossed(SpatialScanEvent _)
        {
            if(PanopticonHall.Active!=null&&PanopticonHall.Active.Entered)return;
            shock=1;
            Notify(SurveyProgress.Count>0?"UNAUTHORIZED ARCHIVE ACCESS / PERMISSIONS UNDER REVIEW":"SURVEYOR IDENTIFIED / SCAN COMPLETE",3);
        }
        public void Notify(string text,float seconds=3){bannerMessage=text;bannerUntil=Time.unscaledTime+seconds;}
        void ProgressChanged(){effects.PlayOneShot(chime,.2f);shock=.35f;}
        public static bool TryOpenInspectable(NarrativeInspectable item)
        {
            if(Instance!=null&&item.TryGetComponent<ClinicalDeskTarget>(out var target))
            {Instance.OpenClinicalArchive(target.desk);return true;}
            if(Instance!=null&&item.TryGetComponent<TownAdvertisement>(out var advertisement))
            {if(!advertisement.Dissolving&&!advertisement.Dissolved)Instance.OpenAdvertisement(advertisement);return true;}
            var node=item.GetComponent<SurveyNode>();
            if(node==null||Instance==null)return false;
            if(node.kind!=SurveyNodeKind.Bottle&&node.kind!=SurveyNodeKind.Medical&&node.kind!=SurveyNodeKind.TownVanity)return false;
            Instance.OpenArchive(node.Fragment);return true;
        }
        public void OpenClinicalArchive(ClinicalDesk source)
        {
            if(source==null||IsReading||MenuVisible||BlocksWorld)return;
            OpenArchive(0);if(!IsReading)return;clinicalDesk=source;
            revealProgress=source.Retained?1:0;
            if(clinicalReader==null)clinicalReader=gameObject.AddComponent<ClinicalDeskReader>();
            clinicalReader.Show(this,source);
        }
        public void OpenArchive(int index)
        {
            if(IsReading||MenuVisible||BlocksWorld)return;
            readingIndex=index;revealProgress=SurveyProgress.Has(index)?1:0;focusing=false;
            archive.SetActive(true);Lock();closedUntil=Time.unscaledTime+.25f;
            bool bottle=index==0;
            var truthRect=(RectTransform)truthLayer.transform;truthRect.anchoredPosition=new Vector2(-176,bottle?85:-55);truthRect.sizeDelta=new Vector2(650,bottle?340:247);
            truthText.rectTransform.sizeDelta=truthRect.sizeDelta;
            archiveTitle.text=bottle?"XII / UNDARK, A MODERN PROMISE":"VI / A DISPUTED DIAGNOSIS";
            officialText.text=bottle?"OFFICIAL LAYER / FICTIONAL RECONSTRUCTION\n\nA modern light for everyday life.\nProduction continues without interruption.\nOccupational risk: ████████":"COMPANY INDEX / FICTIONAL RECONSTRUCTION\n\nWorkplace hazard: not recorded.\nCause of death: ████████\nCompensation request: withheld.";
            truthText.text=bottle?"HISTORICAL TRACE / LIP-POINTING\n\nDial painters shaped their brushes with their lips, bringing radioactive paint into the mouth. The apparent precision of the work concealed exposure inside the body.\n\nEach repeated gesture brought the hazard closer.":"HISTORICAL RESEARCH SUMMARY\n\nAmelia \"Mollie\" Maggia's death was attributed to syphilis. Later investigations linked dial painters' illnesses to radium exposure.\n\nA medical record could conceal the workplace as a cause of harm. The hidden layer restores that context.";
            sourceText.text=bottle?"Source: Historic American Engineering Record, U.S. Radium Corporation, NJ-121.\nWorkshop photograph: thesis research collection. Interaction and official layer are reconstructions.":"Sources: NPS / HAER NJ-121; La Medicina del Lavoro (2017), PMC7682169.\nClinical image: thesis Fig.1.2; patient identity unconfirmed. This is not a verbatim medical certificate.";
            archiveHint.text=bottle?"HOLD [F] / LIP-POINTING FOCUS  ·  2 SECONDS   |   [ESC] CLOSE":"DRAG THE GREEN BRUSH / 3 STROKES   ·   OR HOLD [F] 3s   |   [ESC] CLOSE";
            archivePicture.texture=bottle?workshop:clinical;
            if(bottle&&scene=="ROOM1")
            {
                archiveTitle.text="XII / THE LIGHT WE CARRIED HOME";
                officialText.text="COMMERCIAL PROMISE / FICTIONAL RECONSTRUCTION\n\nA modern light for every home.\nA bright complexion. Safe, precise work.\nOccupational risk: [REDACTED]";
                truthText.text="RECONSTRUCTED DIARY / LIP-POINTING\n\nThey taught me to shape the brush with my lips. By evening, paint dust clung to my clothes. We laughed at the light on our teeth.\n\nNobody told us what the light could carry inside the body.";
                sourceText.text="Historical basis: Library of Congress, Radium Girls: Living Dead Women (2019); HAER NJ-121.\nFirst-person diary and commercial layer are fictional reconstructions, not quotations. Workshop photo: research collection.";
                archiveHint.text="HOLD [F] / LIP-POINTING FOCUS  -  2 SECONDS   |   [ESC] CLOSE";
            }
            FitPicture(archivePicture,305,330);
            scratch.ResetSurface();scratch.gameObject.SetActive(!bottle&&revealProgress<1);
            UpdateArchive();
            SurveyTypography.Instance?.LayoutArchive(this);
        }
        public void OpenAdvertisement(TownAdvertisement source)
        {
            if(IsReading||MenuVisible||BlocksWorld||source==null)return;
            if(Camera.main==null||!source.CanReadFrom(Camera.main.transform.position))return;
            OpenArchive(0);readingIndex=-1;revealProgress=1;advertisementSource=source;
            advertisementTextColor=truthText.color;truthText.color=new Color(.87f,.88f,.83f);
            archiveTitle.text="RADIUM WATER / THE PROMISE OF VITALITY";
            officialLayer.alpha=0;truthLayer.alpha=1;truthText.text=TownAdvertisement.Copy;
            archivePicture.texture=source.advertisement;archivePicture.gameObject.SetActive(true);
            scratch.gameObject.SetActive(false);focusMarker.gameObject.SetActive(false);
            sourceText.text="Artistic reading of commercial imagery; not a transcription or a medical claim.\nOriginal advertisement: Radium Emanation Water / Rayode, The Colorado Radium Products Company.";
            revealLabel.text="ADVERTISEMENT / COMMERCIAL PROMISE";
            archiveHint.text="[E] OR [ESC] RETURN TO THE TOWN";
            SurveyTypography.Instance?.LayoutArchive(this);
            var photoLabel=archivePicture.transform.parent.Find("Photograph label");
            if(photoLabel!=null)photoLabel.GetComponent<TMP_Text>().text="RADIUM WATER ADVERTISEMENT";
        }
        public void AdvanceArchive(float seconds)
        {
            if(!IsReading||revealProgress>=1)return;
            revealProgress=Mathf.Min(1,revealProgress+seconds/(IsClinicalReading?3:readingIndex==0?2:3));
            if(revealProgress>=1)CompleteArchive();else UpdateArchive();
        }
        public void CompleteArchive()
        {
            if(!IsReading)return;revealProgress=1;SurveyProgress.Collect(readingIndex);scratch.gameObject.SetActive(false);
            var rect=(RectTransform)truthLayer.transform;rect.anchoredPosition=new Vector2(-176,85);rect.sizeDelta=new Vector2(650,340);truthText.rectTransform.sizeDelta=rect.sizeDelta;
            UpdateArchive();SurveyTypography.Instance?.LayoutArchive(this);effects.PlayOneShot(reveal,.3f);
            if(clinicalDesk!=null){clinicalDesk.Retain();clinicalReader.Reveal();}
        }
        void UpdateArchive()
        {
            bool medical=readingIndex==2;
            officialLayer.alpha=medical&&revealProgress<1?1:1-Mathf.SmoothStep(0,1,revealProgress);
            truthLayer.alpha=medical?1:Mathf.SmoothStep(0,1,revealProgress);
            archivePicture.gameObject.SetActive(revealProgress>=1);
            if(revealProgress>=1)archiveHint.text="TRACE RETAINED  /  [E] OR [ESC] RETURN TO THE SURVEY";
            revealLabel.text=revealProgress>=1?"TRACE RETAINED / "+(readingIndex==0?"XII":"VI"):"FOCUS  "+Mathf.RoundToInt(revealProgress*100)+"%";
        }
        public void CloseArchive()
        {
            if(!IsReading)return;if(clinicalDesk!=null){clinicalReader.Hide();clinicalDesk=null;}
            archive.SetActive(false);Unlock();closedUntil=Time.unscaledTime+.25f;
            if(advertisementSource!=null){var source=advertisementSource;advertisementSource=null;truthText.color=advertisementTextColor;var label=archivePicture.transform.parent.Find("Photograph label");if(label!=null)label.GetComponent<TMP_Text>().text="RESEARCH COLLECTION";source.ReaderClosed();}
            if(scene=="ROOM1")TownPrologue.Active?.ArchiveClosed(readingIndex);
        }
        public void BeginInspection()
        {
            if(!MenuVisible)return;SurveyProgress.Started=true;menu.SetActive(false);Unlock();effects.PlayOneShot(boot,.25f);Notify("INSPECTION AUTHORIZED / TRACE THE RESIDUE",4);
        }
        public static bool EnterPortal(TransitionPoint portal)
        {
            if(Instance==null||SceneManager.GetActiveScene().name!="ROOM1"||portal.sceneName!="ROOM2")return false;
            if(TownPrologue.Active!=null&&!TownPrologue.Active.CanEnter){Instance.Notify("INSPECT THE RADIUM ADVERTISEMENTS BEFORE ENTERING THE ANOMALY",3);return true;}
            Instance.StartCoroutine(Instance.CleanupTransition(portal));return true;
        }
        IEnumerator CleanupTransition(TransitionPoint portal)
        {
            Lock();shock=1;Notify("<color=#74FF95>TRACE ACCEPTED / TRANSFER TO THE LOWER SECTOR</color>",3);effects.PlayOneShot(boot,.25f);
            if(SurveyProgress.ReducedMotion)yield return new WaitForSecondsRealtime(1.5f);
            else for(int frame=0;frame<8;frame++)
            {
                string message=frame%3==0?"[0x226] ERR0R: ISOTOPE INTEGRITY LOST / LOWER SECTOR TRANSFER":frame%3==1?"TRACE ACCEPTED / TRANSFER TO THE LOWER SECTOR":"ER_ROR: SURVEYOR CONTAMINATED / D3SCENDING TO LOWER SECTOR";
                Notify("<color=#74FF95>"+message+"</color>",1);yield return new WaitForSecondsRealtime(.19f);
            }
            Unlock();SceneController.GetOrCreate().TransitionToDestination(portal);
        }
        void Lock()
        {
            if(ownsLock)return;ownsLock=true;SurveyProgress.Modal=true;
            if(player==null)FindPlayer();
            lockedPlayer=player!=null?player.GetComponent<PlayerInput>():null;lockedInputs=player!=null?player.GetComponent<StarterAssetsInputs>():null;
            cursorMode=Cursor.lockState;cursorVisible=Cursor.visible;
            if(lockedPlayer!=null){inputActive=lockedPlayer.inputIsActive;lockedPlayer.DeactivateInput();}
            if(lockedInputs!=null){lookActive=lockedInputs.cursorInputForLook;cursorActive=lockedInputs.cursorLocked;lockedInputs.cursorInputForLook=false;lockedInputs.cursorLocked=false;ZeroInputs();}
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        void ZeroInputs(){if(lockedInputs!=null){lockedInputs.move=Vector2.zero;lockedInputs.look=Vector2.zero;lockedInputs.jump=false;lockedInputs.sprint=false;}}
        void Unlock()
        {
            if(!ownsLock)return;ownsLock=false;ZeroInputs();
            if(lockedInputs!=null){lockedInputs.cursorInputForLook=lookActive;lockedInputs.cursorLocked=cursorActive;}
            if(lockedPlayer!=null&&inputActive)lockedPlayer.ActivateInput();
            Cursor.lockState=cursorMode;Cursor.visible=cursorVisible;SurveyProgress.Modal=false;lockedPlayer=null;lockedInputs=null;
        }
        public void SuppressWorldOverlay()
        {
            // Story readers can open after this HUD's Update. Hide immediately,
            // so the transition frame has the same reading hierarchy as later frames.
            hud.alpha=0;hud.blocksRaycasts=false;hud.interactable=false;
            banner.SetActive(false);decoder.alpha=0;visionFrame.alpha=0;oath.SetActive(false);
            detector.Stop();
        }
        public void SetExternalModal(bool value){if(value)Lock();else Unlock();}
        public void SetReducedMotion(bool value){SurveyProgress.ReducedMotion=value;ApplyReducedMotion();}
        void ApplyReducedMotion()
        {
            if(story!=null)story.SetReducedMotion(SurveyProgress.ReducedMotion);
            foreach(var scan in FindObjectsByType<SpatialScanEvent>(FindObjectsSortMode.None))scan.screenShockEnabled=!SurveyProgress.ReducedMotion;
        }
        public void ButtonFeedback(bool pressed){if(effects!=null)effects.PlayOneShot(pressed?click:tick,pressed?.16f:.09f);}
        public void PreviewEnding(int value)
        {
            story?.embodied?.SetEndingPreview(value);
            Preview=value;if(previewAudio==null)return;
            if(story!=null&&story.ui!=null)
            {
                var panel=story.ui.decisionPanel.GetComponent<UnityEngine.UI.Image>();
                if(panel!=null)panel.color=value==0?new Color(.045f,.05f,.048f,.99f):value==1?new Color(.012f,.055f,.029f,.99f):new Color(.013f,.019f,.017f,.985f);
                var text=story.ui.decisionPanel.transform.Find("Ending preview");
                if(text!=null)text.GetComponent<TMP_Text>().text=value==0?"Erase the retained traces. Return to a town without the portal.":value==1?"Retain the archive. Let the monument remain as a witness.":"Move the cursor over a choice to hear its promise.";
            }
            previewAudio.Stop();if(story!=null&&story.soundscape!=null)return;if(value<0)return;
            previewAudio.clip=value==0?musicBox:chime;previewAudio.loop=value==0;previewAudio.volume=.12f;previewAudio.Play();
        }
        public void StyleStory(Room3StoryUI ui)
        {
            SkinButton(ui.formatButton,true,false);SkinButton(ui.preserveButton,true,true);SkinButton(ui.closeButton,false,false);SkinButton(ui.restartButton,false,false);SkinButton(ui.clinicalButton,false,false);
            SkinPanel(ui.archivePanel);SkinPanel(ui.decisionPanel);SkinPanel(ui.endingPanel);
            ui.archivePanel.GetComponent<UnityEngine.UI.Image>().color=new Color(.009f,.014f,.012f,1);
            ui.archiveBody.fontSize=20;ui.whiteText.fontSize=23;ui.greenText.fontSize=23;
            if(ui.decisionPanel.transform.Find("Ending preview")==null)
            {
                var go=new GameObject("Ending preview",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(ui.decisionPanel.transform,false);
                var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,-255);r.sizeDelta=new Vector2(850,54);
                var label=go.GetComponent<TMP_Text>();label.font=ui.archiveBody.font;label.fontSize=20;label.color=new Color(.6f,.8f,.69f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;label.text="Move the cursor over a choice to hear its promise.";
            }
        }
        void SkinPanel(GameObject panel)
        {
            var image=panel.GetComponent<UnityEngine.UI.Image>();if(image!=null)image.color=new Color(.013f,.019f,.017f,.985f);
            if(panel.transform.Find("Bloodlines perimeter")!=null)return;
            var child=new GameObject("Bloodlines perimeter",typeof(RectTransform),typeof(UnityEngine.UI.Image));child.transform.SetParent(panel.transform,false);
            var rect=(RectTransform)child.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,8);rect.offsetMax=new Vector2(-8,-8);
            var graphic=child.GetComponent<UnityEngine.UI.Image>();graphic.sprite=frameSprite;graphic.material=artwork;graphic.color=new Color(.24f,.43f,.34f,.36f);graphic.raycastTarget=false;child.transform.SetAsFirstSibling();
        }
        void SkinButton(UnityEngine.UI.Button button,bool ending,bool preserve)
        {
            if(button==null)return;var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=buttonSprite;image.type=UnityEngine.UI.Image.Type.Simple;image.material=artwork;
            button.transition=UnityEngine.UI.Selectable.Transition.None;
            var feedback=button.GetComponent<SurveyButton>();if(feedback==null)feedback=button.gameObject.AddComponent<SurveyButton>();
            feedback.image=image;feedback.ending=ending;feedback.preserve=preserve;feedback.idle=new Color(.43f,.53f,.47f);feedback.hover=new Color(.43f,.95f,.63f);image.color=feedback.idle;
            if(ending&&preserve){feedback.idle=new Color(.28f,.65f,.43f);image.color=feedback.idle;}
        }
        static void FitPicture(RawImage image,float w,float h)
        {
            if(image.texture==null)return;float ratio=(float)image.texture.width/image.texture.height;
            image.rectTransform.sizeDelta=ratio>w/h?new Vector2(w,w/ratio):new Vector2(h*ratio,h);
        }
    }
}

