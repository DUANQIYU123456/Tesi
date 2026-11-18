using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Xiaoduan.Survey;

namespace Xiaoduan.Clinical
{
    // Extends the shared Bloodlines reader and its existing input/modal lifecycle.
    public sealed class ClinicalDeskReader : MonoBehaviour
    {
        SurveyHUD hud;
        ClinicalDesk desk;
        RectTransform page,pen;
        ClinicalCoating coating;
        TMP_Text stamp;
        TMP_Text hint;
        AudioSource paper,bone,geiger;
        float tickAt;
        float originalSourceSize;
        bool completing;
        readonly System.Collections.Generic.Dictionary<TMP_Text,float> textSizes=new System.Collections.Generic.Dictionary<TMP_Text,float>();
        public ClinicalCoating Coating=>coating;
        public bool Visible=>page!=null&&page.gameObject.activeSelf;
        public void Show(SurveyHUD view,ClinicalDesk source)
        {
            hud=view;desk=source;completing=false;originalSourceSize=hud.sourceText.fontSize;
            if(page==null)Build();
            hud.officialLayer.gameObject.SetActive(false);hud.truthLayer.gameObject.SetActive(false);
            hud.archivePicture.gameObject.SetActive(false);hud.focusMarker.gameObject.SetActive(false);
            hud.archiveHint.gameObject.SetActive(false);hud.brushCursor.gameObject.SetActive(false);
            var label=hud.archiveTitle.transform.parent.Find("Photograph label");if(label!=null)label.gameObject.SetActive(false);
            hud.archiveTitle.text="1922 / THE DISPUTED MEDICAL RECORD";
            hud.sourceText.text="Reconstructed record and marginal note. Clinical image: thesis Fig.1.2; patient identity unconfirmed.\nSources: NPS / HAER NJ-121; La Medicina del Lavoro (2017), PMC7682169.";
            hud.sourceText.fontSize=18*(SurveyTypography.Instance!=null&&SurveyTypography.Instance.LargeText?1.15f:1);hud.sourceText.rectTransform.anchoredPosition=new Vector2(0,-305);
            hud.sourceText.rectTransform.sizeDelta=new Vector2(1100,78);
            hud.revealLabel.rectTransform.anchoredPosition=new Vector2(0,-244);
            hud.revealLabel.rectTransform.sizeDelta=new Vector2(1080,32);
            page.gameObject.SetActive(true);pen.gameObject.SetActive(true);hint.gameObject.SetActive(true);
            if(coating.texture==null)coating.ResetCoating();stamp.gameObject.SetActive(true);
            paper.clip=source.paperScratch;bone.clip=source.boneFriction;
            if(source.Retained||hud.DecodeProgress>=1)coating.RevealAll();
            source.BeginInspection();Layout();
        }
        public void Layout()
        {
            if(page==null||hud==null)return;
            page.anchoredPosition=new Vector2(0,37);page.sizeDelta=new Vector2(1110,536);
            var panel=(RectTransform)hud.archiveTitle.transform.parent;panel.sizeDelta=new Vector2(1240,850);
            hint.rectTransform.anchoredPosition=new Vector2(-70,-370);hint.rectTransform.sizeDelta=new Vector2(950,46);
            hud.closeButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(532,-377);
            float scale=SurveyTypography.Instance!=null&&SurveyTypography.Instance.LargeText?1.12f:1;
            foreach(var pair in textSizes)if(pair.Key!=null)pair.Key.fontSize=pair.Value*scale;
        }
        void Build()
        {
            var parent=hud.archiveTitle.transform.parent;
            page=Rect("Clinical document / 1922",parent,Vector2.zero,new Vector2(1110,536));
            var sheet=page.gameObject.AddComponent<UnityEngine.UI.Image>();sheet.color=new Color(.88f,.865f,.80f);sheet.raycastTarget=false;
            var ink=new Color(.08f,.09f,.08f);
            Text("Medical title",page,new Vector2(-260,220),new Vector2(520,40),24,ink,"RECONSTRUCTED MEDICAL RECORD");
            Text("File number",page,new Vector2(355,220),new Vector2(320,36),19,ink,"1922-MM / RESTRICTED");
            var line=Image("Printed rule",page,new Vector2(0,184),new Vector2(1020,2),new Color(.25f,.28f,.24f));
            Text("Registered identity",page,new Vector2(-233,149),new Vector2(560,34),23,ink,"Amelia \"Mollie\" Maggia   |   1922");
            Text("Registered diagnosis",page,new Vector2(0,103),new Vector2(1020,44),25,ink,"RECORDED DIAGNOSIS:  SYPHILIS");
            var exposed=Rect("Concealed clinical evidence",page,new Vector2(0,-98),new Vector2(1020,320));
            Image("Evidence backing",exposed,Vector2.zero,new Vector2(1020,320),new Color(.035f,.055f,.045f));
            var photo=Rect("Thesis Fig 1.2 / original clinical photograph",exposed,new Vector2(-348,22),new Vector2(250,250)).gameObject.AddComponent<RawImage>();
            photo.texture=desk.clinicalImage;photo.raycastTarget=false;
            if(photo.texture!=null){float ratio=(float)photo.texture.width/photo.texture.height;photo.rectTransform.sizeDelta=new Vector2(250*ratio,250);}
            Text("Clinical identity",exposed,new Vector2(-348,-126),new Vector2(286,44),16,new Color(.65f,.73f,.69f),"CLINICAL REFERENCE\nIDENTITY UNCONFIRMED");
            Text("Evidence statement",exposed,new Vector2(145,46),new Vector2(620,110),30,new Color(.65f,.83f,.70f),"The workplace was missing\nfrom the record.");
            Text("Evidence note",exposed,new Vector2(145,-75),new Vector2(620,100),19,new Color(.55f,.66f,.60f),"RADIUM JAW / THESIS FIG.1.2\nA clinical reference placed beside a reconstructed record.\nMarginal note: artistic interpretation, not quoted testimony.");
            var mask=Rect("Marker ink / hold and scrape",exposed,Vector2.zero,new Vector2(1020,320));
            coating=mask.gameObject.AddComponent<ClinicalCoating>();coating.Completed=Retain;
            stamp=Text("REDACTED marker label",mask,Vector2.zero,new Vector2(850,64),29,new Color(.58f,.62f,.58f),"REDACTED  /  ACCESS RESTRICTED");stamp.alignment=TextAlignmentOptions.Center;
            var dustRect=Rect("Ink dust",exposed,Vector2.zero,new Vector2(1020,320));var dust=dustRect.gameObject.AddComponent<ClinicalDust>();dust.raycastTarget=false;coating.dust=dust;
            hint=Text("Scrape instruction",parent,new Vector2(-70,-370),new Vector2(950,46),20,new Color(.70f,.77f,.72f),"HOLD LMB + DRAG / REMOVE THE INK     ·     HOLD [F] / ASSISTED REVEAL");
            pen=Rect("Camel hair isotope brush cursor",hud.safeArea,Vector2.zero,new Vector2(90,90));
            pen.localEulerAngles=new Vector3(0,0,-38);
            Image("Soft green nib glow",pen,new Vector2(0,-23),new Vector2(25,28),new Color(.3f,1,.45f,.06f));
            Image("Green nib halo",pen,new Vector2(0,-23),new Vector2(17,22),new Color(.3f,1,.45f,.10f));
            Image("Wooden brush shaft",pen,new Vector2(0,12),new Vector2(5,54),new Color(.44f,.28f,.13f));
            Image("Brass binding",pen,new Vector2(0,-12),new Vector2(7,12),new Color(.62f,.52f,.28f));
            Image("Camel hair nib",pen,new Vector2(0,-24),new Vector2(6,16),new Color(.43f,.95f,.54f));
            Image("Point",pen,new Vector2(0,-33),new Vector2(2,5),new Color(.69f,1,.69f));
            paper=Audio("Paper grain",true);bone=Audio("Bone friction",true);geiger=Audio("Geiger feedback",false);
        }
        void LateUpdate()
        {
            if(!Visible||hud==null||!hud.IsClinicalReading)return;
            hud.brushCursor.gameObject.SetActive(false);hud.focusMarker.gameObject.SetActive(false);
            hud.archivePicture.gameObject.SetActive(false);hud.archiveHint.gameObject.SetActive(false);
            hud.revealLabel.text=desk.Retained?"MEDICAL RECORD RETAINED   /   [E] OR [ESC] RETURN":"INK REMOVED  /  "+Mathf.RoundToInt(coating.Coverage*100)+"%";
            stamp.color=new Color(.67f,.70f,.64f,Mathf.Clamp01(1-coating.Coverage*8));
            bool drawing=coating.ScrapeEnergy>.02f&&Mouse.current!=null&&Mouse.current.leftButton.isPressed;
            if(drawing){paper.volume=.1f+coating.ScrapeEnergy*.2f;bone.volume=.04f+coating.ScrapeEnergy*.06f;if(!paper.isPlaying)paper.Play();if(!bone.isPlaying)bone.Play();if(Time.unscaledTime>tickAt){tickAt=Time.unscaledTime+Mathf.Lerp(.38f,.17f,coating.ScrapeEnergy);geiger.PlayOneShot(desk.geigerBurst,.09f);}}
            else{paper.Stop();bone.Stop();}
            if(desk.Retained){pen.gameObject.SetActive(false);Cursor.visible=true;hint.gameObject.SetActive(false);}
            else if(Mouse.current!=null)
            {
                Cursor.visible=false;
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(hud.safeArea,Mouse.current.position.ReadValue(),null,out var p))
                    pen.anchoredPosition=p+new Vector2(20,26);
            }
        }
        void Retain()
        {
            if(completing)return;completing=true;desk.Retain();hud.CompleteArchive();
            geiger.PlayOneShot(desk.geigerBurst,.20f);
        }
        public void Reveal(){if(coating!=null)coating.RevealAll();}
        public void Hide()
        {
            if(page==null)return;if(desk!=null)desk.EndInspection();page.gameObject.SetActive(false);pen.gameObject.SetActive(false);hint.gameObject.SetActive(false);
            paper.Stop();bone.Stop();geiger.Stop();
            hud.officialLayer.gameObject.SetActive(true);hud.truthLayer.gameObject.SetActive(true);hud.archiveHint.gameObject.SetActive(true);
            hud.sourceText.fontSize=originalSourceSize;
            var label=hud.archiveTitle.transform.parent.Find("Photograph label");if(label!=null)label.gameObject.SetActive(true);
        }
        AudioSource Audio(string name,bool loop){var a=new GameObject(name).AddComponent<AudioSource>();a.transform.SetParent(transform,false);a.playOnAwake=false;a.spatialBlend=0;a.loop=loop;a.volume=name.Contains("Bone")?.19f:.45f;return a;}
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 p,Vector2 size,Color c){var a=Rect(name,parent,p,size).gameObject.AddComponent<UnityEngine.UI.Image>();a.color=c;a.raycastTarget=false;return a;}
        TMP_Text Text(string name,Transform parent,Vector2 p,Vector2 size,float n,Color c,string s){var t=Rect(name,parent,p,size).gameObject.AddComponent<TextMeshProUGUI>();t.font=SurveyTypography.Instance!=null?SurveyTypography.Instance.bodyFont:hud.sourceText.font;t.text=s;t.fontSize=n;t.color=c;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;textSizes[t]=n;return t;}
    }
}
