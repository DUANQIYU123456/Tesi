using UnityEngine;

namespace Xiaoduan.Survey
{
    public enum SurveyNodeKind { Bottle, Watches, Stamping, Medical, Portal, Court, TownVanity, Advertisement }
    public sealed class SurveyNode : MonoBehaviour
    {
        public SurveyNodeKind kind;
        public Transform focus;
        public float reach=5;
        public int Fragment => (kind==SurveyNodeKind.Bottle||kind==SurveyNodeKind.TownVanity||kind==SurveyNodeKind.Advertisement)?0:kind==SurveyNodeKind.Medical?2:kind==SurveyNodeKind.Court?3:1;
        public Vector3 Position => focus!=null?focus.position:transform.position;
        public bool Complete => kind==SurveyNodeKind.Portal?false:kind==SurveyNodeKind.Watches?SurveyProgress.LeftTuned:kind==SurveyNodeKind.Stamping?SurveyProgress.RightTuned:SurveyProgress.Has(Fragment);
    }


}
