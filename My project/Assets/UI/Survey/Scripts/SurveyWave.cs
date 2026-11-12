using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Xiaoduan.Survey
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SurveyWave : MaskableGraphic
    {
        public float energy;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=rectTransform.rect;int count=110;
            float phase=Application.isPlaying?Time.unscaledTime:0;
            for(int i=0;i<count;i++)
            {
                float x=rect.xMin+rect.width*i/count;
                float y=Mathf.Sin(i*.57f+phase*11)*Mathf.Sin(i*.13f-phase*3)*rect.height*.42f*energy;
                int start=helper.currentVertCount;float h=1.1f;
                helper.AddVert(new Vector3(x,y-h),color,Vector2.zero);helper.AddVert(new Vector3(x,y+h),color,Vector2.zero);
                float nx=rect.xMin+rect.width*(i+1)/count;
                float ny=Mathf.Sin((i+1)*.57f+phase*11)*Mathf.Sin((i+1)*.13f-phase*3)*rect.height*.42f*energy;
                helper.AddVert(new Vector3(nx,ny+h),color,Vector2.zero);helper.AddVert(new Vector3(nx,ny-h),color,Vector2.zero);
                helper.AddTriangle(start,start+1,start+2);helper.AddTriangle(start,start+2,start+3);
            }
        }
        float nextRedraw;
        CanvasGroup group;
        protected override void Awake(){base.Awake();group=GetComponentInParent<CanvasGroup>();}
        void Update(){if((group==null||group.alpha>.01f)&&Time.unscaledTime>=nextRedraw){nextRedraw=Time.unscaledTime+1f/30;SetVerticesDirty();}}
    }
}
