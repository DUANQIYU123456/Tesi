using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Xiaoduan.Survey
{
    public sealed class SurveyButton:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public Image image;
        public Color idle=new Color(.43f,.53f,.47f),hover=new Color(.43f,.95f,.63f);
        public bool ending,preserve;
        bool over,pressed,positionCaptured;
        Sprite originalSprite;Material originalMaterial;
        TMP_Text caption;Color captionColor;
        Vector2 origin;float glitchUntil;
        public void OnPointerEnter(PointerEventData _)
        {
            over=true;image.color=hover;origin=image.rectTransform.anchoredPosition;positionCaptured=true;
            glitchUntil=Time.unscaledTime+.12f;SurveyHUD.Instance?.ButtonFeedback(false);
            if(ending)SurveyHUD.Instance?.PreviewEnding(preserve?1:0);
        }
        public void OnPointerExit(PointerEventData _){over=false;image.color=idle;ResetPosition();if(ending)SurveyHUD.Instance?.PreviewEnding(-1);}
        public void OnPointerDown(PointerEventData _)
        {
            if(pressed)return;pressed=true;originalSprite=image.sprite;originalMaterial=image.material;
            image.sprite=null;image.material=null;image.color=new Color(.24f,.77f,.43f);
            caption=GetComponentInChildren<TMP_Text>();if(caption!=null){captionColor=caption.color;caption.color=new Color(.015f,.06f,.031f);}
            ResetPosition();SurveyHUD.Instance?.ButtonFeedback(true);
        }
        public void OnPointerUp(PointerEventData _){Restore();image.color=over?hover:idle;}
        void Update()
        {
            if(!positionCaptured)return;
            if(over&&!pressed&&!SurveyProgress.ReducedMotion&&Time.unscaledTime<glitchUntil)
                image.rectTransform.anchoredPosition=origin+Vector2.right*(Mathf.Sin(Time.unscaledTime*87)*1.25f);
            else image.rectTransform.anchoredPosition=origin;
        }
        void ResetPosition(){if(positionCaptured)image.rectTransform.anchoredPosition=origin;positionCaptured=false;}
        void Restore(){if(!pressed)return;pressed=false;image.sprite=originalSprite;image.material=originalMaterial;if(caption!=null)caption.color=captionColor;}
        void OnDisable(){Restore();ResetPosition();if(ending)SurveyHUD.Instance?.PreviewEnding(-1);}
    }
}
