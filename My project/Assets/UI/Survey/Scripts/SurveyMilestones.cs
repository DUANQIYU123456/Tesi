using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Xiaoduan.Panopticon;
using Xiaoduan.Room3;

namespace Xiaoduan.Survey
{
    // A separate, non-interactive Canvas keeps large type out of the HUD rebuilds.
    [DefaultExecutionOrder(900)]
    public sealed class SurveyMilestones : MonoBehaviour
    {
        public RectTransform plate;
        public CanvasGroup group;
        public TMP_Text headline, index, caption;
        struct Card { public string title, id, detail; public Color color; }
        readonly Queue<Card> pending = new Queue<Card>();
        SurveyHUD hud;
        SurveyPause pause;
        Room3StoryFlow story;
        bool townDone, hallDone, oathDone, showing, introDone;
        float elapsed, cooldown, searchAt;
        public bool Visible => showing && group != null && group.alpha > .01f;
        public int PresentedCount { get; private set; }
        void Awake()
        {
            hud=GetComponent<SurveyHUD>();pause=GetComponent<SurveyPause>();
            group.alpha=0;group.interactable=false;group.blocksRaycasts=false;
            SceneManager.sceneLoaded+=SceneLoaded;
        }
        void SceneLoaded(Scene _,LoadSceneMode __)
        {
            pending.Clear();showing=false;group.alpha=0;elapsed=0;cooldown=0;
            townDone=hallDone=oathDone=introDone=false;story=null;searchAt=0;
        }
        void Queue(string title,string id,string detail,Color color)
        { pending.Enqueue(new Card{title=title,id=id,detail=detail,color=color}); }
        void LateUpdate()
        {
            if(hud==null||group==null)return;
            if(!introDone&&SceneManager.GetActiveScene().name=="ROOM1"&&SurveyProgress.Started&&!hud.MenuVisible)
            {introDone=true;Queue("HOLD RMB", "01 / ISOTOPE VIEW", "REVEAL THE HIDDEN WORLD",new Color(.40f,1,.62f));}
            if(story==null&&Time.unscaledTime>=searchAt)
            {searchAt=Time.unscaledTime+1;story=FindFirstObjectByType<Room3StoryFlow>();}
            var town=TownBlackout.Active;
            if(!townDone&&town!=null&&town.AftermathSettled)
            {townDone=true;Queue("SIGNAL LOST", "01 / COMMERCIAL TRANSMISSION", "The light remains. The promise does not.",new Color(.75f,.86f,.82f));}
            var hall=PanopticonHall.Active;
            if(!hallDone&&hall!=null&&hall.GateReleased&&!hall.Busy)
            {hallDone=true;Queue("ARCHIVE RETAINED", "02 / THREE RECORDS RECOVERED", "Scan terminated. Follow the cold light.",new Color(.78f,.88f,.86f));}
            if(!oathDone&&story!=null&&story.OathOpened)
            {oathDone=true;cooldown=.85f;Queue("TESTIMONY\nRECEIVED", "03 / WITNESS PROTOCOL", "The record is now in your custody.",new Color(.96f,.91f,.73f));}
            bool blocked=hud.MenuVisible||hud.IsReading||SurveyHUD.BlocksWorld||(pause!=null&&pause.Paused)||
                (story!=null&&(story.Reading||story.ui.DecisionVisible||story.EndingChosen||(story.embodied!=null&&story.embodied.Gripping)));
            // Pause the presentation clock with a reader or settings panel; never consume a queued event invisibly.
            if(blocked){group.alpha=0;return;}
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            cooldown=Mathf.Max(0,cooldown-dt);
            if(!showing&&pending.Count>0&&cooldown<=0)
            {
                var card=pending.Dequeue();headline.text=card.title;headline.color=card.color;
                index.text=card.id;caption.text=card.detail;elapsed=0;showing=true;PresentedCount++;
                headline.maxVisibleCharacters=int.MaxValue;
                headline.fontSize=card.title=="HOLD RMB"?96:card.title.Contains("\n")?146:164;
                float width=headline.GetPreferredValues(card.title).x;
                if(width>1580)headline.fontSize*=1580/width;
            }
            if(!showing){group.alpha=0;return;}
            elapsed+=dt;
            const float duration=3.2f;
            group.alpha=Mathf.SmoothStep(0,1,elapsed/.35f)*(1-Mathf.SmoothStep(0,1,(elapsed-2.55f)/.65f));
            bool reduced=SurveyProgress.ReducedMotion;
            float settle=1-Mathf.SmoothStep(0,1,elapsed/.55f);
            plate.anchoredPosition=new Vector2(0,76+(reduced?0:settle*12));
            plate.localScale=Vector3.one*(reduced?1:1+settle*.018f);
            headline.maxVisibleCharacters=reduced?int.MaxValue:Mathf.CeilToInt(elapsed/.32f*headline.text.Length);
            hud.hud.alpha=Mathf.Min(hud.hud.alpha,1-.85f*group.alpha);
            hud.banner.SetActive(false);
            if(story!=null)story.ui.noticeGroup.alpha=0;
            if(elapsed>=duration){showing=false;group.alpha=0;cooldown=.8f;}
        }
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;}
    }
}
