using UnityEngine;
using Xiaoduan.Survey;
namespace Xiaoduan.Room3
{
    [DefaultExecutionOrder(470)]
    public sealed class WorkGestureProps:MonoBehaviour
    {
        public Transform hand,index,brush,dial;
        void LateUpdate()
        {
            if(hand==null||index==null||brush==null||dial==null)return;
            var grip=Vector3.Lerp(hand.position,index.position,.6f)+transform.up*.03f;
            brush.position=grip;
            var direction=dial.position+transform.up*.012f-grip;
            brush.localScale=new Vector3(1,1,Mathf.Clamp(direction.magnitude/.111f,.75f,1.65f));
            if(direction.sqrMagnitude>.00001f)brush.rotation=Quaternion.LookRotation(direction,transform.up);
        }
    }
}
