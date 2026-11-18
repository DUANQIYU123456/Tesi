using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using StarterAssets;
using TMPro;
using Xiaoduan.Room3;

namespace Xiaoduan.Survey
{
    [DefaultExecutionOrder(-200)]
    public sealed class SurveyPause:MonoBehaviour
    {
        public GameObject panel;
        public UnityEngine.UI.Button resumeButton,restartButton,motionButton,readingButton;
        public UnityEngine.UI.Slider volumeSlider,sensitivitySlider;
        public TMP_Text motionLabel,readingLabel,volumeLabel,sensitivityLabel;
        public bool Paused {get;private set;}
        float previousTimeScale=1,baseRotationSpeed=1;
        bool previousAudioPause;
        FirstPersonController movement;
        readonly System.Collections.Generic.Dictionary<FirstPersonController,float> rotationDefaults=new System.Collections.Generic.Dictionary<FirstPersonController,float>();
        float nextPlayerSearch;
        bool lastMotion;
        const string Prefix="XD.Survey.";
        void Awake()
        {
            panel.SetActive(false);
            SurveyProgress.ReducedMotion=PlayerPrefs.GetInt(Prefix+"ReducedMotion",0)!=0;lastMotion=SurveyProgress.ReducedMotion;
            AudioListener.volume=PlayerPrefs.GetFloat(Prefix+"Volume",1);
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            sensitivitySlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(Prefix+"Sensitivity",1));
            resumeButton.onClick.AddListener(Resume);restartButton.onClick.AddListener(Restart);
            motionButton.onClick.AddListener(()=>{SurveyHUD.Instance.SetReducedMotion(!SurveyProgress.ReducedMotion);RefreshLabels();});
            readingButton.onClick.AddListener(()=>{var type=SurveyTypography.Instance;if(type!=null)type.SetLargeText(!type.LargeText);RefreshLabels();});
            volumeSlider.onValueChanged.AddListener(v=>{AudioListener.volume=v;PlayerPrefs.SetFloat(Prefix+"Volume",v);RefreshLabels();});
            sensitivitySlider.onValueChanged.AddListener(v=>{PlayerPrefs.SetFloat(Prefix+"Sensitivity",v);ApplySensitivity();RefreshLabels();});
            SceneManager.sceneLoaded+=SceneLoaded;
        }
        void Start(){SurveyHUD.Instance?.SetReducedMotion(SurveyProgress.ReducedMotion);RefreshLabels();}
        void SceneLoaded(Scene _,LoadSceneMode __){if(Paused)Resume();movement=null;nextPlayerSearch=0;}
        void Update()
        {
            if(movement==null&&Time.unscaledTime>=nextPlayerSearch)
            {
                nextPlayerSearch=Time.unscaledTime+.5f;var player=GameObject.FindGameObjectWithTag("Player");
                if(player!=null){movement=player.GetComponent<FirstPersonController>();if(movement!=null){if(!rotationDefaults.ContainsKey(movement))rotationDefaults[movement]=movement.RotationSpeed;baseRotationSpeed=rotationDefaults[movement];ApplySensitivity();}}
            }
            if(lastMotion!=SurveyProgress.ReducedMotion)
            {lastMotion=SurveyProgress.ReducedMotion;PlayerPrefs.SetInt(Prefix+"ReducedMotion",lastMotion?1:0);}
            if(Keyboard.current==null||!Keyboard.current.escapeKey.wasPressedThisFrame)return;
            if(Paused){Resume();return;}
            var hud=SurveyHUD.Instance;var story=FindFirstObjectByType<Room3StoryFlow>();
            if(hud==null||hud.MenuVisible||hud.IsReading||SurveyHUD.BlocksWorld||
               (story!=null&&(story.Reading||story.ui.DecisionVisible||story.EndingChosen||(story.embodied!=null&&story.embodied.Gripping)))||
               (SceneController.Instance!=null&&SceneController.Instance.IsTransitioning))return;
            Open();
        }
        public void Open()
        {
            if(Paused||SurveyHUD.Instance==null||SurveyHUD.BlocksWorld)return;
            Paused=true;previousTimeScale=Time.timeScale;previousAudioPause=AudioListener.pause;
            SurveyHUD.Instance.SetExternalModal(true);Time.timeScale=0;AudioListener.pause=true;panel.SetActive(true);RefreshLabels();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
        }
        public void Resume()
        {
            if(!Paused)return;Paused=false;panel.SetActive(false);Time.timeScale=previousTimeScale;AudioListener.pause=previousAudioPause;
            SurveyHUD.Instance?.SetExternalModal(false);PlayerPrefs.Save();
        }
        public void Restart(){Resume();SurveyProgress.NewGame();SceneManager.LoadScene("ROOM1",LoadSceneMode.Single);}
        void ApplySensitivity(){if(movement!=null)movement.RotationSpeed=baseRotationSpeed*sensitivitySlider.value;}
        public void RefreshLabels()
        {
            motionLabel.text="REDUCED MOTION  "+(SurveyProgress.ReducedMotion?"ON":"OFF");
            readingLabel.text="LARGE READING TEXT  "+(SurveyTypography.Instance!=null&&SurveyTypography.Instance.LargeText?"ON":"OFF");
            volumeLabel.text="MASTER VOLUME  "+Mathf.RoundToInt(volumeSlider.value*100)+"%";
            sensitivityLabel.text="LOOK SENSITIVITY  "+sensitivitySlider.value.ToString("0.0")+"×";
        }
        void OnDestroy(){Resume();SceneManager.sceneLoaded-=SceneLoaded;}
    }
}

