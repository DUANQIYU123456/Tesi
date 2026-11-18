#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Xiaoduan.Room3;
public static class Room3SoundscapePass
{
    const string F="Assets/Room3/WitnessSound/";
    static AudioClip Clip(string n)=>AssetDatabase.LoadAssetAtPath<AudioClip>(F+"Audio/"+n+".wav");
    static AudioSource Source(string name,AudioClip clip,bool loop,Transform parent,Vector3 position,bool spatial=false)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=position;
        var a=t.gameObject.AddComponent<AudioSource>();a.clip=clip;a.loop=loop;a.playOnAwake=false;a.volume=0;a.spatialBlend=spatial?1:0;a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=2.5f;a.maxDistance=22;a.dopplerLevel=0;a.reverbZoneMix=spatial?.45f:0;a.priority=loop?160:64;return a;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        var flow=UnityEngine.Object.FindFirstObjectByType<Room3StoryFlow>();var x=flow.embodied;
        var old=flow.transform.Find("ROOM3 composed soundscape");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var sound=flow.GetComponent<Room3Soundscape>();if(sound==null)sound=flow.gameObject.AddComponent<Room3Soundscape>();sound.flow=flow;flow.soundscape=sound;
        var root=new GameObject("ROOM3 composed soundscape").transform;root.SetParent(flow.transform,false);
        AudioSource A(string label,string clip,bool loop=true,bool spatial=false,Vector3? p=null)=>Source(label,Clip(clip),loop,root,p??Vector3.zero,spatial);
        sound.alarm=A("01 - muffled industrial alarm","Muffled pressure alarm");sound.alarm.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=760;
        sound.leak=A("01 - pressure leakage and falling metal","Falling door pressure",true,true,x.door.position);
        sound.wind=A("02 - cold abyss wind","Abyss cold wind");sound.drone=A("02 - sub pressure without melody","Abyss sub pressure");
        sound.gear=A("01 - forced gear per stroke","Valve forced gear",false,true,x.valve.position);sound.gearClip=Clip("Valve forced gear");
        sound.latch=A("01 - latch and sudden alarm cutoff","Door latch and exhaust",false,true,x.door.position);sound.latchClip=Clip("Door latch and exhaust");
        sound.sonar=A("02 - submerged isotope wave","Isotope submerged pulse",false);sound.sonarClip=Clip("Isotope submerged pulse");
        sound.radiation=A("02 - revealed radiation grains","Isotope revealed clicks",false);sound.radiationClip=Clip("Isotope revealed clicks");
        sound.bridge=A("03 - concrete reconstruction boom","Concrete assembly boom",false);sound.bridgeClip=Clip("Concrete assembly boom");
        sound.settling=A("03 - stone friction and long tail","Stone settling",false,true,new Vector3(0,0,17));sound.settlingClip=Clip("Stone settling");
        sound.watch=A("04 - focused antique tick","Antique watch close tick");sound.cough=A("04 - recorded female cough","Female cough recorded",false);sound.coughClip=Clip("Female cough recorded");
        sound.fan=A("04 - focused compute cooling","Compute cooling fan");sound.clicks=A("04 - focused irregular mouse work","Compute irregular clicks");
        sound.riser=A("04 - resistance pitch follows effort","Oath resistance riser");sound.heart=A("04 - heart and bone pressure","Oath heart and bone pressure");
        sound.organ=A("04 - pipe organ released only after oath","Testimony pipe organ");sound.choir=A("04 - airy female choir released after oath","Testimony airy female choir");
        sound.wipe=A("05 - FORMATTA cold wipe","Format cold digital wipe");sound.pad=A("05 - CUSTODISCI major chord","Preserve warm major chord");sound.gavel=A("05 - court gavel","Court gavel echo",false);sound.gavelClip=Clip("Court gavel echo");
        sound.credits=A("05 - last antique watch and fade","Antique watch close tick");
        var reverbOld=GameObject.Find("Abyss acoustic volume");if(reverbOld!=null)UnityEngine.Object.DestroyImmediate(reverbOld);
        var zone=new GameObject("Abyss acoustic volume");zone.transform.position=new Vector3(0,8,47);var reverb=zone.AddComponent<AudioReverbZone>();reverb.reverbPreset=AudioReverbPreset.Cave;reverb.reverbPreset=AudioReverbPreset.User;reverb.minDistance=62;reverb.maxDistance=92;reverb.decayTime=4.2f;reverb.room=-1700;reverb.reverb=-1200;sound.abyssReverb=reverb;
        foreach(var a in new[]{flow.clockSource,flow.clickSource,flow.geigerSource,flow.heartbeatSource}){a.Stop();a.volume=0;a.playOnAwake=false;}
        foreach(var a in UnityEngine.Object.FindObjectsByType<AudioSource>(UnityEngine.FindObjectsSortMode.None))if(a.name=="Archive air and distant clock"){a.Stop();a.volume=0;a.playOnAwake=false;}
        foreach(var id in AssetDatabase.FindAssets("t:AudioClip",new[]{F+"Audio"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(id);var importer=(AudioImporter)AssetImporter.GetAtPath(path);var s=importer.defaultSampleSettings;s.loadType=AudioClipLoadType.DecompressOnLoad;s.compressionFormat=AudioCompressionFormat.Vorbis;s.quality=.85f;s.preloadAudioData=true;importer.defaultSampleSettings=s;importer.SaveAndReimport();
        }
        EditorUtility.SetDirty(flow);EditorUtility.SetDirty(sound);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);EditorSceneManager.SaveScene(flow.gameObject.scene);
        return "21 authored clips, 22 AudioSources, Cave reverb, event-driven phase transitions; previous tonal bed disabled";
    }
}
#endif
