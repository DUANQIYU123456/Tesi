using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Xiaoduan.Survey
{
    public sealed class SurveyButton:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISelectHandler,IDeselectHandler
    {
        public Image image;
        public Color idle=new Color(.65f,.7f,.65f),hover=new Color(.82f,.9f,.79f);
        public Sprite idleSprite,hoverSprite,pressedSprite;
        public bool ending,preserve;
        bool over,selected,pressed;
        TMP_Text caption;
        Color captionColor;
        Sprite originalSprite;
        void Awake(){if(image!=null)originalSprite=image.sprite;caption=GetComponentInChildren<TMP_Text>();if(caption!=null)captionColor=caption.color;}
        void Visual()
        {
            if(image==null)return;
            image.sprite=pressed&&pressedSprite!=null?pressedSprite:(over||selected)&&hoverSprite!=null?hoverSprite:idleSprite!=null?idleSprite:originalSprite;
            image.color=pressed?new Color(.84f,.89f,.79f):over||selected?hover:idle;
            if(caption!=null)caption.color=pressed&&pressedSprite!=null?new Color(.025f,.045f,.032f):captionColor;
        }
        public void OnPointerEnter(PointerEventData _){over=true;Visual();SurveyHUD.Instance?.ButtonFeedback(false);if(ending)SurveyHUD.Instance?.PreviewEnding(preserve?1:0);}
        public void OnPointerExit(PointerEventData _){over=false;Visual();if(ending&&!selected)SurveyHUD.Instance?.PreviewEnding(-1);}
        public void OnPointerDown(PointerEventData _){if(pressed)return;pressed=true;Visual();SurveyHUD.Instance?.ButtonFeedback(true);}
        public void OnPointerUp(PointerEventData _){pressed=false;Visual();}
        public void OnSelect(BaseEventData _){selected=true;Visual();if(ending)SurveyHUD.Instance?.PreviewEnding(preserve?1:0);}
        public void OnDeselect(BaseEventData _){selected=false;Visual();if(ending&&!over)SurveyHUD.Instance?.PreviewEnding(-1);}
        void OnEnable(){Visual();}
        void OnDisable(){pressed=over=selected=false;Visual();if(ending)SurveyHUD.Instance?.PreviewEnding(-1);}
    }
}
