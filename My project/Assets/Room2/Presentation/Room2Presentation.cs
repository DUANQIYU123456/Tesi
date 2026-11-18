using UnityEngine;
using Xiaoduan.Survey;
using Xiaoduan.Panopticon;

namespace Xiaoduan.Room2
{
    // Scene-local direction of the scan, evidence receipts and released threshold.
    [DefaultExecutionOrder(450)]
    public sealed class Room2Presentation : MonoBehaviour
    {
        public PanopticonHall hall;
        public Light thresholdLight;
        public Renderer thresholdSurface;
        public GameObject thresholdPortal;
        float releaseAge=-1;
        MaterialPropertyBlock block;
        void Awake(){block=new MaterialPropertyBlock();releaseAge=-1;}
        public void ReleaseArchive(){releaseAge=0;}
        void LateUpdate()
        {
            if(hall==null)return;
            if(block==null)block=new MaterialPropertyBlock();
            if(thresholdPortal!=null)
            {
                thresholdPortal.SetActive(hall.GateReleased);
                if(hall.GateReleased){var surface=thresholdPortal.GetComponent<Renderer>();if(surface!=null){surface.GetPropertyBlock(block);block.SetFloat("_speed",SurveyProgress.ReducedMotion?0:surface.sharedMaterial.GetFloat("_speed"));surface.SetPropertyBlock(block);}}
            }
            Shader.SetGlobalFloat("_Room2ProjectionMotion",SurveyProgress.ReducedMotion?0:1);
            bool blocked=SurveyHUD.BlocksWorld;
            var scan=hall.scanner;
            if(scan!=null&&scan.scanAudio!=null)
            {
                float phase=Shader.GetGlobalFloat("_XDScanTravel")/Mathf.Max(.1f,scan.scanDistance+scan.scanTrailLength);
                float envelope=Mathf.SmoothStep(0,1,phase/.045f)*(1-Mathf.SmoothStep(.87f,1,phase));
                var eye=Camera.main;float proximity=0;
                if(eye!=null){float coordinate=hall.Coordinate(eye.transform.position);float distance=Mathf.Abs(coordinate-Shader.GetGlobalFloat("_XDScanTravel"));proximity=1-Mathf.Clamp01(distance/8);}
                float gain=SurveyHUD.Instance!=null&&SurveyHUD.Instance.SoundFocusActive?.32f:1;
                if(SurveyHUD.Instance!=null&&SurveyHUD.Instance.IsReading)gain*=.35f;
                scan.scanAudio.volume=scan.IsScanning?(.13f+.055f*proximity)*envelope*gain:0;
            }
            if(releaseAge>=0&&!blocked)releaseAge+=Time.deltaTime;
            float reveal=releaseAge<0?0:Mathf.SmoothStep(0,1,Mathf.Clamp01((releaseAge-.3f)/1.7f));
            if(thresholdLight!=null)thresholdLight.intensity=reveal*13;
            if(thresholdSurface!=null){block.SetColor("_EmissionColor",new Color(.45f,.58f,.61f)*reveal*1.3f);thresholdSurface.SetPropertyBlock(block);}
        }
        void OnDisable(){Shader.SetGlobalFloat("_Room2ProjectionMotion",0);}
    }
}
