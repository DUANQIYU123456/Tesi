using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Xiaoduan.Survey;

namespace Xiaoduan.Clinical
{
    public sealed class ClinicalDesk : MonoBehaviour
    {
        public static ClinicalDesk Active {get;private set;}
        public Transform entrance,table;
        public Transform[] bottles;
        public Light candle,keyLight;
        public Texture clinicalImage;
        public AudioClip paperScratch,boneFriction,geigerBurst;
        public bool Entered {get;private set;}
        public bool Inspecting {get;private set;}
        public bool Retained {get;private set;}
        public float Lift {get;private set;}
        Vector3[] resting;
        Renderer[][] liquids;
        Light[][] specimenLights;
        float[][] specimenIntensities;
        readonly System.Collections.Generic.Dictionary<Renderer,MaterialPropertyBlock> originalLiquidBlocks=new System.Collections.Generic.Dictionary<Renderer,MaterialPropertyBlock>();
        MaterialPropertyBlock specimenBlock;
        Light[] dimmed;
        float[] intensities;
        bool[] lightEnabled;
        float ambient,reflection;
        Transform player;
        float nextSearch,fade;
        Volume grade;
        VolumeProfile profile;
        bool cached;

        void Awake()
        {
            Active=this;
            resting=new Vector3[bottles.Length];
            for(int i=0;i<bottles.Length;i++)if(bottles[i]!=null)resting[i]=bottles[i].position;
            liquids=new Renderer[bottles.Length][];specimenLights=new Light[bottles.Length][];specimenIntensities=new float[bottles.Length][];specimenBlock=new MaterialPropertyBlock();
            for(int i=0;i<bottles.Length;i++)
            {
                var surfaces=new System.Collections.Generic.List<Renderer>();
                if(bottles[i]!=null)foreach(var r in bottles[i].GetComponentsInChildren<Renderer>(true))if(r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Xiaoduan/Fluorescent Liquid URP"){surfaces.Add(r);var saved=new MaterialPropertyBlock();r.GetPropertyBlock(saved);originalLiquidBlocks[r]=saved;}
                liquids[i]=surfaces.ToArray();specimenLights[i]=bottles[i]!=null?bottles[i].GetComponentsInChildren<Light>(true):new Light[0];specimenIntensities[i]=new float[specimenLights[i].Length];
                for(int k=0;k<specimenLights[i].Length;k++)specimenIntensities[i][k]=specimenLights[i][k].intensity;
            }
            var list=new System.Collections.Generic.List<Light>();
            foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(l!=candle&&l!=keyLight&&(l.type==LightType.Directional||
                   (l.transform.position.z>entrance.position.z-5&&l.transform.position.z<entrance.position.z+44)))
                    if(!l.name.Contains("Potion"))list.Add(l);
            dimmed=list.ToArray();intensities=new float[dimmed.Length];lightEnabled=new bool[dimmed.Length];
            for(int i=0;i<dimmed.Length;i++){intensities[i]=dimmed[i].intensity;lightEnabled[i]=dimmed[i].enabled;}
            ambient=RenderSettings.ambientIntensity;reflection=RenderSettings.reflectionIntensity;cached=true;
            var obj=new GameObject("Entrance candle exposure");obj.transform.SetParent(transform,false);
            grade=obj.AddComponent<Volume>();grade.isGlobal=true;grade.priority=120;grade.weight=0;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();grade.profile=profile;
            var adjust=profile.Add<ColorAdjustments>();adjust.postExposure.Override(-1.05f);adjust.saturation.Override(-14);
            if(candle!=null)candle.shadows=LightShadows.None;
        }
        void Update()
        {
            if(player==null&&Time.unscaledTime>nextSearch){nextSearch=Time.unscaledTime+.5f;var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)player=go.transform;}
            bool inside=player!=null&&player.position.z>entrance.position.z+.35f&&player.position.z<entrance.position.z+44&&Mathf.Abs(player.position.x-entrance.position.x)<7&&Mathf.Abs(player.position.y-entrance.position.y)<7;
            if(inside&&!Entered)Entered=true;
            fade=Mathf.MoveTowards(fade,inside?1:0,Time.deltaTime*2.8f);
            if(grade!=null)grade.weight=fade;
            if(cached)
            {
                for(int i=0;i<dimmed.Length;i++)if(dimmed[i]!=null){dimmed[i].intensity=intensities[i]*(1-fade);dimmed[i].enabled=lightEnabled[i]&&fade<.999f;}
                RenderSettings.ambientIntensity=Mathf.Lerp(ambient,.025f,fade);
                RenderSettings.reflectionIntensity=Mathf.Lerp(reflection,.06f,fade);
            }
            if(candle!=null)candle.intensity=inside?2.8f+(SurveyProgress.ReducedMotion?0:Mathf.Sin(Time.time*8)*.09f):1;
            if(keyLight!=null)keyLight.intensity=inside?5.2f:2;
            float proximity=inside&&player!=null?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.4f,5f,Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(table.position.x,table.position.z)))):0;
            Lift=Mathf.MoveTowards(Lift,Mathf.Clamp01(Inspecting?1:proximity),Time.deltaTime*1.5f);
            float eased=Lift*Lift*(3-2*Lift);
            for(int i=0;i<bottles.Length;i++)if(bottles[i]!=null)
            {
                float bob=SurveyProgress.ReducedMotion?0:Mathf.Sin(Time.time*1.3f+i*1.7f)*.018f;
                bottles[i].position=resting[i]+Vector3.up*(eased*(.18f+(i%3)*.035f+bob));
                foreach(var r in liquids[i])if(r!=null){r.GetPropertyBlock(specimenBlock);specimenBlock.SetFloat("_EmissionStrength",Mathf.Lerp(.04f,1.55f,eased));specimenBlock.SetColor("_GlowColor",new Color(.10f,1,.22f));r.SetPropertyBlock(specimenBlock);}
                for(int k=0;k<specimenLights[i].Length;k++)if(specimenLights[i][k]!=null)specimenLights[i][k].intensity=Mathf.Lerp(specimenIntensities[i][k],.65f,eased);
            }
        }
        public void BeginInspection(){Inspecting=true;}
        public void EndInspection(){Inspecting=false;}
        public void Retain(){Retained=true;}
        void OnDisable()
        {
            if(!cached)return;
            for(int i=0;i<dimmed.Length;i++)if(dimmed[i]!=null){dimmed[i].intensity=intensities[i];dimmed[i].enabled=lightEnabled[i];}
            RenderSettings.ambientIntensity=ambient;RenderSettings.reflectionIntensity=reflection;
            for(int i=0;i<bottles.Length;i++)if(bottles[i]!=null)bottles[i].position=resting[i];
            foreach(var pair in originalLiquidBlocks)if(pair.Key!=null)pair.Key.SetPropertyBlock(pair.Value);
            for(int i=0;i<specimenLights.Length;i++)for(int k=0;k<specimenLights[i].Length;k++)if(specimenLights[i][k]!=null)specimenLights[i][k].intensity=specimenIntensities[i][k];
            if(grade!=null)grade.weight=0;
        }
        void OnDestroy(){if(Active==this)Active=null;if(profile!=null)Destroy(profile);}
    }
}
