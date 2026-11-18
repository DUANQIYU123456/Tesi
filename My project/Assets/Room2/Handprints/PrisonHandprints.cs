using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xiaoduan.Survey;

namespace Xiaoduan.PrisonTraces
{
    [DefaultExecutionOrder(350)]
    public sealed class PrisonHandprints:MonoBehaviour
    {
        public Renderer[] marks;
        public static PrisonHandprints Active {get;private set;}
        public float Visibility {get;private set;}
        public Q_Vignette_Single Vignette {get;private set;}
        readonly List<Graphic> hiddenGraphics=new List<Graphic>();
        MaterialPropertyBlock block;
        float[] opacity;
        float[] visibleOpacity;
        bool bound;
        static readonly int Opacity=Shader.PropertyToID("_Opacity");
        void Awake()
        {
            Active=this;block=new MaterialPropertyBlock();if(marks==null)marks=new Renderer[0];opacity=new float[marks.Length];visibleOpacity=new float[marks.Length];
            for(int i=0;i<marks.Length;i++)if(marks[i]!=null){opacity[i]=marks[i].sharedMaterial.GetFloat(Opacity);marks[i].enabled=false;}
        }
        void LateUpdate()
        {
            var hud=SurveyHUD.Instance;if(hud==null)return;
            if(!bound){BindVisor(hud);bound=true;}
            float previous=Visibility;Visibility=Mathf.MoveTowards(Visibility,hud.Vision?1:0,Time.unscaledDeltaTime*5);
            var eye=Camera.main;
            for(int i=0;i<marks.Length;i++)if(marks[i]!=null)
            {
                var delta=eye!=null?marks[i].bounds.center-eye.transform.position:Vector3.zero;
                float near=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(9,24,delta.magnitude));
                float gaze=eye!=null?Mathf.Lerp(.7f,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.85f,Vector3.Dot(eye.transform.forward,delta.normalized)))):1;
                visibleOpacity[i]=Mathf.MoveTowards(visibleOpacity[i],opacity[i]*Visibility*near*gaze,Time.unscaledDeltaTime*1.8f);
                marks[i].enabled=visibleOpacity[i]>.001f;marks[i].GetPropertyBlock(block);block.SetFloat(Opacity,visibleOpacity[i]);block.SetFloat("_Glow",2.4f);marks[i].SetPropertyBlock(block);
            }
        }
        void BindVisor(SurveyHUD hud)
        {
            var root=hud.visionFrame.transform;
            foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))if(graphic.enabled){hiddenGraphics.Add(graphic);graphic.enabled=false;}
            var original=Resources.Load<Q_Vignette_Single>("Room1SurveyQVignette");
            if(original==null){Debug.LogError("Missing shared ROOM1 isotope vignette",this);return;}
            Vignette=Instantiate(original,root,false);Vignette.name="ROOM2 / Q Vignette / same isotope green";
            Vignette.transform.SetAsFirstSibling();
            var frame=(RectTransform)Vignette.transform;frame.anchorMin=Vector2.zero;frame.anchorMax=Vector2.one;
            frame.offsetMin=-((RectTransform)root).offsetMin;frame.offsetMax=-((RectTransform)root).offsetMax;
            Vignette.CheckReferences();foreach(var corner in Vignette.cornerImages)corner.raycastTarget=false;
            Vignette.mainScale=.95f;Vignette.mainColor=new Color(.06f,1,.20f,.46f);Vignette.UpdateVignette();
        }
        void OnDestroy()
        {
            if(Active==this)Active=null;
            foreach(var graphic in hiddenGraphics)if(graphic!=null)graphic.enabled=true;
            if(Vignette!=null)Destroy(Vignette.gameObject);
        }
    }
}
