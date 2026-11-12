using UnityEngine;

namespace Xiaoduan.Survey
{
    public sealed class SurveyTrace : MonoBehaviour
    {
        Renderer[] surfaces;
        void Awake(){surfaces=GetComponentsInChildren<Renderer>(true);}
        void Update(){bool visible=SurveyHUD.Instance!=null&&SurveyHUD.Instance.Vision;foreach(var surface in surfaces)if(surface!=null)surface.enabled=visible;}
    }
}
