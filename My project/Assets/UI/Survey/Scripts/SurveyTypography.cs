using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Xiaoduan.Narrative;
using Xiaoduan.Room3;

namespace Xiaoduan.Survey
{
    public sealed class SurveyTypography:MonoBehaviour
    {
        public TMP_FontAsset bodyFont,titleFont,instrumentFont;
        public bool LargeText {get;private set;}
        readonly Dictionary<TMP_Text,float> sizes=new Dictionary<TMP_Text,float>();
        Room3StoryUI story;
        public static SurveyTypography Instance {get;private set;}
        const string LargeKey="XD.Survey.LargeText";
        void Awake(){Instance=this;LargeText=PlayerPrefs.GetInt(LargeKey,0)!=0;SceneManager.sceneLoaded+=SceneLoaded;}
        void Start(){StartCoroutine(RefreshLater());}
        void SceneLoaded(Scene _,LoadSceneMode __){StartCoroutine(RefreshLater());}
        IEnumerator RefreshLater(){yield return null;yield return null;Apply();}
        void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;if(Instance==this)Instance=null;}
        public void SetLargeText(bool value){LargeText=value;PlayerPrefs.SetInt(LargeKey,value?1:0);PlayerPrefs.Save();Apply();}
        public void Apply()
        {
            var hud=SurveyHUD.Instance;if(hud==null||bodyFont==null)return;
            story=FindFirstObjectByType<Room3StoryUI>();
            foreach(var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!sizes.ContainsKey(text))sizes[text]=text.fontSize;
                var font=text==hud.cpm||text==hud.traceCount||text==hud.integrity||text==hud.reducedText||System.Array.IndexOf(hud.fragments,text)>=0||text.name.StartsWith("Index ")?instrumentFont:
                    text==hud.roomLabel||sizes[text]>=30||text.name.Contains("title")||text.name.Contains("Title")||text.GetComponentInParent<UnityEngine.UI.Button>()!=null?titleFont:bodyFont;
                if(text.font!=font){text.font=font;text.fontSharedMaterial=font.material;}
                text.enableAutoSizing=false;
                float baseline=sizes[text];
                if(text==hud.truthText)baseline=26;
                if(text==hud.sourceText)baseline=18;
                if(text==hud.officialText)baseline=24;
                if(text==hud.objective)baseline=23;
                if(story!=null&&(text==story.greenText||text==story.whiteText))baseline=26;
                if(story!=null&&text==story.archiveSource)baseline=18;
                bool reading=text==hud.truthText||text==hud.officialText||text==hud.sourceText||
                    (story!=null&&(text==story.greenText||text==story.whiteText||text==story.archiveSource));
                text.fontSize=baseline*(reading&&LargeText?1.15f:1);
            }
            LayoutArchive(hud);
            if(story!=null)
            {
                ((RectTransform)story.archivePanel.transform).sizeDelta=new Vector2(1240,850);
                story.archiveTitle.rectTransform.anchoredPosition=new Vector2(0,367);
                story.archiveTitle.rectTransform.sizeDelta=new Vector2(1120,60);
                story.archiveBody.rectTransform.anchoredPosition=new Vector2(0,307);
                story.whiteText.rectTransform.anchoredPosition=story.greenText.rectTransform.anchoredPosition=new Vector2(-175,35);
                story.whiteText.rectTransform.sizeDelta=story.greenText.rectTransform.sizeDelta=new Vector2(730,470);
                story.archivePicture.rectTransform.anchoredPosition=new Vector2(435,50);
                story.clinicalButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(435,-180);
                story.archiveSource.rectTransform.anchoredPosition=new Vector2(0,-291);
                story.archiveSource.rectTransform.sizeDelta=new Vector2(1120,92);
                story.revealLabel.rectTransform.anchoredPosition=new Vector2(0,-365);
                story.closeButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(435,-365);
            }
        }
        public void LayoutArchive(SurveyHUD hud)
        {
            if(hud==null)return;
            var panel=(RectTransform)hud.archiveTitle.transform.parent;panel.sizeDelta=new Vector2(1240,850);
            hud.archiveTitle.rectTransform.anchoredPosition=new Vector2(0,367);hud.archiveTitle.rectTransform.sizeDelta=new Vector2(1120,58);
            var rule=panel.Find("Header rule") as RectTransform;if(rule!=null){rule.anchoredPosition=new Vector2(0,320);rule.sizeDelta=new Vector2(1120,2);}
            hud.officialText.rectTransform.sizeDelta=new Vector2(730,190);
            ((RectTransform)hud.officialLayer.transform).anchoredPosition=new Vector2(-175,200);
            var truth=(RectTransform)hud.truthLayer.transform;
            bool concealed=hud.IsReading&&hud.DecodeProgress<1&&hud.scratch.gameObject.activeSelf;
            truth.anchoredPosition=new Vector2(-175,concealed?-40:80);truth.sizeDelta=new Vector2(730,concealed?258:385);
            hud.truthText.rectTransform.sizeDelta=truth.sizeDelta;
            hud.scratch.rectTransform.sizeDelta=truth.sizeDelta;
            hud.archivePicture.rectTransform.anchoredPosition=new Vector2(435,80);
            var photoLabel=panel.Find("Photograph label") as RectTransform;if(photoLabel!=null)photoLabel.anchoredPosition=new Vector2(435,-110);
            hud.revealLabel.rectTransform.anchoredPosition=new Vector2(-175,-240);hud.revealLabel.rectTransform.sizeDelta=new Vector2(730,35);
            hud.sourceText.rectTransform.anchoredPosition=new Vector2(0,-308);hud.sourceText.rectTransform.sizeDelta=new Vector2(1120,83);
            hud.archiveHint.rectTransform.anchoredPosition=new Vector2(-75,-378);hud.archiveHint.rectTransform.sizeDelta=new Vector2(970,35);hud.archiveHint.fontSize=18;
            hud.closeButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(532,-377);
        }
    }
}
