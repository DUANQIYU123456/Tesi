using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Xiaoduan.Survey;
namespace Xiaoduan.Room3
{
    // Light is a spatial argument: exposure, absence, examination, then a choice about memory.
    [DefaultExecutionOrder(520)]
    public sealed class Room3ChromaticSequence:MonoBehaviour
    {
        public Room3StoryFlow flow;
        public Volume sequenceVolume;
        public Light entryGreen,clinicalDown,clinicalFill,goldBack,goldCore;
        public Light[] oldClinical,oldCore;
        public Light[] goldPathLights=new Light[0];
        public Renderer[] goldPathMarkers=new Renderer[0];
        public float[] clinicalPowers,corePowers;
        public Renderer clinicalBeam,sacredField;
        public Renderer[] oldCeiling,coreEdges;
        public ParticleSystem entryMist,clinicalDust;
        public AudioSource steam;
        public float entryZ=-18.7f;
        public float goldBackPower=1300,goldFogDensity=.0035f,goldBloom=.42f,radiancePower=2.1f;
        readonly System.Collections.Generic.List<(Renderer renderer,int slot,Color color)> witnessColors=new System.Collections.Generic.List<(Renderer,int,Color)>();
        public string Phase {get;private set;}="Sealed";
        public float MistAge {get;private set;}=-1;
        public float MistWeight {get;private set;}
        public float GoldWeight {get;private set;}
        public float ClinicalWeight {get;private set;}=1;
        public float GoldOutput {get;private set;}
        Transform player;
        Camera eye;
        float nextFind;
        bool mistDone,playedSteam;
        VolumetricFogVolumeComponent fog;
        ColorAdjustments grade;
        Bloom bloom;
        MaterialPropertyBlock block;
        readonly System.Collections.Generic.List<Renderer> archiveGlow=new System.Collections.Generic.List<Renderer>();
        readonly System.Collections.Generic.List<Color> archiveEmission=new System.Collections.Generic.List<Color>();
        readonly Color green=new Color(57/255f,1,20/255f);
        readonly Color cold=new Color(224/255f,1,1);
        readonly Color bone=new Color(245/255f,245/255f,220/255f);
        void Start()
        {
            var p=sequenceVolume.profile;p.TryGet(out fog);p.TryGet(out grade);p.TryGet(out bloom);
            block=new MaterialPropertyBlock();FindPlayer();
            foreach(var actor in FindObjectsByType<RocketboxWitness>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(actor.suspended&&!actor.workStation&&actor.variation<5)foreach(var r in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))for(int slot=0;slot<r.sharedMaterials.Length;slot++){var m=r.sharedMaterials[slot];if(m!=null&&m.HasProperty("_BaseColor"))witnessColors.Add((r,slot,m.GetColor("_BaseColor")));}
            foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {var m=r.sharedMaterial;if(r.gameObject.scene!=gameObject.scene||r.transform.position.z<44||m==null||!m.HasProperty("_EmissionColor"))continue;var c=m.GetColor("_EmissionColor");if(c.maxColorComponent>.2f){archiveGlow.Add(r);archiveEmission.Add(c);}}
        }
        void FindPlayer()
        {nextFind=Time.unscaledTime+.4f;var g=GameObject.FindGameObjectWithTag("Player");player=g!=null?g.transform:null;eye=Camera.main;}
        void LateUpdate()
        {
            if(player==null||eye==null||Time.unscaledTime>=nextFind)FindPlayer();
            if(player==null||eye==null||player.gameObject.scene!=gameObject.scene||fog==null)return;
            bool paused=SurveyHUD.BlocksWorld||flow.Reading;
            float dt=paused?0:Time.deltaTime;
            float z=player.position.z;
            bool entry=flow.embodied.DoorOpened&&z>entryZ-.3f&&z<entryZ+10&&Mathf.Abs(player.position.x)<13;
            if(entry&&!mistDone&&MistAge<0)MistAge=0;
            if(MistAge>=0&&!mistDone)MistAge+=dt;
            float duration=flow.ReducedMotion?2.4f:1.3f;
            if(MistAge>=3+duration)mistDone=true;
            float envelope=MistAge<0?0:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,3+duration,MistAge));
            MistWeight=entry&&!mistDone?envelope*(flow.ReducedMotion?.52f:1):0;
            if(MistWeight>.01f&&!playedSteam){playedSteam=true;if(steam!=null)steam.Play();}
            if(steam!=null){steam.volume=MistWeight*.16f;if(MistWeight<=.001f&&playedSteam)steam.Stop();}
            if(entryMist!=null)
            {
                bool run=flow.embodied.DoorOpened&&!mistDone;
                if(run&&!entryMist.isPlaying)entryMist.Play();
                if(!run&&entryMist.isPlaying)entryMist.Stop(true,ParticleSystemStopBehavior.StopEmitting);
                float cloud=MistAge<0?1:envelope;
                var em=entryMist.emission;em.rateOverTime=run?7*cloud:0;
                var main=entryMist.main;main.startColor=new Color(green.r,green.g,green.b,.11f*cloud);
            }
            float target=flow.OathOpened?1:0;
            GoldWeight=Mathf.MoveTowards(GoldWeight,target,dt/(flow.ReducedMotion?3.5f:2.1f));
            int preview=flow.ui.DecisionVisible?flow.embodied.Preview:-1;
            float choice=preview==0?0:preview==1?1.12f:1;
            GoldOutput=flow.EndingChosen?0:GoldWeight*choice;
            ClinicalWeight=1-Mathf.SmoothStep(0,1,GoldWeight);
            float region=Mathf.SmoothStep(0,1,Mathf.InverseLerp(20,30,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(48,60,z)));
            Phase=MistWeight>.02f?"Radium green":GoldWeight>.1f?(preview==0?"Format / cold erasure":"Sacred bone / witness"):region>.3f?"Clinical examination":flow.embodied.DoorOpened?"Black archive":"Sealed";
            entryGreen.color=green;entryGreen.intensity=20*MistWeight;
            clinicalDown.color=cold;clinicalDown.intensity=300*ClinicalWeight;
            clinicalFill.color=cold;clinicalFill.intensity=14*ClinicalWeight;
            goldBack.color=bone;goldBack.intensity=goldBackPower*GoldOutput;
            goldCore.color=Color.Lerp(bone,new Color(1,.8f,.25f),.28f);goldCore.intensity=95*GoldOutput;
            foreach(var l in goldPathLights)if(l!=null)l.intensity=2.8f*GoldOutput;
            foreach(var r in goldPathMarkers)if(r!=null)r.enabled=GoldOutput>.05f;
            for(int i=0;i<oldClinical.Length;i++)if(oldClinical[i]!=null)oldClinical[i].intensity=clinicalPowers[i]*.10f*ClinicalWeight;
            for(int i=0;i<oldCore.Length;i++)if(oldCore[i]!=null)oldCore[i].intensity=corePowers[i]*(.18f+.08f*GoldOutput);
            block.Clear();block.SetFloat("_Strength",.115f*ClinicalWeight);clinicalBeam.SetPropertyBlock(block);
            block.Clear();block.SetColor("_BaseColor",new Color(radiancePower,radiancePower,radiancePower,Mathf.Clamp01(GoldOutput)));sacredField.SetPropertyBlock(block);
            sacredField.transform.rotation=Quaternion.LookRotation(sacredField.transform.position-eye.transform.position);
            foreach(var witness in witnessColors){if(witness.renderer==null)continue;witness.renderer.GetPropertyBlock(block,witness.slot);float shade=GoldOutput>.01f?Mathf.Lerp(1,.12f,GoldWeight):1;var color=witness.color;block.SetColor("_BaseColor",new Color(color.r*shade,color.g*shade,color.b*shade,color.a));witness.renderer.SetPropertyBlock(block,witness.slot);}
            block.Clear();block.SetColor("_BaseColor",Color.Lerp(new Color(.004f,.008f,.009f),new Color(.04f,.029f,.012f),GoldOutput));
            foreach(var r in oldCeiling)if(r!=null)r.SetPropertyBlock(block);
            block.Clear();block.SetColor("_EmissionColor",new Color(.2f,1.1f,.3f)*Mathf.Lerp(.65f,.025f,GoldWeight));foreach(var r in coreEdges)if(r!=null)r.SetPropertyBlock(block);
            for(int i=0;i<archiveGlow.Count;i++)if(archiveGlow[i]!=null){archiveGlow[i].GetPropertyBlock(block);block.SetColor("_EmissionColor",archiveEmission[i]*(1-GoldWeight*.99f));archiveGlow[i].SetPropertyBlock(block);}
            Shader.SetGlobalFloat("_Room3Reveal",Shader.GetGlobalFloat("_Room3Reveal")*(1-GoldWeight));
            if(clinicalDust!=null){var e=clinicalDust.emission;e.rateOverTime=ClinicalWeight*5;}
            fog.enabled.Override(true);fog.enableAdditionalLightsContribution.Override(true);fog.enableMainLightContribution.Override(false);
            fog.density.Override(MistWeight>.001f?.56f*MistWeight:Mathf.Lerp(.004f,.025f,region)*ClinicalWeight+goldFogDensity*GoldOutput);
            fog.distance.Override(MistWeight>.001f?13:85);
            fog.baseHeight.Override(0);fog.maximumHeight.Override(MistWeight>.001f?5.5f:14);
            fog.attenuationDistance.Override(MistWeight>.001f?12:105);
            fog.tint.Override(MistWeight>.001f?green:Color.Lerp(cold,bone,GoldOutput));
            grade.postExposure.Override(preview==0?-.05f:Mathf.Lerp(.05f,-.18f,GoldWeight));
            grade.contrast.Override(18);grade.saturation.Override(preview==0?-85:Mathf.Lerp(-16,-4,GoldWeight));
            grade.colorFilter.Override(Color.white);
            bloom.threshold.Override(1.15f);bloom.intensity.Override(Mathf.Lerp(.34f,goldBloom,GoldOutput));bloom.scatter.Override(.65f);bloom.clamp.Override(8);
            if(GoldWeight>.5f&&z>85)SurveyHUD.Instance?.SuppressWorldOverlay();
            eye.backgroundColor=Color.black;eye.clearFlags=CameraClearFlags.SolidColor;
        }
    }
}
