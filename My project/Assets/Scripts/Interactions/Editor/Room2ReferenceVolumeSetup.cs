#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Room2ReferenceVolumeSetup
{
    const string Marker="Library/XiaoduanRoom2ReferenceVolume.completed";
    const string Version="backrooms-exact-profile-v1";
    const string ProfilePath="Assets/Settings/Room2Reference/ROOM2_BackroomsProfile.asset";
    static bool running;
    static double nextCheck;
    [Serializable] sealed class Report
    {
        public string status,objectName,profilePath,backup,message;
        public string[] effects,disabledOldVolumes,cameras,renderers;
        public float exposure,saturation,distortion,chromatic,bloomIntensity,bloomThreshold,vignetteIntensity,vignetteSmoothness,priority,weight;
        public Color colorFilter;
        public Vector4 shadows,midtones,highlights;
        public string tonemapping;
        public bool global,hdr,textureReferenceValid;
    }
    static Room2ReferenceVolumeSetup() => EditorApplication.update+=WhenReady;
    static void WhenReady()
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.timeSinceStartup<nextCheck) return;
        nextCheck=EditorApplication.timeSinceStartup+3;
        if (File.Exists(Marker) && File.ReadAllText(Marker)==Version)
        { EditorApplication.update-=WhenReady; return; }
        if (PrefabStageUtility.GetCurrentPrefabStage()!=null) return;
        running=true;
        try { Configure(); }
        catch (Exception error)
        {
            File.WriteAllText("Library/XiaoduanRoom2ReferenceVolume.json",JsonUtility.ToJson(new Report {status="error",message=error.ToString()},true));
            Debug.LogException(error);
        }
        finally { running=false; EditorApplication.update-=WhenReady; }
    }
    [MenuItem("Tools/Xiaoduan/Add Screenshot Volume To ROOM2")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        var urp=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp==null || !urp.supportsHDR) throw new InvalidOperationException("Expected current URP with HDR enabled.");
        var rendererPaths=AssetDatabase.FindAssets("t:UniversalRendererData",new[]{"Assets/Settings"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        foreach (string path in rendererPaths)
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var property=new SerializedObject(renderer).FindProperty("postProcessData");
            if (property==null || property.objectReferenceValue==null) throw new InvalidOperationException("Missing renderer post-process data: "+path);
        }
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile==null || profile.components.Count!=7 || profile.components.Any(c=>c==null))
            throw new InvalidOperationException("Expected exact imported Backrooms profile with seven effects.");
        if (!profile.TryGet<ColorAdjustments>(out var color) || !profile.TryGet<ShadowsMidtonesHighlights>(out var smh) ||
            !profile.TryGet<LensDistortion>(out var distortion) || !profile.TryGet<ChromaticAberration>(out var chromatic) ||
            !profile.TryGet<Bloom>(out var bloom) || !profile.TryGet<Vignette>(out var vignette) || !profile.TryGet<Tonemapping>(out var tone))
            throw new InvalidOperationException("Reference profile effects missing.");
        if (!Mathf.Approximately(color.postExposure.value,.48f) || !Mathf.Approximately(color.saturation.value,24f) ||
            !Mathf.Approximately(distortion.intensity.value,.218f) || !Mathf.Approximately(chromatic.intensity.value,.233f))
            throw new InvalidOperationException("Imported values differ from screenshots.");
        if (bloom.dirtTexture.value==null) throw new InvalidOperationException("Reference dirt texture missing.");
        Scene scene=SceneManager.GetSceneByPath("Assets/Scenes/ROOM2.unity");
        bool opened=!scene.isLoaded;
        if (opened) scene=EditorSceneManager.OpenScene("Assets/Scenes/ROOM2.unity",OpenSceneMode.Additive);
        try
        {
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            const string name="ROOM2 Reference Global Volume";
            if (all.Count(t=>t.name==name)>1) throw new InvalidOperationException("Duplicate reference Volume objects.");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save ROOM2 before setup.");
            string backup="Library/Room2VolumeBackups/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path,backup+"/ROOM2.unity",false);
            Undo.IncrementCurrentGroup();
            int group=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply screenshot Volume to ROOM2");
            GameObject go=all.FirstOrDefault(t=>t.name==name)?.gameObject;
            if (go==null)
            {
                go=new GameObject(name);
                SceneManager.MoveGameObjectToScene(go,scene);
                Undo.RegisterCreatedObjectUndo(go,"Create ROOM2 reference Volume");
            }
            Undo.RecordObject(go,"Use Default Volume layer");
            go.layer=0;
            Volume volume=go.GetComponent<Volume>();
            if (volume==null) volume=Undo.AddComponent<Volume>(go);
            Undo.RecordObject(volume,"Assign exact Backrooms profile");
            volume.enabled=true; volume.isGlobal=true; volume.weight=1; volume.priority=0; volume.sharedProfile=profile;
            if (go.GetComponent<Room2VolumeCameraSetup>()==null) Undo.AddComponent<Room2VolumeCameraSetup>(go);
            var old=all.Select(t=>t.GetComponent<Volume>()).Where(v=>v!=null && v!=volume && v.enabled).ToArray();
            foreach (var v in old)
            {
                Undo.RecordObject(v,"Keep original look available but prevent grading overlap");
                v.enabled=false;
                EditorUtility.SetDirty(v);
                if (PrefabUtility.IsPartOfPrefabInstance(v)) PrefabUtility.RecordPrefabInstancePropertyModifications(v);
            }
            var cameras=all.Select(t=>t.GetComponent<Camera>()).Where(c=>c!=null).ToArray();
            var cameraReport=new System.Collections.Generic.List<string>();
            foreach (var camera in cameras)
            {
                var data=camera.GetComponent<UniversalAdditionalCameraData>();
                if (data==null || data.renderType!=CameraRenderType.Base) continue;
                Undo.RecordObjects(new UnityEngine.Object[]{camera,data},"Enable ROOM2 camera post processing");
                camera.allowHDR=true; data.renderPostProcessing=true; data.volumeLayerMask|=1;
                EditorUtility.SetDirty(camera); EditorUtility.SetDirty(data);
                if (PrefabUtility.IsPartOfPrefabInstance(camera)) PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                if (PrefabUtility.IsPartOfPrefabInstance(data)) PrefabUtility.RecordPrefabInstancePropertyModifications(data);
                cameraReport.Add(camera.name+": post=true, HDR=true, includes Default");
            }
            EditorUtility.SetDirty(volume);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save ROOM2 Volume.");
            Selection.activeGameObject=go;
            File.WriteAllText("Library/XiaoduanRoom2ReferenceVolume.json",JsonUtility.ToJson(new Report {
                status="configured",objectName=go.name,profilePath=ProfilePath,backup=backup,
                effects=profile.components.Select(c=>c.GetType().Name).ToArray(),
                disabledOldVolumes=old.Select(v=>v.name).ToArray(),cameras=cameraReport.ToArray(),renderers=rendererPaths,
                exposure=color.postExposure.value,saturation=color.saturation.value,colorFilter=color.colorFilter.value,
                shadows=smh.shadows.value,midtones=smh.midtones.value,highlights=smh.highlights.value,
                distortion=distortion.intensity.value,chromatic=chromatic.intensity.value,
                bloomIntensity=bloom.intensity.value,bloomThreshold=bloom.threshold.value,
                vignetteIntensity=vignette.intensity.value,vignetteSmoothness=vignette.smoothness.value,
                tonemapping=tone.mode.value.ToString(),priority=volume.priority,weight=volume.weight,
                global=volume.isGlobal,hdr=urp.supportsHDR,textureReferenceValid=bloom.dirtTexture.value!=null,
                message="Exact source profile copied with override flags and texture dependency. Old ROOM2 volumes retained disabled. Carried gameplay camera configured at runtime."},true));
            File.WriteAllText(Marker,Version);
            Debug.Log("[ROOM2 Volume] Exact screenshot profile applied and saved.");
        }
        finally { if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene,true); }
    }
}
#endif
