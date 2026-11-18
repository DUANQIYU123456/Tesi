using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Xiaoduan.Clinical
{
    public sealed class ClinicalCoating : RawImage,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        const int W=320,H=180;
        Texture2D mask;
        Color32[] original,pixels;
        Vector2 previous;
        bool dragging,finishing,finished;
        float finishFade=1,uploadAt;
        public float Coverage {get;private set;}
        public bool Scraping {get;private set;}
        public float ScrapeEnergy {get;private set;}
        public System.Action Completed;
        public ClinicalDust dust;
        protected override void Awake(){base.Awake();}
        public void ResetCoating()
        {
            if(mask==null){mask=new Texture2D(W,H,TextureFormat.RGBA32,false);mask.filterMode=FilterMode.Bilinear;mask.wrapMode=TextureWrapMode.Clamp;}
            original=new Color32[W*H];pixels=new Color32[W*H];
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                float edge=Mathf.Min(x,W-1-x,y,H-1-y);
                byte a=(byte)(edge<2?Mathf.Clamp(edge/2,0,1)*255:255);
                float grain=Mathf.PerlinNoise(x*.19f,y*.8f);
                byte ink=(byte)(7+grain*8+Mathf.PerlinNoise(x*.03f,y*.15f)*6);
                if(edge<5)a=(byte)(a*Mathf.Lerp(.35f,1,grain));
                original[y*W+x]=pixels[y*W+x]=new Color32(ink,(byte)(ink+2),ink,a);
            }
            texture=mask;color=Color.white;Coverage=0;dragging=finishing=finished=Scraping=false;finishFade=1;
            mask.SetPixels32(pixels);mask.Apply(false);raycastTarget=true;
        }
        public void OnPointerDown(PointerEventData e){if(finished||finishing)return;dragging=true;previous=e.position;Erase(e.position,e.pressEventCamera);}
        public void OnDrag(PointerEventData e)
        {
            if(!dragging||finished||finishing)return;
            int steps=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(previous,e.position)/6),1,100);
            for(int i=1;i<=steps;i++)Erase(Vector2.Lerp(previous,e.position,(float)i/steps),e.pressEventCamera);
            previous=e.position;
        }
        public void OnPointerUp(PointerEventData e){dragging=Scraping=false;Upload();}
        void Erase(Vector2 screen,Camera camera)
        {
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screen,camera,out var p))return;
            var r=rectTransform.rect;if(!r.Contains(p))return;
            int cx=Mathf.RoundToInt((p.x-r.xMin)/r.width*W),cy=Mathf.RoundToInt((p.y-r.yMin)/r.height*H);
            float rx=22/r.width*W,ry=22/r.height*H;int removed=0;
            for(int y=Mathf.Max(0,(int)(cy-ry-1));y<Mathf.Min(H,cy+ry+2);y++)for(int x=Mathf.Max(0,(int)(cx-rx-1));x<Mathf.Min(W,cx+rx+2);x++)
            {
                float distance=Mathf.Sqrt(Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2));
                if(distance<1){int k=y*W+x;byte before=pixels[k].a;pixels[k].a=(byte)Mathf.Min(pixels[k].a,Mathf.SmoothStep(0,255,Mathf.InverseLerp(.55f+Mathf.PerlinNoise(x*.6f,y*.6f)*.15f,1,distance)));removed+=before-pixels[k].a;}
            }
            Scraping=removed>0;if(removed>0){ScrapeEnergy=Mathf.Clamp01(ScrapeEnergy+removed/22000f);dust?.Puff(p);}
            if(Time.unscaledTime>=uploadAt){Upload();uploadAt=Time.unscaledTime+.025f;}
        }
        void Upload()
        {
            if(mask==null||finished)return;int n=0;
            foreach(var pixel in pixels)if(pixel.a<100)n++;
            Coverage=(float)n/pixels.Length;mask.SetPixels32(pixels);mask.Apply(false);
            if(Coverage>=.50f&&!finishing&&!finished)finishing=true;
        }
        public void RevealAll(){if(finished)return;finishing=true;}
        void Update()
        {
            ScrapeEnergy=Mathf.MoveTowards(ScrapeEnergy,0,Time.unscaledDeltaTime*2.8f);
            if(dragging&&Mouse.current!=null&&!Mouse.current.leftButton.isPressed){dragging=Scraping=false;Upload();}
            if(finishing)
            {
                dragging=Scraping=false;finishFade=Mathf.MoveTowards(finishFade,0,Time.unscaledDeltaTime*1.1f);color=new Color(1,1,1,finishFade);
                if(finishFade<=0){finished=true;finishing=false;Coverage=1;raycastTarget=false;Completed?.Invoke();}
            }
        }
        protected override void OnDisable(){dragging=Scraping=false;ScrapeEnergy=0;Upload();base.OnDisable();}
        protected override void OnDestroy(){if(mask!=null)Destroy(mask);base.OnDestroy();}
    }
    public sealed class ClinicalDust : MaskableGraphic
    {
        struct Mote {public Vector2 p,v;public float life,size;}
        readonly Mote[] motes=new Mote[100];int next;
        public void Puff(Vector2 p)
        {
            for(int n=0;n<2;n++){motes[next]=new Mote{p=p+Random.insideUnitCircle*7,v=new Vector2(Random.Range(-15f,15f),Random.Range(9f,32f)),life=.7f,size=Random.Range(.8f,2f)};next=(next+1)%motes.Length;}
            SetVerticesDirty();
        }
        void Update(){bool active=false;for(int i=0;i<motes.Length;i++)if(motes[i].life>0){var m=motes[i];m.life-=Time.unscaledDeltaTime;m.p+=m.v*Time.unscaledDeltaTime;motes[i]=m;active=true;}if(active)SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper h)
        {
            h.Clear();foreach(var m in motes)if(m.life>0){int k=h.currentVertCount;var c=new Color(.18f,.20f,.17f,Mathf.Clamp01(m.life/.7f)*.65f);h.AddVert(m.p+Vector2.left*m.size*.4f,c,Vector2.zero);h.AddVert(m.p+Vector2.up*m.size,c,Vector2.zero);h.AddVert(m.p+Vector2.right*m.size*.4f,c,Vector2.zero);h.AddVert(m.p-Vector2.up*m.size*.6f,c,Vector2.zero);h.AddTriangle(k,k+1,k+2);h.AddTriangle(k,k+2,k+3);}
        }
    }
}
