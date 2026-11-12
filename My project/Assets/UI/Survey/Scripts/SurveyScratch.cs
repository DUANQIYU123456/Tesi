using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Xiaoduan.Survey
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SurveyScratch : MaskableGraphic, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const int Columns=48,Rows=22;
        readonly bool[] erased=new bool[Columns*Rows];
        public int Strokes {get;private set;}
        public float Coverage {get;private set;}
        Vector2 last;bool dragging;float path;
        public System.Action Completed;
        bool done;
        public void ResetSurface(){System.Array.Clear(erased,0,erased.Length);Strokes=0;Coverage=0;dragging=done=false;SetVerticesDirty();}
        public void OnPointerDown(PointerEventData e){if(done)return;dragging=true;path=0;last=e.position;Erase(e.position,e.pressEventCamera);}
        public void OnDrag(PointerEventData e)
        {
            if(!dragging||done)return;float distance=Vector2.Distance(last,e.position);path+=distance;
            int steps=Mathf.Clamp(Mathf.CeilToInt(distance/8),1,100);
            for(int i=1;i<=steps;i++)Erase(Vector2.Lerp(last,e.position,(float)i/steps),e.pressEventCamera);
            last=e.position;
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(!dragging)return;dragging=false;if(path>45)Strokes++;
            if(Strokes>=3&&Coverage>.22f&&!done){done=true;Completed?.Invoke();}
        }
        void Erase(Vector2 screen,Camera camera)
        {
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screen,camera,out var p))return;
            var r=rectTransform.rect;int n=0;
            for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++)
            {
                var center=new Vector2(r.xMin+(x+.5f)*r.width/Columns,r.yMin+(y+.5f)*r.height/Rows);
                if(Vector2.Distance(center,p)<34)erased[y*Columns+x]=true;
                if(erased[y*Columns+x])n++;
            }
            Coverage=(float)n/erased.Length;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();var r=rectTransform.rect;
            for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++)
            {
                if(erased[y*Columns+x])continue;float a=r.xMin+x*r.width/Columns,b=r.yMin+y*r.height/Rows;
                float w=r.width/Columns+.2f,t=r.height/Rows+.2f;int k=h.currentVertCount;
                h.AddVert(new Vector3(a,b),color,Vector2.zero);h.AddVert(new Vector3(a,b+t),color,Vector2.zero);
                h.AddVert(new Vector3(a+w,b+t),color,Vector2.zero);h.AddVert(new Vector3(a+w,b),color,Vector2.zero);
                h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);
            }
        }
    }
}
