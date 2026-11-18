using UnityEngine;
using UnityEngine.Rendering.Universal;
using Xiaoduan.Survey;
namespace Xiaoduan.Room3
{
    [DefaultExecutionOrder(550)]
    public sealed class Room3ArchiveDistortion:MonoBehaviour
    {
        public Room3StoryFlow flow;
        public Room3MemoryFragment[] fragments;
        public Renderer[] memoryVeils;
        public Renderer[] archiveVeils;
        public Renderer eraseField;
        public Light eraseRim;
        public Vefects.Distortion.VefectsDistortionFeature feature;
        public float MemoryStrength {get;private set;}
        public float EraseStrength {get;private set;}
        public int MemoryBursts {get;private set;}
        Camera eye;
        bool[] retained;
        float[] echo;
        float previewAge,commitAge=-1;
        bool wasFormat,wasEnding;
        MaterialPropertyBlock props;
        void Awake(){retained=new bool[fragments.Length];echo=new float[fragments.Length];for(int i=0;i<retained.Length;i++)retained[i]=fragments[i].Retained;props=new MaterialPropertyBlock();}
        void LateUpdate()
        {
            if(eye==null){eye=Camera.main;if(eye!=null){var d=eye.GetUniversalAdditionalCameraData();d.requiresDepthTexture=true;d.requiresColorTexture=true;}}
            if(eye==null)return;
            bool reduced=flow.ReducedMotion;
            bool blocked=SurveyHUD.BlocksWorld||flow.Reading;
            bool format=flow.ui.DecisionVisible&&flow.embodied.Preview==0;
            float dt=blocked?0:Time.deltaTime;
            if(format&&!wasFormat)previewAge=0;if(format)previewAge+=dt;else previewAge=0;wasFormat=format;
            if(flow.EndingChosen&&!wasEnding&&flow.Ending=="format")commitAge=0;wasEnding=flow.EndingChosen;
            if(commitAge>=0)commitAge+=Time.unscaledDeltaTime;
            bool committing=commitAge>=0&&commitAge<.58f;
            MemoryStrength=0;
            for(int i=0;i<fragments.Length;i++){
                var f=fragments[i];if(f.Retained&&!retained[i]){echo[i]=.7f;MemoryBursts++;}retained[i]=f.Retained;
                echo[i]=Mathf.Max(0,echo[i]-dt);
                float capture=!f.Retained&&f.Progress>0?Mathf.Sin(f.Progress*Mathf.PI)*.012f:0;
                float amount=capture+Mathf.Sin(Mathf.Clamp01(echo[i]/.7f)*Mathf.PI)*.012f;
                if(reduced||blocked||flow.ui.DecisionVisible||flow.EndingChosen)amount=0;
                MemoryStrength=Mathf.Max(MemoryStrength,amount);
                var veil=memoryVeils[i];var toward=(eye.transform.position-f.focus.position).normalized;
                veil.transform.position=f.focus.position+toward*.50f;veil.transform.rotation=Quaternion.LookRotation(veil.transform.position-eye.transform.position);
                Strength(veil,amount);
            }
            float hover=format?(.006f+Mathf.Sin(Mathf.Min(previewAge,1.4f)/1.4f*Mathf.PI)*.012f):0;
            float commit=committing?Mathf.Sin(Mathf.Clamp01(commitAge/.58f)*Mathf.PI)*.026f:0;
            EraseStrength=reduced||blocked?0:Mathf.Max(hover,commit);
            foreach(var r in archiveVeils)Strength(r,EraseStrength);
            // A restrained cold afterimage gives the erased archive a visible surface to fracture.
            float cold=blocked?0:format?.24f:committing?(1-commitAge/.58f)*.28f:0;
            props.Clear();props.SetFloat("_Strength",cold);props.SetColor("_Bone",new Color(.23f,.28f,.29f));props.SetColor("_Gold",new Color(.035f,.05f,.06f));eraseField.SetPropertyBlock(props);eraseField.enabled=cold>.001f;
            eraseRim.intensity=cold*190;
            feature.SetActive(MemoryStrength>.0001f||EraseStrength>.0001f);
        }
        void Strength(Renderer r,float value){r.enabled=value>.0001f;props.Clear();props.SetFloat("_DistortionOverallIntensity",value);r.SetPropertyBlock(props);}
        void OnDisable(){foreach(var r in memoryVeils)if(r!=null)r.enabled=false;foreach(var r in archiveVeils)if(r!=null)r.enabled=false;if(eraseField!=null)eraseField.enabled=false;if(eraseRim!=null)eraseRim.intensity=0;if(feature!=null)feature.SetActive(false);Shader.SetGlobalFloat("_VefectsDistortionLate",0);}
    }
}
