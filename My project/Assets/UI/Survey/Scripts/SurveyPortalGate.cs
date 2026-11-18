using UnityEngine;

namespace Xiaoduan.Survey
{
    public sealed class SurveyPortalGate : MonoBehaviour
    {
        public TransitionPoint portal;
        void Update()
        {
            // The two audible cells are beyond the entrance gate. Gate the exit,
            // so the player can reach both sources before being asked to decode them.
            if(portal!=null)portal.enabled=(Xiaoduan.Panopticon.PanopticonHall.Active!=null?Xiaoduan.Panopticon.PanopticonHall.Active.GateReleased:SurveyProgress.Has(0)&&SurveyProgress.Has(1)&&SurveyProgress.Has(2))&&!SurveyProgress.Modal;
        }
    }
}
