#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SpatialScanLoopSetup
{
    const string Marker="Library/XiaoduanScanLoop.completed";
    const string Version="scan-5s-rest-15s-v1";
    static bool running;
    static double nextCheck;
    [Serializable] sealed class Report
    {
        public string status,objectName,backup,message;
        public float startDelay,scanDuration,repeatDelay,shockDuration,speed;
        public bool repeatScans;
    }
    static SpatialScanLoopSetup() => EditorApplication.update+=WhenReady;
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
            File.WriteAllText("Library/XiaoduanScanLoop.json",JsonUtility.ToJson(new Report {status="error",message=error.ToString()},true));
            Debug.LogException(error);
        }
        finally { running=false; EditorApplication.update-=WhenReady; }
    }
    [MenuItem("Tools/Xiaoduan/Set Scan 5 Seconds Repeat After 15")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        Scene scene=SceneManager.GetSceneByPath("Assets/Scenes/ROOM2.unity");
        bool opened=!scene.isLoaded;
        if (opened) scene=EditorSceneManager.OpenScene("Assets/Scenes/ROOM2.unity",OpenSceneMode.Additive);
        try
        {
            var effects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpatialScanEvent>(true)).ToArray();
            if (effects.Length!=1) throw new InvalidOperationException("Expected one hall scanner.");
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save ROOM2 before update.");
            string backup="Library/ScanLoopBackups/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);
            File.Copy(scene.path,backup+"/ROOM2.unity",false);
            // Keep a recoverable copy of the old runtime script next to the scene backup.
            if (File.Exists("Library/SpatialScanEvent_before_loop.cs.txt"))
                File.Copy("Library/SpatialScanEvent_before_loop.cs.txt",backup+"/SpatialScanEvent.cs.txt",false);
            var effect=effects[0];
            Undo.RecordObject(effect,"Set five second scan with fifteen second rest");
            effect.startDelay=2;
            effect.scanDuration=5;
            effect.repeatDelay=15;
            effect.repeatScans=true;
            effect.scanSpeed=(effect.scanDistance+effect.scanTrailLength)/effect.scanDuration;
            EditorUtility.SetDirty(effect);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save scanner loop.");
            File.WriteAllText("Library/XiaoduanScanLoop.json",JsonUtility.ToJson(new Report {
                status="configured",objectName=effect.name,backup=backup,startDelay=effect.startDelay,
                scanDuration=effect.scanDuration,repeatDelay=effect.repeatDelay,repeatScans=effect.repeatScans,
                shockDuration=effect.shockDuration,speed=effect.scanSpeed,
                message="Entry delay 2s, scan 5s, quiet interval 15s, repeat until scanner is disabled or scene unloads."},true));
            File.WriteAllText(Marker,Version);
            Debug.Log("[SpatialScan] Saved 5 second scan, 15 second quiet interval, repeating.");
        }
        finally { if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene,true); }
    }
}
#endif
