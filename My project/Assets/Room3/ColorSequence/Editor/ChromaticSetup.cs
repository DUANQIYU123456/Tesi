#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Xiaoduan.Room3;
public static class ChromaticSetup
{
    const string F="Assets/Room3/ColorSequence/";
    static Transform root;
    static Transform Group(string n){var t=new GameObject(n).transform;t.SetParent(root,false);return t;}
    static Material Material(string name,string shader)
    {var p=F+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find(shader)){name=name};AssetDatabase.CreateAsset(m,p);}return m;}
    static Light Light(string n,Vector3 p,Color c,float intensity,float range,LightType type,Vector3 target)
    {
        var t=Group(n);t.position=p;t.rotation=Quaternion.LookRotation(target-p);var l=t.gameObject.AddComponent<Light>();l.type=type;l.color=c;l.intensity=intensity;l.range=range;l.shadows=LightShadows.Soft;l.shadowBias=.03f;l.shadowNormalBias=.15f;
        if(type==LightType.Spot){l.spotAngle=66;l.innerSpotAngle=42;}
        var vol=t.gameObject.AddComponent<VolumetricAdditionalLight>();vol.Scattering=type==LightType.Point?.5f:1;vol.Anisotropy=.25f;vol.Radius=.45f;return l;
    }
    static Renderer Beam()
    {
        var vs=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();
        for(int i=0;i<=72;i++){float a=i*Mathf.PI*2/72;for(int j=0;j<2;j++){float r=j==0?4.65f:.22f;vs.Add(new Vector3(Mathf.Cos(a)*r,j==0?.15f:8.05f,Mathf.Sin(a)*r));uv.Add(new Vector2(i/72f,j));}if(i<72){int k=i*2;ts.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}}
        var mesh=new Mesh{name="Open clinical light cone"};mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,F+"Clinical cone.asset");
        var t=Group("Clinical scattering silhouette");t.position=new Vector3(0,0,40.6f);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r0=t.gameObject.AddComponent<MeshRenderer>();r0.sharedMaterial=Material("Clinical cyan scattering","Xiaoduan/Room3/Clinical beam");r0.shadowCastingMode=ShadowCastingMode.Off;return r0;
    }
    static ParticleSystem Particles(string name,Vector3 p,Vector3 box,float size,Color color,Material mat)
    {
        var t=Group(name);t.position=p;var ps=t.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=ps.main;m.playOnAwake=false;m.loop=true;m.duration=5;m.startLifetime=3;m.startSpeed=.08f;m.startSize=new ParticleSystem.MinMaxCurve(size*.7f,size*1.2f);m.startColor=color;m.maxParticles=size>1?90:80;m.simulationSpace=ParticleSystemSimulationSpace.World;
        var s=ps.shape;s.shapeType=ParticleSystemShapeType.Box;s.scale=box;var e=ps.emission;e.rateOverTime=5;
        var a=ps.colorOverLifetime;a.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.22f),new GradientAlphaKey(.7f,.65f),new GradientAlphaKey(0,1)});a.color=g;
        var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=mat;r.renderMode=ParticleSystemRenderMode.Billboard;r.shadowCastingMode=ShadowCastingMode.Off;return ps;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before setup");var f=UnityEngine.Object.FindFirstObjectByType<Room3StoryFlow>();if(f==null)throw new Exception("Open ROOM3");if(GameObject.Find("ROOM3 - Four chromatic thresholds")!=null)throw new Exception("Already installed");
        Directory.CreateDirectory(F+"Materials");Directory.CreateDirectory(F+"Profiles");AssetDatabase.Refresh();root=new GameObject("ROOM3 - Four chromatic thresholds").transform;
        var sequence=root.gameObject.AddComponent<Room3ChromaticSequence>();sequence.flow=f;sequence.entryZ=f.embodied.door.position.z;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Four thresholds - controlled exposure";AssetDatabase.CreateAsset(profile,F+"Profiles/Four thresholds.asset");
        var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.value=.05f;grade.contrast.value=18;grade.saturation.value=-16;
        var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.34f;bloom.threshold.value=1.15f;bloom.scatter.value=.65f;bloom.clamp.value=8;
        var fog=profile.Add<VolumetricFogVolumeComponent>(true);fog.enabled.value=true;fog.density.value=.004f;fog.distance.value=85;fog.maximumHeight.value=14;fog.attenuationDistance.value=105;fog.enableAdditionalLightsContribution.value=true;fog.enableMainLightContribution.value=false;fog.maxSteps.value=64;fog.blurIterations.value=2;
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        var v=root.gameObject.AddComponent<Volume>();v.isGlobal=true;v.priority=80;v.weight=1;v.sharedProfile=profile;sequence.sequenceVolume=v;
        var cold=new Color(224/255f,1,1);var bone=new Color(245/255f,245/255f,220/255f);var green=new Color(57/255f,1,20/255f);
        sequence.entryGreen=Light("Radium threshold scattering",new Vector3(0,2,-13.5f),green,0,12,LightType.Point,Vector3.zero);sequence.entryGreen.shadows=LightShadows.None;
        sequence.clinicalDown=Light("Clinical examination / overhead",new Vector3(0,8.1f,40.6f),cold,300,14,LightType.Spot,new Vector3(0,0,40.6f));sequence.clinicalDown.spotAngle=70;sequence.clinicalDown.innerSpotAngle=48;
        sequence.clinicalFill=Light("Clinical bounce / low fill",new Vector3(-3.2f,3.3f,35.5f),cold,14,11,LightType.Spot,new Vector3(0,1.7f,41));sequence.clinicalFill.GetComponent<VolumetricAdditionalLight>().Scattering=.15f;
        sequence.goldBack=Light("Bone white / terminal counterlight",new Vector3(0,8,122),bone,0,105,LightType.Spot,new Vector3(0,2,36));sequence.goldBack.spotAngle=48;sequence.goldBack.innerSpotAngle=30;sequence.goldBack.GetComponent<VolumetricAdditionalLight>().Scattering=.32f;
        sequence.goldCore=Light("Warm core rim",new Vector3(0,8,119),bone,0,42,LightType.Point,Vector3.zero);sequence.goldCore.GetComponent<VolumetricAdditionalLight>().Scattering=.25f;
        sequence.clinicalBeam=Beam();
        var field=GameObject.CreatePrimitive(PrimitiveType.Quad);field.name="Sacred bone field / behind terminal";field.transform.SetParent(root,false);field.transform.position=new Vector3(0,8.5f,126);field.transform.localScale=new Vector3(28,28,1);UnityEngine.Object.DestroyImmediate(field.GetComponent<Collider>());var renderer=field.GetComponent<Renderer>();renderer.sharedMaterial=Material("Bone and gold radiance","Xiaoduan/Room3/Sacred field");renderer.shadowCastingMode=ShadowCastingMode.Off;sequence.sacredField=renderer;
        var mistMat=Material("Soft local isotope mist","Xiaoduan/Room3/Local mist");
        sequence.entryMist=Particles("Ten metre threshold mist",new Vector3(0,2.5f,sequence.entryZ+5),new Vector3(11,4,9),3.5f,new Color(green.r,1,green.b,.11f),mistMat);
        sequence.clinicalDust=Particles("Clinical dust / isotope residue",new Vector3(0,3.5f,40.6f),new Vector3(7,5,6),.035f,new Color(.7f,1,.86f,.35f),mistMat);var dm=sequence.clinicalDust.main;dm.playOnAwake=true;
        var st=Group("Threshold pressure release");st.position=new Vector3(0,2,sequence.entryZ+3);sequence.steam=st.gameObject.AddComponent<AudioSource>();sequence.steam.clip=f.soundscape.leak.clip;sequence.steam.loop=true;sequence.steam.playOnAwake=false;sequence.steam.spatialBlend=.75f;sequence.steam.minDistance=2;sequence.steam.maxDistance=14;sequence.steam.rolloffMode=AudioRolloffMode.Linear;
        sequence.oldClinical=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>new[]{"Cold portrait key","Quiet portrait fill","Upper silhouettes cool rim"}.Contains(l.name)).ToArray();sequence.clinicalPowers=sequence.oldClinical.Select(l=>l.intensity).ToArray();
        sequence.oldCore=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>new[]{"Core left rim","Core right rim","Core green reflected light"}.Contains(l.name)).ToArray();sequence.corePowers=sequence.oldCore.Select(l=>l.intensity).ToArray();
        foreach(var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional||l.name.StartsWith("Crimson upper rim"))l.enabled=false;
        foreach(var n in new[]{"Soft testimony light volume","Pale archive axis"}){var g=GameObject.Find(n);if(g!=null)g.SetActive(false);}
        var atmosphere=UnityEngine.Object.FindFirstObjectByType<RocketboxAtmosphere>();sequence.oldCeiling=atmosphere.ceiling;atmosphere.enabled=false;
        var corePulse=UnityEngine.Object.FindFirstObjectByType<Room3CorePulse>();sequence.coreEdges=corePulse.edges;corePulse.enabled=false;
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.ambientSkyColor=Color.black;RenderSettings.ambientEquatorColor=Color.black;RenderSettings.ambientGroundColor=Color.black;RenderSettings.reflectionIntensity=.08f;RenderSettings.fog=false;
        foreach(var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)){var d=c.GetUniversalAdditionalCameraData();d.renderPostProcessing=true;d.volumeLayerMask=~0;c.allowHDR=true;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;}
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(f.gameObject.scene);EditorSceneManager.SaveScene(f.gameObject.scene);
        return "Four thresholds installed; physical volumetric lights, local entry mist, clinical examination and choice-aware bone counterlight";
    }
}
#endif
