using TMPro;
using UnityEngine;

namespace Xiaoduan.Narrative
{
    public sealed class NarrativeHUD : MonoBehaviour
    {
        public NarrativeTheme theme;
        public RectTransform safeArea,promptRect,cardRect;
        public CanvasGroup promptGroup,readingGroup,introGroup;
        public TMP_Text promptAction,promptTitle,cardCategory,cardTitle,cardBody,closeText,returnText,roomText,introText;
        public UnityEngine.UI.Button closeButton;
        public UnityEngine.UI.Image dot;
        public CanvasGroup focusGroup;
        bool showPrompt,showReading,focused;
        float introUntil;
        Rect lastSafe;
        Vector2Int lastSize;
        Vector2 lastSafeSize;

        void Awake()
        {
            ApplyTheme();
            promptGroup.alpha=0; readingGroup.alpha=0;
            readingGroup.blocksRaycasts=false; readingGroup.interactable=false;
            roomText.text=gameObject.scene.name=="ROOM1" ? theme.room1Label : theme.room2Label;
            introText.text=theme.introLabel;
            closeText.text=theme.closeLabel+"  [ESC]";
            returnText.text=theme.returnLabel;
            introUntil=Time.unscaledTime+(gameObject.scene.name=="ROOM1" ? 7f : 0f);
            introGroup.alpha=0;
            Layout();
        }
        void Start() { Layout(); }
        [ContextMenu("Apply Theme")]
        public void ApplyTheme()
        {
            if (theme==null) return;
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            { if (theme.font!=null) label.font=theme.font; label.color=theme.text; }
            promptAction.fontSize=theme.promptSize;
            promptTitle.fontSize=theme.captionSize; promptTitle.color=theme.muted;
            cardTitle.fontSize=theme.titleSize;
            cardBody.fontSize=theme.bodySize;
            cardCategory.fontSize=theme.captionSize; cardCategory.color=theme.accent;
            closeText.fontSize=theme.captionSize;
            returnText.fontSize=theme.captionSize; returnText.color=theme.muted;
            introText.fontSize=theme.captionSize;
            roomText.fontSize=theme.captionSize; roomText.color=theme.text;
            foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (graphic.name.Contains("Accent") || graphic.name.StartsWith("Focus ")) graphic.color=theme.accent;
                if (graphic.name=="Observation Card") graphic.color=theme.panel;
            }
        }
        public void SetPrompt(string action,string title)
        {
            showPrompt=!string.IsNullOrEmpty(action);
            if (promptAction.text!=action) promptAction.text=action;
            if (promptTitle.text!=title) promptTitle.text=title;
        }
        public void SetFocus(bool hasTarget) { focused=hasTarget; }
        public void ShowEntry(NarrativeEntry entry)
        {
            cardCategory.text=entry.category;
            cardTitle.text=entry.title;
            cardBody.text=entry.body;
            showReading=true;
            readingGroup.blocksRaycasts=true; readingGroup.interactable=true;
        }
        public void HideEntry()
        {
            showReading=false;
            readingGroup.blocksRaycasts=false; readingGroup.interactable=false;
        }
        void Update()
        {
            float step=Time.unscaledDeltaTime/Mathf.Max(.05f,theme.fadeSeconds);
            promptGroup.alpha=Mathf.MoveTowards(promptGroup.alpha,showPrompt && !showReading ? 1:0,step);
            readingGroup.alpha=Mathf.MoveTowards(readingGroup.alpha,showReading ? 1:0,step);
            introGroup.alpha=Mathf.MoveTowards(introGroup.alpha,Time.unscaledTime<introUntil && !showReading && !showPrompt ? 1:0,step);
            focusGroup.alpha=Mathf.MoveTowards(focusGroup.alpha,focused && !showReading ? 1:0,step);
            dot.enabled=!showReading;
            dot.color=Color.Lerp(dot.color,focused ? theme.accent : theme.muted,Time.unscaledDeltaTime*10);
            promptRect.anchoredPosition=new Vector2(0,110-8*(1-promptGroup.alpha));
            if (Screen.safeArea!=lastSafe || lastSize.x!=Screen.width || lastSize.y!=Screen.height || safeArea.rect.size!=lastSafeSize) Layout();
        }
        void Layout()
        {
            lastSafe=Screen.safeArea;
            lastSize=new Vector2Int(Screen.width,Screen.height);
            if (Screen.width<=0 || Screen.height<=0) return;
            safeArea.anchorMin=lastSafe.position/new Vector2(Screen.width,Screen.height);
            safeArea.anchorMax=(lastSafe.position+lastSafe.size)/new Vector2(Screen.width,Screen.height);
            safeArea.offsetMin=Vector2.zero; safeArea.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();
            lastSafeSize=safeArea.rect.size;
            cardRect.sizeDelta=new Vector2(Mathf.Min(900,safeArea.rect.width-64),Mathf.Min(620,safeArea.rect.height-64));
            promptRect.sizeDelta=new Vector2(Mathf.Min(620,safeArea.rect.width-64),86);
        }
    }
}
