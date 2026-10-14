#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LaborEchoSetup
{
    const string Version = "right-before-hall-labor-v1";
    const string Marker = "Library/XiaoduanLaborEcho.completed";
    const string ReportPath = "Library/XiaoduanLaborEcho.json";
    static bool running;
    static double nextCheck;
    [Serializable] sealed class Report
    {
        public string status, objectName, scene, backup, floor, message;
        public Vector3 position, entrancePosition;
        public float rightOffset, beforeDoorOffset, duration, spatialBlend, minDistance, maxDistance, baseVolume;
        public int channels;
    }
    static LaborEchoSetup() => EditorApplication.update += WhenReady;
    static void WhenReady()
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 3;
        if (File.Exists(Marker) && File.ReadAllText(Marker) == Version)
        {
            EditorApplication.update -= WhenReady;
            return;
        }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        running = true;
        try { Setup(); }
        catch (Exception error)
        {
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report { status = "error",message = error.ToString() },true));
            Debug.LogException(error);
        }
        finally { running = false; EditorApplication.update -= WhenReady; }
    }

    [MenuItem("Tools/Xiaoduan/Place Labor Echo Right Before Hall")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ROOM2.unity");
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ROOM2.unity",OpenSceneMode.Additive);
        try { Configure(scene); }
        finally { if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene,true); }
    }

    static void Configure(Scene scene)
    {
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var anchors = objects.Where(item => item.name == "FinalHallScanTrigger").ToArray();
        if (anchors.Length != 1) throw new InvalidOperationException("Expected the existing FinalHallScanTrigger.");
        Transform anchor = anchors[0];
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LaborMemory/Labor_BrushAndContainer_Prototype.wav");
        if (clip == null) throw new InvalidOperationException("Labor sound has not imported.");
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save ROOM2 before placement.");
        string backup = "Library/LaborEchoBackups/ROOM2_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        File.Copy(scene.path,backup,false);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place labor sound right before the hall");
        var existing = objects.Where(item => item.name == "LaborEcho_RightBeforeHall").ToArray();
        if (existing.Length > 1) throw new InvalidOperationException("Duplicate labor sound objects.");
        GameObject echo;
        if (existing.Length == 1) echo = existing[0].gameObject;
        else
        {
            echo = new GameObject("LaborEcho_RightBeforeHall");
            SceneManager.MoveGameObjectToScene(echo,scene);
            Undo.RegisterCreatedObjectUndo(echo,"Create hidden labor sound source");
        }
        Vector3 right = anchor.right.normalized;
        Vector3 forward = anchor.forward.normalized;
        // Scan trigger is 1.8 metres inside the door; put this source 3.2 metres before the door.
        Vector3 position = anchor.position - forward * 5f + right * 4f;
        string floor = "Expected entrance floor height";
        Physics.SyncTransforms();
        var ground = Physics.RaycastAll(position + Vector3.up * 0.4f,Vector3.down,4f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
            .Where(hit => hit.collider.gameObject.scene == scene && hit.normal.y > 0.6f)
            .OrderBy(hit => hit.distance).ToArray();
        if (ground.Length > 0)
        {
            position.y = ground[0].point.y + 0.75f;
            floor = ground[0].collider.name;
        }
        else position.y -= 0.35f;
        Undo.RecordObject(echo.transform,"Position sound before entrance on right");
        echo.transform.position = position;
        AudioSource source = echo.GetComponent<AudioSource>();
        if (source == null) source = Undo.AddComponent<AudioSource>(echo);
        Undo.RecordObject(source,"Configure localized labor sound");
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = 6f;
        source.dopplerLevel = 0f;
        source.volume = 0f;
        AudioLowPassFilter filter = echo.GetComponent<AudioLowPassFilter>();
        if (filter == null) filter = Undo.AddComponent<AudioLowPassFilter>(echo);
        Undo.RecordObject(filter,"Configure muffled sound through obstacles");
        filter.cutoffFrequency = 12000f;
        LaborEchoEmitter emitter = echo.GetComponent<LaborEchoEmitter>();
        if (emitter == null) emitter = Undo.AddComponent<LaborEchoEmitter>(echo);
        Undo.RecordObject(emitter,"Configure approach and scan sound fading");
        emitter.baseVolume = 0.3f;
        emitter.audibleDistance = 6f;
        emitter.duckDuringScan = true;
        foreach (UnityEngine.Object item in new UnityEngine.Object[] { echo.transform,source,filter,emitter }) EditorUtility.SetDirty(item);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save labor sound placement.");
        File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report { status = "configured",objectName = echo.name,
            scene = scene.path,backup = backup,position = position,entrancePosition = anchor.position,
            rightOffset = Vector3.Dot(position - anchor.position,right),beforeDoorOffset = 3.2f,
            floor = floor,channels = clip.channels,duration = clip.length,spatialBlend = source.spatialBlend,
            minDistance = source.minDistance,maxDistance = source.maxDistance,baseVolume = emitter.baseVolume,
            message = "One original synthetic labor-memory sound source, right before hall; fades by distance and during scan." },true));
        File.WriteAllText(Marker,Version);
        Debug.Log("[LaborEcho] Sound placed and saved on right before hall at " + position);
    }
}
#endif
