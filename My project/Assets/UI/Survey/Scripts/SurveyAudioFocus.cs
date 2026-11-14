using System.Collections.Generic;
using UnityEngine;
using Xiaoduan.Room3;
namespace Xiaoduan.Survey
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SurveyAudioFocus:MonoBehaviour
    {
        sealed class Voice{public AudioSource source;public float dry,applied;public bool modified;}
        readonly List<Voice> voices=new List<Voice>();
        float refresh,gain=1;
        Room3StoryFlow story;
        public float WorldGain=>gain;
        void Restore()
        {
            foreach(var voice in voices)
                if(voice.source!=null&&voice.modified&&Mathf.Abs(voice.source.volume-voice.applied)<.0001f)
                {voice.source.volume=voice.dry;voice.modified=false;}
        }
        void Update()
        {
            Restore();
            if(Time.unscaledTime<refresh)return;refresh=Time.unscaledTime+1;
            voices.RemoveAll(v=>v.source==null);story=FindFirstObjectByType<Room3StoryFlow>();
            foreach(var source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if(source.transform.root==transform.root||(!source.loop&&source.spatialBlend<.1f&&(source.clip==null||!source.clip.name.Contains("MRI"))))continue;
                if(!voices.Exists(v=>v.source==source))voices.Add(new Voice{source=source});
            }
        }
        void LateUpdate()
        {
            var hud=SurveyHUD.Instance;
            bool reading=hud!=null&&hud.IsReading||story!=null&&(story.Reading||story.ui.DecisionVisible);
            gain=Mathf.MoveTowards(gain,reading?.35f:1,Time.unscaledDeltaTime*2.5f);
            foreach(var voice in voices)
            {
                if(voice.source==null)continue;
                voice.dry=voice.source.volume;voice.applied=voice.dry*gain;
                voice.source.volume=voice.applied;voice.modified=true;
            }
        }
        void OnDisable(){Restore();}
    }
}
