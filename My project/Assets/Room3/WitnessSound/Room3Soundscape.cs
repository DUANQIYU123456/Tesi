using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Xiaoduan.Survey;
namespace Xiaoduan.Room3
{
    // A scene-local score driven by the existing story, not a global audio framework.
    [DefaultExecutionOrder(460)]
    public sealed class Room3Soundscape:MonoBehaviour
    {
        public Room3StoryFlow flow;
        public AudioSource alarm,leak,wind,drone,gear,latch,sonar,radiation,bridge,settling;
        public AudioSource watch,cough,fan,clicks,riser,heart,organ,choir,wipe,pad,gavel,credits;
        public AudioClip gearClip,latchClip,sonarClip,radiationClip,bridgeClip,settlingClip,coughClip,gavelClip;
        public AudioReverbZone abyssReverb;
        public string Phase {get;private set;}="sealed";
        public int GearStrokes {get;private set;}
        public int BridgeStrikes {get;private set;}
        public int SonarPulses {get;private set;}
        public bool AlignmentSilence {get;private set;}
        public bool MusicReleased {get;private set;}
        public bool TerminalSilence {get;private set;}
        public float CreditsAge {get;private set;}=-1;
        Transform player;
        float findAt,lastStroke=-10,lastPressure,watchMix,computeMix,nextCough,endingAt;
        bool latched,terminal,committed,endingStarted,formatCreditsStyled;
        int hovered=-1;
        AudioSource[] world;
        void Start()
        {
            world=new[]{alarm,leak,wind,drone,gear,latch,sonar,radiation,bridge,settling,watch,cough,fan,clicks,riser,heart,organ,choir};
            foreach(var a in world)if(a!=null){a.Stop();a.volume=0;if(a.loop&&a!=organ&&a!=choir)a.Play();}
            credits.Stop();wipe.Stop();pad.Stop();gavel.Stop();
            // The previous prototype bed contains clocks and tonal sounds throughout the abyss.
            foreach(var a in new[]{flow.clockSource,flow.clickSource,flow.geigerSource,flow.heartbeatSource}){a.Stop();a.volume=0;}
        }
        void FindPlayer()
        {findAt=Time.unscaledTime+.8f;var p=GameObject.FindGameObjectWithTag("Player");player=p!=null?p.transform:null;}
        float ReaderGain=>flow.Reading?.3f:1;
        void Level(AudioSource a,float value,float speed=1.6f)
        {if(a==null)return;a.volume=Mathf.MoveTowards(a.volume,value,Time.unscaledDeltaTime*speed);if(value>.0001f&&a.loop&&!a.isPlaying)a.Play();}
        void Update()
        {
            if(player==null||Time.unscaledTime>findAt)FindPlayer();
            if(committed){TickCredits();return;}
            if(player==null||flow.embodied==null)return;
            if(SurveyHUD.BlocksWorld)return;
            var x=flow.embodied;float dt=Time.unscaledDeltaTime;float z=player.position.z;
            if(!latched&&x.DoorOpened)DoorLatched();
            bool decision=flow.ui.DecisionVisible;
            bool atTerminal=flow.OathOpened&&z>89;
            if(atTerminal||decision)
            {
                if(!terminal){terminal=true;organ.Stop();choir.Stop();MusicReleased=false;TerminalSilence=true;}
                Phase=decision?"judgment":"terminal";
            }
            bool mute=decision||atTerminal;
            bool align=!flow.BridgeLocked&&flow.CalibrationProgress>.565f&&!flow.Reading;
            AlignmentSilence=align;
            float audience=ReaderGain;
            float alarmGain=x.DoorOpened?0:Mathf.Lerp(.13f,.035f,Mathf.Clamp01((z+24)/8));
            Level(alarm,mute?0:alarmGain*audience);
            float falling=!x.DoorOpened&&x.Pressure>.005f&&Time.time-lastStroke>.35f?1:0;
            Level(leak,mute?0:(falling*.19f+(!x.DoorOpened?.027f:0))*audience);
            if(align||mute){wind.volume=0;drone.volume=0;}
            else {Level(wind,x.DoorOpened?.045f*audience:0);Level(drone,x.DoorOpened?.037f*audience:0);}
            if(!mute&&!terminal)Phase=!x.DoorOpened?"sealed":flow.OathOpened?"testimony":flow.BridgeLocked?"pilgrimage":"abyss";
            var focused=x.FocusedFragment;
            float targetWatch=!mute&&focused==x.fragments[0]?1:0,targetCompute=!mute&&focused==x.fragments[1]?1:0;
            watchMix=Mathf.MoveTowards(watchMix,targetWatch,dt*3);computeMix=Mathf.MoveTowards(computeMix,targetCompute,dt*3);
            watch.volume=watchMix*.18f*audience;fan.volume=computeMix*.07f*audience;clicks.volume=computeMix*.14f*audience;
            if(watch.volume>.0001f&&!watch.isPlaying)watch.Play();if(fan.volume>.0001f&&!fan.isPlaying)fan.Play();if(clicks.volume>.0001f&&!clicks.isPlaying)clicks.Play();
            if(watchMix>.8f&&Time.time>nextCough){nextCough=Time.time+10;cough.volume=.19f*audience;cough.PlayOneShot(coughClip);}
            if(watchMix<.01f)cough.Stop();
            bool near=Vector3.Distance(player.position,flow.oathPoint.position)<4.2f;
            bool held=Mouse.current!=null&&Mouse.current.leftButton.isPressed||Keyboard.current!=null&&Keyboard.current.eKey.isPressed;
            bool effort=near&&held&&!mute&&!flow.Reading&&flow.CourtDecoded&&flow.BridgeLocked&&!flow.OathOpened;
            float pressure=flow.OathProgress;
            Level(riser,effort?Mathf.Lerp(.035f,.15f,pressure):0,.5f);riser.pitch=1+pressure*1.8f;
            Level(heart,effort?Mathf.Lerp(.06f,.19f,pressure):0,.7f);heart.pitch=1+pressure*.7f;
            if(MusicReleased&&!mute){Level(organ,.42f*audience,.6f);Level(choir,.2f*audience,.5f);}
            else if(!MusicReleased){organ.volume=0;choir.volume=0;}
            lastPressure=x.Pressure;
            if(!decision&&hovered>=0)Preview(-1);
        }
        public void ValveStroke(float pressure)
        {
            GearStrokes++;lastStroke=Time.time;gear.transform.position=flow.embodied.valve.position;gear.pitch=.76f+pressure*.22f;gear.volume=ReaderGain;gear.PlayOneShot(gearClip,.22f);
        }
        void DoorLatched()
        {
            latched=true;alarm.Stop();alarm.volume=0;leak.Stop();leak.volume=0;gear.Stop();
            latch.transform.position=flow.embodied.door.position;latch.volume=1;latch.PlayOneShot(latchClip,.46f);Phase="abyss";
        }
        public void Pulse()
        {
            SonarPulses++;sonar.volume=ReaderGain;sonar.PlayOneShot(sonarClip,.22f);StartCoroutine(RevealCrackle());
        }
        IEnumerator RevealCrackle()
        {
            yield return new WaitForSeconds(.32f);if(committed||flow.Reading||flow.ui.DecisionVisible)yield break;
            radiation.volume=ReaderGain;radiation.PlayOneShot(radiationClip,.12f);
        }
        public void BridgeImpact(int module)
        {
            if(module<0){bridge.volume=1;bridge.PlayOneShot(bridgeClip,.52f);return;}
            BridgeStrikes++;settling.transform.position=flow.bridgeModules[module].position;settling.volume=1;settling.pitch=.91f+module*.07f;settling.PlayOneShot(settlingClip,.17f);
        }
        public void OathRelease()
        {
            riser.Stop();heart.Stop();organ.volume=.42f;choir.volume=.2f;organ.Play();choir.Play();MusicReleased=true;
            settling.transform.position=flow.oathPoint.position;settling.pitch=1.2f;settling.volume=1;settling.PlayOneShot(settlingClip,.17f);Phase="testimony";
        }
        public void Preview(int value)
        {
            if(committed||hovered==value)return;hovered=value;wipe.Stop();pad.Stop();gavel.Stop();
            if(value<0)return;
            foreach(var a in world)if(a!=null)a.volume=0;
            organ.Stop();choir.Stop();TerminalSilence=true;MusicReleased=false;
            if(value==0){wipe.volume=.115f;wipe.Play();}
            else {pad.volume=.13f;pad.Play();gavel.volume=.2f;gavel.PlayOneShot(gavelClip);}
        }
        public void Commit()
        {
            committed=true;hovered=-1;foreach(var a in world)if(a!=null)a.Stop();wipe.Stop();pad.Stop();gavel.Stop();endingAt=Time.unscaledTime;Phase="credits";
        }
        void TickCredits()
        {
            if(!endingStarted&&flow.ui!=null&&flow.ui.endingPanel.activeSelf){endingStarted=true;CreditsAge=0;credits.volume=.17f;credits.Play();foreach(var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))if(a.GetComponentInParent<Room3Soundscape>()==null&&a.loop)a.Stop();}
            if(!endingStarted)return;CreditsAge+=Time.unscaledDeltaTime;credits.volume=.17f*Mathf.Clamp01((38-CreditsAge)/10);
            if(CreditsAge>Room3StoryUI.ResultHoldSeconds)
            {
                if(!formatCreditsStyled){formatCreditsStyled=true;flow.ui.ShowMemorial();}
                float age=CreditsAge-Room3StoryUI.ResultHoldSeconds;
                var rect=flow.ui.endingBody.rectTransform;rect.anchoredPosition=new Vector2(0,Mathf.Lerp(-390,390,Mathf.Clamp01(age/32)));
                flow.ui.portraitMollie.color=flow.ui.portraitGrace.color=new Color(1,1,1,Mathf.Clamp01(age/2));
            }
            if(CreditsAge>38)credits.Stop();
        }
        void OnDisable()
        {
            StopAllCoroutines();if(world!=null)foreach(var a in world)if(a!=null)a.Stop();
            if(wipe!=null)wipe.Stop();if(pad!=null)pad.Stop();if(gavel!=null)gavel.Stop();if(credits!=null)credits.Stop();
        }
    }
}
