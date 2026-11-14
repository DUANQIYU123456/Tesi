using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Xiaoduan.Room3
{
    public sealed class Room3StoryUI : MonoBehaviour
    {
        public Room3StoryFlow flow;
        public TMP_Text objectiveText,recordText,settingsText,promptText,promptCaption,noticeText;
        public CanvasGroup promptGroup,noticeGroup;
        public UnityEngine.UI.Image progressFill,fade;
        public GameObject archivePanel,decisionPanel,endingPanel,calibration;
        public TMP_Text archiveTitle,archiveBody,archiveSource,revealLabel,endingTitle,endingBody,endingCredits;
        public TMP_Text ledgerDiagram;
        public UnityEngine.UI.RawImage archivePicture,portraitMollie,portraitGrace;
        public Texture clinicalImage,newspaperImage,mollieImage,graceImage;
        public UnityEngine.UI.Button closeButton,formatButton,preserveButton,restartButton,clinicalButton;
        public RectTransform targetMarker;
        public CanvasGroup whiteRecord,greenRecord;
        public TMP_Text whiteText,greenText;
        float noticeUntil;
        int archive;
        bool clinicalShown;
        public bool DecisionVisible=>decisionPanel!=null&&decisionPanel.activeSelf;
        void Awake()
        {
            closeButton.onClick.AddListener(()=>flow.CloseArchive());
            formatButton.onClick.AddListener(()=>flow.ChooseEnding(false));
            preserveButton.onClick.AddListener(()=>flow.ChooseEnding(true));
            restartButton.onClick.AddListener(()=>flow.Restart());
            clinicalButton.onClick.AddListener(()=>{clinicalShown=!clinicalShown;archivePicture.texture=clinicalShown?clinicalImage:mollieImage;archivePicture.gameObject.SetActive(true);Fit(archivePicture,275,350);});
            archivePanel.SetActive(false);decisionPanel.SetActive(false);endingPanel.SetActive(false);calibration.SetActive(false);
            fade.color=new Color(0,0,0,0);promptGroup.alpha=0;noticeGroup.alpha=0;
        }
        void Update(){noticeGroup.alpha=Mathf.MoveTowards(noticeGroup.alpha,Time.unscaledTime<noticeUntil?1:0,Time.unscaledDeltaTime*5);}
        public void SetStatus(string objective,int count,bool reduced)
        {
            objectiveText.text=objective;
            recordText.text="ARCHIVE TRACES   "+(count>0?"<color=#A4EDAB>VI</color>":"VI")+"  /  "+(count>1?"<color=#A4EDAB>IX</color>":"IX")+"    "+count+" / 2";
            settingsText.text="[M]  REDUCED MOTION: "+(reduced?"ON":"OFF");
        }
        public void SetPrompt(string action,string caption,float progress)
        {
            promptText.text=action;promptCaption.text=caption;
            promptGroup.alpha=string.IsNullOrEmpty(action)?0:1;
            progressFill.transform.parent.gameObject.SetActive(progress>0);
            progressFill.fillAmount=progress;
        }
        public void Notice(string message,float seconds){noticeText.text=message;noticeUntil=Time.unscaledTime+seconds;}
        public void SetCalibration(bool visible,Vector3 position,Camera camera,float progress)
        {
            calibration.SetActive(visible);
            if(!visible||camera==null)return;
            Vector3 pixel=camera.WorldToScreenPoint(position);
            targetMarker.gameObject.SetActive(pixel.z>0);
            if(pixel.z>0&&RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)calibration.transform,pixel,null,out var local))targetMarker.anchoredPosition=local;
        }
        public void ShowArchive(int index,float amount)
        {
            archive=index;clinicalShown=false;archivePanel.SetActive(true);
            if(index==2)
            {
                archiveTitle.text="LABOR / THE WORK BEHIND THE OUTPUT";
                whiteText.text="<b>PRODUCTION INDEX / FICTIONAL SYSTEM</b>\n\nOutput: accepted.\nWorker: omitted from the index.\nRepeat the task. Keep the surface clean.";
                greenText.text="<b>TWO WORKPLACES / A CRITICAL COMPARISON</b>\n\nDIAL PAINTING\nRepeated brushwork made luminous dials. Exposure could remain hidden in a worker's body.\n\nDIGITAL DATA WORK\nPeople label, check and filter information behind services presented as automated. Different work and harms. Whose time disappears from the finished product?";
                archiveSource.text="Gray & Suri, Ghost Work (2019); Crawford, Atlas of AI (2021). Historical comparison: NPS / HAER NJ-121.\nResearch summary and fictional interface; no claim of identical disease or working conditions.";
                archivePicture.gameObject.SetActive(false);clinicalButton.gameObject.SetActive(false);
            }
            else if(index==0)
            {
                archiveTitle.text="VI / A DISPUTED DIAGNOSIS";
                whiteText.text="<b>COMPANY FILE / RECONSTRUCTED INTERFACE</b>\n\nNo workplace hazard recorded.\nCause of death: <mark=#222C27>████████</mark>\nCompensation request: withheld.";
                greenText.text="<b>HISTORICAL RESEARCH SUMMARY</b>\n\nAmelia \"Mollie\" Maggia's death was attributed to syphilis. Later investigation linked dial painters' illness to radium exposure.\n\nThe diagnosis and the industrial conditions must be read together. A record can conceal as well as document.";
                archiveSource.text="Sources: NPS / HAER NJ-121; La Medicina del Lavoro (2017), PMC7682169.\nClinical image: thesis Fig.1.2, patient identity unconfirmed. The text is a summary, not a doctor's quotation.";
                archivePicture.texture=mollieImage;
                clinicalButton.gameObject.SetActive(amount>=1);
            }
            else
            {
                archiveTitle.text="IX / FIVE WOMEN DOOMED TO DIE";
                whiteText.text="<b>ACCESS INDEX / FICTIONAL SYSTEM</b>\n\nHearing status: pending.\nApplicant statement: <mark=#222C27>████████</mark>\nNo further action authorized.";
                greenText.text="<b>HISTORICAL TIMELINE</b>\n\nFive former New Jersey dial painters sued in 1927. Hearings followed in January and April 1928. An adjournment until September drew public criticism.\n\nThey accepted an out-of-court settlement on 4 June 1928.\n\nThe testimony gate is a symbolic reconstruction, not a reenactment of a documented oath.";
                archiveSource.text="The World (New York), 10 May 1928, p.14; clipping reproduced from thesis Fig.1.5.\nChronology checked against the Library of Congress. Modern digital labor is a critical comparison, not an identical medical condition.";
                archivePicture.texture=newspaperImage;
                clinicalButton.gameObject.SetActive(false);
            }
            archiveBody.text=index==2?"Hold [F] or [SPACE] to retain the omitted labor record. [E] / [ESC] to close.":"Hold [F] or [SPACE] to reveal the concealed layer. [E] / [ESC] to close.";
            Fit(archivePicture,275,350);
            Reveal(amount);
        }
        public void Reveal(float amount)
        {
            whiteRecord.alpha=1-Mathf.SmoothStep(0,1,amount);
            greenRecord.alpha=Mathf.SmoothStep(0,1,amount);
            revealLabel.text=amount>=1?"TRACE RETAINED":"REVEALING  "+Mathf.RoundToInt(amount*100)+"%";
            archivePicture.gameObject.SetActive(amount>.9f&&archive!=2);
            if(ledgerDiagram!=null)ledgerDiagram.gameObject.SetActive(archive==2&&amount>.9f);
            if(archive==0)clinicalButton.gameObject.SetActive(amount>=1);
        }
        public void HideArchive(){archivePanel.SetActive(false);if(ledgerDiagram!=null)ledgerDiagram.gameObject.SetActive(false);}
        public void ShowDecision(){decisionPanel.SetActive(true);}
        public void HideDecision(){decisionPanel.SetActive(false);}
        public void SetFade(float alpha,Color color){color.a=alpha;fade.color=color;}
        public void ShowEnding(bool preserve)
        {
            endingPanel.SetActive(true);archivePanel.SetActive(false);decisionPanel.SetActive(false);promptGroup.alpha=0;
            objectiveText.transform.parent.gameObject.SetActive(false);settingsText.gameObject.SetActive(false);calibration.SetActive(false);noticeUntil=0;noticeGroup.alpha=0;
            endingTitle.text=preserve?"Custodisci le rovine":"Ripristina l'ordine";
            endingBody.text=preserve?"THE ARCHIVE IS RETAINED\n\nGrace Fryer\nKatherine Schaub\nEdna Hussman\nAlbina Larice\nQuinta McDonald\n\nAlso remembering Amelia \"Mollie\" Maggia and the other dial painters.":"SECTOR RESET\n\nAnomalous records erased.\nAccess to the sector has been closed.\n\n\"No history remains in the index.\"\n\nA fictional system message. Erasure cannot undo what happened.";
            endingCredits.text=preserve?"Historical references: National Archives, Radium Dial Painters records; Library of Congress.\nPortraits: Mollie Maggia and Grace Fryer, from the thesis research collection.\nThe green light and suspended monument are artistic interpretations.":"This ending changes the current playthrough only.\nRestarting restores the town and its portal.";
            portraitMollie.gameObject.SetActive(preserve);portraitGrace.gameObject.SetActive(preserve);
            portraitMollie.texture=mollieImage;portraitGrace.texture=graceImage;
            Fit(portraitMollie,205,270);Fit(portraitGrace,205,270);
            endingPanel.transform.Find("Portrait name left").gameObject.SetActive(preserve);
            endingPanel.transform.Find("Portrait name right").gameObject.SetActive(preserve);
        }
        static void Fit(UnityEngine.UI.RawImage picture,float width,float height)
        {
            if(picture.texture==null)return;
            float ratio=(float)picture.texture.width/picture.texture.height;
            picture.rectTransform.sizeDelta=ratio>width/height?new Vector2(width,width/ratio):new Vector2(height*ratio,height);
        }
    }
}

