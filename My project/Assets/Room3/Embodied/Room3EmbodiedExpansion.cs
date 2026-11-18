using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using StarterAssets;
using TMPro;
using Xiaoduan.Survey;

namespace Xiaoduan.Room3
{
    // Adds physical feedback to the retained archive route; the story flow owns progression.
    [DefaultExecutionOrder(350)]
    public sealed class Room3EmbodiedExpansion : MonoBehaviour
    {
        public Room3StoryFlow flow;
        public Transform door,valve;
        public Collider doorBarrier;
        public Light warningLight;
        public Renderer warningLens;
        public Material warningRed,warningGreen;
        public ParticleSystem[] steam;
        public ParticleSystem impactDust,oathBurst;
        public Transform sonarRing;
        public Room3MemoryFragment[] fragments;
        public Renderer[] finalBridgeVisuals;
        public Light[] chestLights;
        public Renderer[] aurora;
        public AudioSource mechanical,sonarAudio,memoryAudio,endingAudio;
        public AudioClip valveStroke,pressureRelease,pulseSound,memoryMix,formatHum,preserveChord,finalTick;
        public TMP_Text pressureReadout;
        public bool DoorOpened {get;private set;}
        public bool Gripping {get;private set;}
        public float Pressure {get;private set;}
        public int Pulses {get;private set;}
        public int Memories {get;private set;}
        public int Preview {get;private set;}=-1;
        public float PulseAge=>pulseAge;
        public bool Focused=>focused!=null;
        public Room3MemoryFragment FocusedFragment=>focused;
        Transform player;
        Camera eye;
        CinemachineCamera rig;
#pragma warning disable CS0618 // The existing project rig is a legacy CinemachineVirtualCamera.
        CinemachineVirtualCamera legacyRig;
#pragma warning restore CS0618
        float baseFov,findAt,pulseAge=10,nextPulse,lastStroke,shake;
        float ringOpacity,previewFormat,previewPreserve,doorVelocity,valveAngle,valveAngleTarget;
        Quaternion valveOrigin;
        Vector3 doorOrigin,pulseOrigin;
        Room3MemoryFragment focused;
        Material ringMaterial;
        bool bridgeShown,endingCommitted,styled,decisionStyled;
        ColorAdjustments colorAdjust;
        float initialExposure,initialSaturation;
        Color initialFilter;
        bool initialExposureOverride,initialSaturationOverride,initialFilterOverride;
        readonly List<Light> previewLights=new List<Light>();
        readonly List<Color> lightColors=new List<Color>();
        readonly List<float> lightIntensities=new List<float>();
        readonly List<Renderer> emissive=new List<Renderer>();
        readonly List<Color> emissionColors=new List<Color>();
        readonly List<MaterialPropertyBlock> originalBlocks=new List<MaterialPropertyBlock>();
        readonly Dictionary<Renderer,bool> hiddenLabels=new Dictionary<Renderer,bool>();
        bool labelsHidden;
        MaterialPropertyBlock block;

        void Start()
        {
            doorOrigin=door.localPosition;valveOrigin=valve.localRotation;flow.embodied=this;
            foreach(var r in finalBridgeVisuals)r.enabled=false;
            foreach(var r in aurora)r.enabled=false;
            foreach(var light in chestLights)light.intensity=0;
            ringMaterial=sonarRing.GetComponent<Renderer>().material;
            sonarRing.gameObject.SetActive(false);
            var profile=flow.volume.profile;
            if(!profile.TryGet(out colorAdjust))colorAdjust=profile.Add<ColorAdjustments>();
            initialExposure=colorAdjust.postExposure.value;initialSaturation=colorAdjust.saturation.value;initialFilter=colorAdjust.colorFilter.value;
            initialExposureOverride=colorAdjust.postExposure.overrideState;initialSaturationOverride=colorAdjust.saturation.overrideState;initialFilterOverride=colorAdjust.colorFilter.overrideState;
            foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(l.gameObject.scene==gameObject.scene&&l!=warningLight&&System.Array.IndexOf(chestLights,l)<0)
                {previewLights.Add(l);lightColors.Add(l.color);lightIntensities.Add(l.intensity);}
            foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var m=r.sharedMaterial;
                if(r.gameObject.scene!=gameObject.scene||m==null||!m.HasProperty("_EmissionColor")||!m.IsKeywordEnabled("_EMISSION"))continue;
                var c=m.GetColor("_EmissionColor");if(c.maxColorComponent<.3f)continue;
                var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);originalBlocks.Add(b);emissive.Add(r);emissionColors.Add(c);
            }
            block=new MaterialPropertyBlock();FindPlayer();
        }
        void FindPlayer()
        {
            findAt=Time.unscaledTime+.7f;
            var g=GameObject.FindGameObjectWithTag("Player");player=g!=null?g.transform:null;eye=Camera.main;
            if(rig==null&&legacyRig==null)
            {
                rig=FindFirstObjectByType<CinemachineCamera>();
#pragma warning disable CS0618 // Keep the shipped rig while also supporting the newer camera.
                legacyRig=FindFirstObjectByType<CinemachineVirtualCamera>();
#pragma warning restore CS0618
                baseFov=rig!=null?rig.Lens.FieldOfView:legacyRig!=null?legacyRig.m_Lens.FieldOfView:eye!=null?eye.fieldOfView:40;
            }
        }
        bool ActivePlayer=>player!=null&&eye!=null&&player.gameObject.scene==gameObject.scene;
        bool Modal=>flow.Reading||flow.ui.DecisionVisible||flow.EndingChosen||SurveyHUD.BlocksWorld;
        void Update()
        {
            if(Time.unscaledTime>=findAt||player==null||eye==null)FindPlayer();
            if(!ActivePlayer)return;
            if(SurveyHUD.Instance!=null&&(!styled||(flow.ui.DecisionVisible&&!decisionStyled))){StyleDecision();styled=true;}
            decisionStyled=flow.ui.DecisionVisible;
            SetWorldLabelsHidden(flow.ui.DecisionVisible);
            if(SurveyHUD.BlocksWorld){RestoreZoom();return;}
            if(flow.EndingChosen){TickEnding();return;}
            if(!DoorOpened)TickValve();
            else door.localPosition=Vector3.MoveTowards(door.localPosition,doorOrigin+Vector3.up*4.8f,Time.deltaTime*4);
            if(!Modal&&!Gripping)TickSonar();
            else {pulseAge=10;Shader.SetGlobalFloat("_R3PulseEnabled",0);if(sonarRing.gameObject.activeSelf)sonarRing.gameObject.SetActive(false);}
            TickFragments();
            shake=Mathf.MoveTowards(shake,0,Time.deltaTime*1.6f);
            if(!flow.ReducedMotion)flow.cameraInterference.strength=Mathf.Max(flow.cameraInterference.strength,shake);
            if(flow.OathOpened)
            {
                foreach(var l in chestLights)l.intensity=Mathf.MoveTowards(l.intensity,2.5f,Time.deltaTime*3);
                if(!bridgeShown){foreach(var r in finalBridgeVisuals)r.enabled=true;bridgeShown=true;}
            }
            TickPreview();
        }
        void TickValve()
        {
            warningLight.intensity=flow.ReducedMotion?1.45f:1.4f+Mathf.Pow(Mathf.Sin(Time.time*2.2f)*.5f+.5f,3)*.8f;
            bool e=Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame;
            bool escape=Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame;
            float distance=Vector3.Distance(eye.transform.position,valve.position);
            bool aims=Vector3.Dot(eye.transform.forward,(valve.position-eye.transform.position).normalized)>.87f;
            if(!Gripping&&e&&distance<2.65f&&aims&&!Modal)
            {Gripping=true;lastStroke=Time.time;flow.SetExternalModal(true);}
            else if(Gripping&&(escape||e))ReleaseValve();
            if(Gripping)
            {
                bool stroke=(Keyboard.current!=null&&Keyboard.current.spaceKey.isPressed&&Time.time-lastStroke>.115f)||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame);
                if(stroke&&Time.time-lastStroke>.065f)
                {
                    Pressure=Mathf.Min(1,Pressure+.085f);lastStroke=Time.time;
                    valveAngleTarget-=26;shake=.1f;
                    if(flow.soundscape!=null)flow.soundscape.ValveStroke(Pressure);else {mechanical.pitch=.72f+Pressure*.35f;mechanical.PlayOneShot(valveStroke,.4f);}
                    foreach(var p in steam)p.Emit(4);
                }
            }
            if(Time.time-lastStroke>.35f)Pressure=Mathf.MoveTowards(Pressure,0,Time.deltaTime*.16f);
            float height=Mathf.SmoothDamp(door.localPosition.y,doorOrigin.y+Pressure*4.8f,ref doorVelocity,Time.time-lastStroke>.35f?.21f:.1f);
            door.localPosition=new Vector3(doorOrigin.x,height,doorOrigin.z);
            valveAngle=Mathf.Lerp(valveAngle,valveAngleTarget,1-Mathf.Exp(-Time.deltaTime*18));valve.localRotation=valveOrigin*Quaternion.Euler(0,0,valveAngle);
            pressureReadout.text="MANUAL OVERRIDE\n"+Mathf.RoundToInt(Pressure*100).ToString("00")+"% / PRESSURE";
            if(Pressure>=.999f)
            {
                DoorOpened=true;ReleaseValve();doorBarrier.enabled=false;
                warningLight.color=new Color(.25f,1,.45f);warningLight.intensity=2;warningLens.sharedMaterial=warningGreen;
                if(flow.soundscape==null){mechanical.pitch=1;mechanical.PlayOneShot(pressureRelease,.65f);}foreach(var p in steam)p.Emit(36);
                flow.ui.Notice("PRESSURE LOCK BROKEN / ARCHIVE ACCESS RESTORED",3);pressureReadout.text="MANUAL OVERRIDE\nLOCK RELEASED";
            }
        }
        void ReleaseValve(){if(!Gripping)return;Gripping=false;flow.SetExternalModal(false);}
        void TickSonar()
        {
            bool held=Mouse.current!=null&&Mouse.current.rightButton.isPressed;
            if(held&&Time.time>=nextPulse)
            {
                pulseAge=0;pulseOrigin=player.position+Vector3.up*.15f;nextPulse=Time.time+4.15f;Pulses++;
                if(flow.soundscape!=null)flow.soundscape.Pulse();else sonarAudio.PlayOneShot(pulseSound,.22f);
            }
            pulseAge+=Time.deltaTime;
            Shader.SetGlobalVector("_R3PulseOrigin",pulseOrigin);Shader.SetGlobalFloat("_R3PulseAge",pulseAge);
            Shader.SetGlobalFloat("_R3PulseEnabled",pulseAge<4?1:0);
            bool show=pulseAge<2.1f;sonarRing.gameObject.SetActive(show);
            if(show)
            {
                sonarRing.position=pulseOrigin;sonarRing.localScale=Vector3.one*Mathf.Max(.01f,pulseAge*23);
                ringOpacity=(1-Mathf.Clamp01(pulseAge/2.1f))*(flow.ReducedMotion?.14f:.30f);ringMaterial.SetFloat("_Opacity",ringOpacity);
            }
        }
        void TickFragments()
        {
            focused=null;
            if(Modal||Gripping||!flow.BridgeLocked){RestoreZoom();return;}
            foreach(var f in fragments)
            {
                var d=f.focus.position-eye.transform.position;
                if(d.magnitude>3.2f||Vector3.Dot(eye.transform.forward,d.normalized)<.96f)continue;
                if(Physics.Raycast(eye.transform.position,d.normalized,out var hit,d.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(f.transform))continue;
                focused=f;break;
            }
            float current=rig!=null?rig.Lens.FieldOfView:legacyRig!=null?legacyRig.m_Lens.FieldOfView:eye.fieldOfView;
            SetFov(Mathf.MoveTowards(current,focused!=null?baseFov-7:baseFov,Time.unscaledDeltaTime*25));
            foreach(var f in fragments)
            {
                if(f.Retained)continue;
                if(f==focused&&Keyboard.current!=null&&Keyboard.current.eKey.isPressed)
                {
                    f.Progress=Mathf.Min(1,f.Progress+Time.deltaTime/1.5f);
                    float jitter=flow.ReducedMotion?0:Mathf.Sin(Time.time*70)*.012f;
                    f.visual.localPosition+=new Vector3(jitter,0,0);
                    if(f.Progress>=1)
                    {
                        f.Retained=true;Memories++;if(flow.soundscape==null)memoryAudio.PlayOneShot(memoryMix,.28f);
                        flow.ui.Notice(f.retainedText,4);
                    }
                }
                else f.Progress=Mathf.MoveTowards(f.Progress,0,Time.deltaTime*.4f);
            }
        }
        void SetFov(float value){if(rig!=null)rig.Lens.FieldOfView=value;else if(legacyRig!=null)legacyRig.m_Lens.FieldOfView=value;else if(eye!=null)eye.fieldOfView=value;}
        void RestoreZoom(){if(baseFov>0)SetFov(baseFov);}
        void LateUpdate()
        {
            if(!ActivePlayer||SurveyHUD.BlocksWorld||flow.EndingChosen)return;
            if(!DoorOpened)
            {
                flow.ui.SetStatus("Sector sealed. Use the red valve to overload the pressure lock.",flow.DecodedCount,flow.ReducedMotion);
                float distance=Vector3.Distance(eye.transform.position,valve.position);
                bool aims=Vector3.Dot(eye.transform.forward,(valve.position-eye.transform.position).normalized)>.87f;
                if(Gripping)flow.ui.SetPrompt("HOLD SPACE / CLICK LMB TO FORCE THE VALVE", "Maintain pressure. [E / ESC] release the wheel.",Pressure);
                else if(distance<2.65f&&aims)flow.ui.SetPrompt("[E] GRIP THE RED VALVE","Then hold [SPACE] or click LMB to build pressure.",Pressure);
                else if(distance<6f)flow.ui.SetPrompt(distance>=2.65f?"MOVE CLOSER TO THE VALVE":"AIM AT THE RED WHEEL","Approach the wheel, aim at it, then press [E].",0);
            }
            else if(focused!=null&&!flow.Reading&&!flow.ui.DecisionVisible)
                flow.ui.SetPrompt(focused.Retained?"MEMORY RETAINED":"HOLD [E] / RETAIN THE FRAGMENT","OPTIONAL TRACE / "+focused.title,focused.Retained?1:focused.Progress);
            if(flow.ui.DecisionVisible)
            {
                flow.ui.decisionPanel.GetComponent<UnityEngine.UI.Image>().color=new Color(.006f,.009f,.008f,.96f);
                var t=flow.ui.decisionPanel.transform.Find("Ending preview");if(t!=null)t.GetComponent<TMP_Text>().text=Preview==0?"Erase the local record. Return to the town; its portal closes.":Preview==1?"Retain the evidence. Keep the ruins as a memorial.":"Hover to preview the world you will leave behind.  /  [ESC] return";
            }
        }
        public void BridgeImpact(int module)
        {
            flow.soundscape?.BridgeImpact(module);
            shake=flow.ReducedMotion?0:module<0?.25f:.9f;
            if(module>=0)
            {
                impactDust.transform.position=flow.bridgeModules[module].position+Vector3.up*.5f;impactDust.Emit(24);
                if(flow.soundscape==null)flow.effectsSource.PlayOneShot(flow.bridgeSound,.28f+module*.07f);
            }
        }
        public void OathCompleted()
        {
            oathBurst.Play();shake=flow.ReducedMotion?0:1;
            if(flow.soundscape!=null)flow.soundscape.OathRelease();else mechanical.PlayOneShot(pressureRelease,.25f);
        }
        public void SetEndingPreview(int value)
        {
            if(endingCommitted)return;Preview=value;
            if(flow.soundscape!=null){flow.soundscape.Preview(value);endingAudio.Stop();return;}
            if(endingAudio==null)return;endingAudio.Stop();
            if(value<0)return;
            endingAudio.clip=value==0?formatHum:preserveChord;endingAudio.loop=true;endingAudio.volume=value==0?.14f:.2f;endingAudio.Play();
        }
        void TickPreview()
        {
            int wanted=flow.ui.DecisionVisible?Preview:-1;
            if(!flow.ui.DecisionVisible&&Preview>=0)SetEndingPreview(-1);
            if(Mathf.Abs(previewFormat-(wanted==0?1:0))<.001f&&Mathf.Abs(previewPreserve-(wanted==1?1:0))<.001f)return;
            float format=previewFormat=Mathf.MoveTowards(previewFormat,wanted==0?1:0,Time.unscaledDeltaTime*3);
            float preserve=previewPreserve=Mathf.MoveTowards(previewPreserve,wanted==1?1:0,Time.unscaledDeltaTime*3);
            colorAdjust.postExposure.Override(initialExposure+format*.55f+preserve*.12f);
            colorAdjust.saturation.Override(Mathf.Lerp(initialSaturation,-100,format));
            colorAdjust.colorFilter.Override(Color.Lerp(initialFilter,format>.01f?new Color(.85f,.91f,1):new Color(.8f,1,.84f),Mathf.Max(format,preserve)*.35f));
            for(int i=0;i<previewLights.Count;i++)if(previewLights[i]!=null)
            {
                previewLights[i].color=Color.Lerp(lightColors[i],new Color(.76f,1,.75f),preserve*.55f);
                previewLights[i].intensity=lightIntensities[i]*(1-format*.88f+preserve*.25f);
            }
            for(int i=0;i<emissive.Count;i++)if(emissive[i]!=null)
            {emissive[i].GetPropertyBlock(block);block.SetColor("_EmissionColor",emissionColors[i]*(1-format));emissive[i].SetPropertyBlock(block);}
            foreach(var r in aurora)r.enabled=preserve>.05f;
            if(wanted<0&&format==0&&preserve==0)RestoreWorld();
        }
        public void CommitEnding(bool preserve)
        {
            SetWorldLabelsHidden(false);
            SetEndingPreview(-1);RestoreWorld();endingCommitted=true;RestoreZoom();
            if(flow.soundscape!=null){flow.soundscape.Commit();endingAudio.Stop();}else {endingAudio.Stop();endingAudio.loop=false;endingAudio.PlayOneShot(finalTick,.35f);}
            foreach(var r in aurora)r.enabled=preserve;
            Shader.SetGlobalFloat("_R3PulseEnabled",0);sonarRing.gameObject.SetActive(false);
        }
        // Soundscape owns the shared memorial timing for both outcomes.
        void TickEnding(){RestoreZoom();}
        void StyleDecision()
        {
            var ui=flow.ui;var panel=(RectTransform)ui.decisionPanel.transform;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,0);
            panel.anchoredPosition=new Vector2(0,32);panel.sizeDelta=new Vector2(1720,430);
            Layout("Decision title",new Vector2(0,162),new Vector2(1540,50),31,"THE RECORD IS IN YOUR HANDS");
            Layout("Decision explanation",new Vector2(0,112),new Vector2(1540,42),23,"What should this system retain?");
            Layout("Format consequence",new Vector2(-410,-75),new Vector2(720,80),23,"Erase the local archive.\nReturn to the town; close the portal.");
            Layout("Preserve consequence",new Vector2(410,-75),new Vector2(720,80),23,"Retain the evidence.\nLet the ruins remain as a witness.");
            Layout("Ending preview",new Vector2(0,-172),new Vector2(1530,42),21,null);
            Button(ui.formatButton,-410,"ERASE");Button(ui.preserveButton,410,"PRESERVE");
            var border=panel.Find("Bloodlines perimeter");if(border!=null)border.gameObject.SetActive(false);
            void Layout(string name,Vector2 p,Vector2 size,float fontSize,string text)
            {var t=panel.Find(name);if(t==null)return;var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=p;r.sizeDelta=size;var label=t.GetComponent<TMP_Text>();label.fontSize=fontSize;if(text!=null)label.text=text;}
            void Button(UnityEngine.UI.Button button,float x,string caption)
            {
                var r=(RectTransform)button.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,30);r.sizeDelta=new Vector2(740,96);
                var label=button.GetComponentInChildren<TMP_Text>();label.text=caption;label.font=Xiaoduan.Survey.SurveyTypography.Instance!=null?Xiaoduan.Survey.SurveyTypography.Instance.titleFont:ui.archiveBody.font;label.fontSize=48;label.enableAutoSizing=false;label.fontSizeMin=38;label.fontSizeMax=48;
                label.fontStyle=FontStyles.Normal;label.characterSpacing=2;
                var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=null;image.material=null;
                var feedback=button.GetComponent<SurveyButton>();if(feedback!=null){feedback.idle=new Color(.018f,.03f,.024f,.78f);feedback.hover=new Color(.12f,.25f,.17f,.88f);image.color=feedback.idle;}
            }
        }
        void RestoreWorld()
        {
            if(colorAdjust!=null){colorAdjust.postExposure.value=initialExposure;colorAdjust.saturation.value=initialSaturation;colorAdjust.colorFilter.value=initialFilter;colorAdjust.postExposure.overrideState=initialExposureOverride;colorAdjust.saturation.overrideState=initialSaturationOverride;colorAdjust.colorFilter.overrideState=initialFilterOverride;}
            for(int i=0;i<previewLights.Count;i++)if(previewLights[i]!=null){previewLights[i].intensity=lightIntensities[i];previewLights[i].color=lightColors[i];}
            for(int i=0;i<emissive.Count;i++)if(emissive[i]!=null)emissive[i].SetPropertyBlock(originalBlocks[i]);
            foreach(var r in aurora)if(r!=null)r.enabled=false;
        }
        void SetWorldLabelsHidden(bool value)
        {
            if(value==labelsHidden)return;labelsHidden=value;
            if(value)
            {
                foreach(var label in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(label.gameObject.scene!=gameObject.scene)continue;
                    var r=label.GetComponent<Renderer>();if(r==null)continue;hiddenLabels[r]=r.enabled;r.enabled=false;
                }
            }
            else {foreach(var pair in hiddenLabels)if(pair.Key!=null)pair.Key.enabled=pair.Value;hiddenLabels.Clear();}
        }
        void OnDisable()
        {
            ReleaseValve();RestoreZoom();RestoreWorld();SetWorldLabelsHidden(false);Shader.SetGlobalFloat("_R3PulseEnabled",0);
            if(sonarRing!=null)sonarRing.gameObject.SetActive(false);
            if(endingAudio!=null)endingAudio.Stop();
        }
        void OnDestroy(){if(ringMaterial!=null)Destroy(ringMaterial);}
    }
}
