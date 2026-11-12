using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using TMPro;

namespace Xiaoduan.Room3
{
    [DefaultExecutionOrder(10000)]
    public sealed class Room3RecursiveArchive : MonoBehaviour
    {
        public Room3StoryFlow flow;
        public ResidualPortal[] portals;
        public ResidualPortal repetition,arrival,release;
        public Room3StoryNode ledger;
        public Renderer[] skins;
        public Transform[] skinTransforms;
        public TMP_Text passLabel,workLabel;
        public AudioSource watches,clicks;
        public int requiredPasses=2;
        [Range(256,1024)] public int viewWidth=768;
        public int Passes {get;private set;}
        public int Traversals {get;private set;}
        public int RenderedViews {get;private set;}
        public bool RecordDecoded {get;private set;}
        public bool Inside {get;private set;}
        public bool RenderingFailed {get;private set;}
        public Vector3 GuidePosition => Passes<requiredPasses?repetition.transform.position:ledger.focus.position;
        public string Guidance => Passes<requiredPasses?"REPEATED THRESHOLD":"LABOR LEDGER";
        public string Objective => Passes<requiredPasses?"Follow the repeated threshold. Observe what changes.":
            !RecordDecoded?"Read the labor ledger beside the workstation. Hold F to retain it.":"The route is released. Continue through the far threshold.";
        Transform player;
        Camera eye,viewCamera;
        Vector3 previous;
        float nextFind,nextRender,decay;
        bool hasPrevious,rendering;
        int budget;
        readonly Dictionary<int,RenderTexture> textures=new Dictionary<int,RenderTexture>();
        MaterialPropertyBlock properties;
        Vector3[] origins;
        UniversalRenderPipeline.SingleCameraRequest request=new UniversalRenderPipeline.SingleCameraRequest();
        void Awake()
        {
            properties=new MaterialPropertyBlock();origins=new Vector3[skinTransforms.Length];
            for(int i=0;i<origins.Length;i++)origins[i]=skinTransforms[i].localPosition;
            var cameraObject=new GameObject("Archive portal view / offscreen only");cameraObject.transform.SetParent(transform,false);
            viewCamera=cameraObject.AddComponent<Camera>();viewCamera.enabled=false;viewCamera.allowHDR=true;
            viewCamera.clearFlags=CameraClearFlags.SolidColor;viewCamera.backgroundColor=new Color(.025f,.039f,.043f);
            viewCamera.nearClipPlane=.08f;viewCamera.farClipPlane=65;viewCamera.useOcclusionCulling=false;
            var data=viewCamera.GetUniversalAdditionalCameraData();data.SetRenderer(0);
            data.renderPostProcessing=false;data.renderShadows=false;data.volumeLayerMask=0;
            UpdateLabels();
        }
        void Update()
        {
            if(Time.unscaledTime>=nextFind||player==null||eye==null)
            {
                nextFind=Time.unscaledTime+.5f;var go=GameObject.FindGameObjectWithTag("Player");
                player=go!=null?go.transform:null;eye=Camera.main;
            }
            if(player==null||eye==null||flow.EndingChosen)return;
            Inside=player.position.x>75&&player.position.x<85&&player.position.z> -23&&player.position.z<3;
            if(!hasPrevious){previous=player.position;hasPrevious=true;}
            if(Time.timeScale<=0||Xiaoduan.Survey.SurveyHUD.BlocksWorld||flow.Reading||flow.ui.DecisionVisible)
            {previous=player.position;return;}
            if((player.position-previous).sqrMagnitude<25)
                foreach(var portal in portals)
                    if(portal.Crossed(previous,player.position)){Traverse(portal);break;}
            previous=player.position;
            float target=RecordDecoded?.74f:Mathf.Min(.64f,Passes*.30f);
            decay=Mathf.MoveTowards(decay,target,Time.deltaTime*.4f);
            properties.SetFloat("_Erosion",decay);
            foreach(var skin in skins)skin.SetPropertyBlock(properties);
            for(int i=0;i<skinTransforms.Length;i++)
            {
                float sign=origins[i].x<80?-1:1;
                skinTransforms[i].localPosition=origins[i]+Vector3.right*sign*decay*.16f;
                skinTransforms[i].localRotation=Quaternion.Euler(0,0,sign*decay*(i%2==0?1.3f:-.8f));
            }
            watches.volume=Mathf.MoveTowards(watches.volume,Inside?Mathf.Lerp(.14f,.045f,Passes/2f):0,Time.deltaTime*.3f);
            clicks.volume=Mathf.MoveTowards(clicks.volume,Inside?Mathf.Lerp(.025f,.13f,Passes/2f):0,Time.deltaTime*.3f);
        }
        void Traverse(ResidualPortal portal)
        {
            Matrix4x4 mapped=portal.Mapping*player.localToWorldMatrix;
            Vector3 old=player.position;var cc=player.GetComponent<CharacterController>();bool active=cc!=null&&cc.enabled;
            if(active)cc.enabled=false;
            player.SetPositionAndRotation(mapped.GetColumn(3),mapped.rotation);
            Physics.SyncTransforms();if(active)cc.enabled=true;
            foreach(var camera in FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsSortMode.None))
            {
                if(camera.Follow!=null&&(camera.Follow==player||camera.Follow.IsChildOf(player)))
                {camera.OnTargetObjectWarped(camera.Follow,player.position-old);camera.PreviousStateIsValid=false;}
            }
            Traversals++;
            Inside=player.position.x>75&&player.position.x<85;
            if(portal==repetition&&!RecordDecoded)
            {
                Passes=Mathf.Min(requiredPasses,Passes+1);UpdateLabels();
                flow.ui.Notice(Passes<requiredPasses?"ROUTE REPEATED / OUTPUT UNCHANGED":"DEVIATION RETAINED / READ THE WORKSTATION LEDGER",3.4f);
                flow.effectsSource.PlayOneShot(flow.revealSound,.10f);
            }
            else if(portal.purpose==ResidualPortal.Purpose.Entrance&&!RecordDecoded)
                flow.ui.Notice("ARCHIVE ROUTE / REPETITION AUTHORIZED",2.5f);
        }
        public bool CanRead => Passes>=requiredPasses;
        public void RetainRecord()
        {
            if(!CanRead||RecordDecoded)return;
            RecordDecoded=true;repetition.linked=release;UpdateLabels();
            flow.ui.Notice("LABOR RECORD RETAINED / ROUTE RELEASED",4);
        }
        void UpdateLabels()
        {
            passLabel.text=RecordDecoded?"ROUTE RELEASED\nRECORD RETAINED":"REPETITION "+Passes.ToString("00")+"\nOUTPUT UNCHANGED";
            workLabel.text=RecordDecoded?"LABOR IS PRESENT\nIN THE SYSTEM":CanRead?"[E] LABOR LEDGER\nREAD THE OTHER LAYER":"WORKSTATION\nOBSERVE THE REPETITION";
        }
        void LateUpdate()
        {
            if(eye==null||RenderingFailed||rendering||flow.EndingChosen||Time.timeScale<=0||flow.Reading||Xiaoduan.Survey.SurveyHUD.BlocksWorld)return;
            foreach(var portal in portals)portal.ProtectNearPlane(eye);
            if(Time.unscaledTime<nextRender)return;nextRender=Time.unscaledTime+1f/30;
            rendering=true;budget=4;
            try
            {
                foreach(var portal in portals)
                {
                    if(budget<=0)break;
                    if(portal.Visible(eye))portal.SetView(RenderView(portal,eye.transform.localToWorldMatrix,0));
                }
            }
            catch(Exception e){RenderingFailed=true;Debug.LogError("Residual portal rendering stopped: "+e.Message,this);}
            finally{rendering=false;viewCamera.targetTexture=null;}
        }
        RenderTexture TextureFor(ResidualPortal portal,int depth)
        {
            int key=portal.GetInstanceID()*3+depth;
            int width=Mathf.Clamp(Mathf.RoundToInt(Screen.width*.67f),256,viewWidth);
            int height=Mathf.Max(128,Mathf.RoundToInt(width/eye.aspect));
            if(textures.TryGetValue(key,out var old)&&old.width==width&&old.height==height)return old;
            if(old!=null){old.Release();Destroy(old);}
            var rt=new RenderTexture(width,height,24,RenderTextureFormat.DefaultHDR){name="Archive portal / "+portal.name+" / "+depth,antiAliasing=1};
            rt.Create();textures[key]=rt;return rt;
        }
        void ConfigureView(ResidualPortal portal,Matrix4x4 pose)
        {
            viewCamera.transform.SetPositionAndRotation(pose.GetColumn(3),pose.rotation);
            viewCamera.fieldOfView=eye.fieldOfView;viewCamera.aspect=eye.aspect;
            viewCamera.projectionMatrix=Matrix4x4.Perspective(eye.fieldOfView,eye.aspect,.08f,65);
            var plane=portal.linked.transform;
            float sign=Mathf.Sign(Vector3.Dot(plane.forward,plane.position-viewCamera.transform.position));
            Vector3 position=viewCamera.worldToCameraMatrix.MultiplyPoint(plane.position);
            Vector3 normal=viewCamera.worldToCameraMatrix.MultiplyVector(plane.forward)*sign;
            float distance=-Vector3.Dot(position,normal)+.025f;
            if(Mathf.Abs(distance)>.16f)viewCamera.projectionMatrix=viewCamera.CalculateObliqueMatrix(new Vector4(normal.x,normal.y,normal.z,distance));
        }
        RenderTexture RenderView(ResidualPortal portal,Matrix4x4 observer,int depth)
        {
            var pose=portal.Mapping*observer;var saved=new RenderTexture[portals.Length];
            for(int i=0;i<portals.Length;i++)saved[i]=portals[i].View;
            ConfigureView(portal,pose);
            if(depth<1&&budget>1)
            {
                foreach(var next in portals)
                {
                    if(next==portal.linked||budget<=1||!next.Visible(viewCamera))continue;
                    next.SetView(RenderView(next,pose,depth+1));ConfigureView(portal,pose);
                }
            }
            else foreach(var p in portals)p.SetView(null);
            var texture=TextureFor(portal,depth);ConfigureView(portal,pose);
            request.destination=texture;
            if(!RenderPipeline.SupportsRenderRequest(viewCamera,request))throw new NotSupportedException("URP SingleCameraRequest is unavailable");
            // The exit's physical screen must not cover the view with its back face.
            bool exitEnabled=portal.linked.screen.enabled;portal.linked.screen.enabled=false;
            try{RenderPipeline.SubmitRenderRequest(viewCamera,request);RenderedViews++;budget--;}
            finally{portal.linked.screen.enabled=exitEnabled;}
            for(int i=0;i<portals.Length;i++)portals[i].SetView(saved[i]);
            return texture;
        }
        void OnDestroy()
        {
            foreach(var texture in textures.Values)if(texture!=null){texture.Release();Destroy(texture);}
            textures.Clear();
        }
    }
}
