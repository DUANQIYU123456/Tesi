using UnityEngine;
using TMPro;
using Xiaoduan.Survey;
namespace Xiaoduan.Room3
{
    // The screen belongs to the console; it should not become a distant floating label.
    public sealed class ArchiveConsoleSurface:MonoBehaviour
    {
        public TMP_Text label;
        public Room3StoryFlow flow;
        Camera eye;
        Color ink;
        float visibility,findAt;
        void Awake(){ink=label.color;var c=ink;c.a=0;label.color=c;}
        void LateUpdate()
        {
            if(eye==null||Time.unscaledTime>findAt){eye=Camera.main;findAt=Time.unscaledTime+.5f;}
            if(eye==null||label==null)return;
            bool blocked=SurveyHUD.BlocksWorld||(flow!=null&&(flow.Reading||flow.ui.DecisionVisible||flow.EndingChosen));
            float target=blocked?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,3.3f,Vector3.Distance(eye.transform.position,transform.position)));
            visibility=Mathf.MoveTowards(visibility,target,Time.unscaledDeltaTime*5);
            var c=ink;c.a=ink.a*visibility;label.color=c;
        }
    }
}
