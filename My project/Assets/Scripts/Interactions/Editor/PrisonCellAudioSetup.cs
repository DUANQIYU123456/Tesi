#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrisonCellAudioSetup
{
    const string Version = "two-cell-memories-v1";
    const string Marker = "Library/XiaoduanPrisonCellAudio.completed";
    const string ReportPath = "Library/XiaoduanPrisonCellAudio.json";
    static bool running;
    static double nextCheck;
    [Serializable] sealed class CellReport
    {
        public string door, soundObject, clip, floor;
        public Vector3 doorway, sourcePosition, intoCell;
        public float duration, baseVolume, minDistance, maxDistance, spatialBlend;
        public int channels;
        public bool independentOfDoor, looping, fadesDuringScan;
    }
    [Serializable] sealed class Report
    {
        public string status, scene, backup, message;
        public CellReport[] cells;
        public bool entranceSoundRetained;
    }
    static PrisonCellAudioSetup() => EditorApplication.update += WhenReady;
    static void WhenReady()
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 3;
        if (File.Exists(Marker) && File.ReadAllText(Marker) == Version)
        { EditorApplication.update -= WhenReady; return; }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
        running = true;
        try { Setup(); }
        catch (Exception error)
        {
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { status="error", message=error.ToString() }, true));
            Debug.LogException(error);
        }
        finally { running=false; EditorApplication.update -= WhenReady; }
    }
    [MenuItem("Tools/Xiaoduan/Place Two Prison Cell Sounds")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ROOM2.unity");
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/ROOM2.unity", OpenSceneMode.Additive);
        try { Configure(scene); }
        finally { if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); }
    }
    static void Configure(Scene scene)
    {
        var all = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        string[] doors={"SM_PrisonDoor15","SM_PrisonDoor14"};
        string[] names={"CellEcho_Left_Watches","CellEcho_Right_Stamping"};
        string[] paths={"Assets/Audio/LaborMemory/Cell_Left_MultipleWatches_Prototype.wav",
                        "Assets/Audio/LaborMemory/Cell_Right_IndustrialStamp_Prototype.wav"};
        var anchors = doors.Select(name=>all.Single(t=>t.name==name)).ToArray();
        var clips = paths.Select(path=>AssetDatabase.LoadAssetAtPath<AudioClip>(path)).ToArray();
        if (clips.Any(clip=>clip==null || clip.channels!=1)) throw new InvalidOperationException("Expected two imported mono clips.");
        if (names.Any(name=>all.Count(t=>t.name==name)>1)) throw new InvalidOperationException("Duplicate cell sound objects.");
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save ROOM2 before sound placement.");
        string backup="Library/CellAudioBackups/ROOM2_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        File.Copy(scene.path,backup,false);
        Undo.IncrementCurrentGroup();
        int group=Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place two distinct prison cell memories");
        var reports=new CellReport[2];
        try
        {
            Physics.SyncTransforms();
            for (int i=0;i<2;i++)
            {
                Transform door=anchors[i];
                var behaviour=door.GetComponent<ProximityPrisonDoor>();
                if (behaviour==null) throw new InvalidOperationException("Expected automatic door on "+door.name);
                // Read the stored closed rotation, so placement never follows a temporarily open leaf.
                var serializedDoor=new SerializedObject(behaviour);
                Quaternion localClosed=serializedDoor.FindProperty("closedLocalRotation").quaternionValue;
                Quaternion closed=door.parent!=null ? door.parent.rotation*localClosed : localClosed;
                Vector3 inward=-(closed*Vector3.right).normalized;
                Vector3 center=behaviour.DoorwayCenter;
                Vector3 position=center+inward*1.8f;
                string floor="Door floor estimate";
                var ground=Physics.RaycastAll(position+Vector3.up*.4f,Vector3.down,3f,
                    Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                    .Where(hit=>hit.collider.gameObject.scene==scene && hit.normal.y>.6f)
                    .OrderBy(hit=>hit.distance).ToArray();
                if (ground.Length>0) { position.y=ground[0].point.y+.8f; floor=ground[0].collider.name; }
                else position.y=center.y-.3f;
                GameObject echo=all.FirstOrDefault(t=>t.name==names[i])?.gameObject;
                if (echo==null)
                {
                    echo=new GameObject(names[i]);
                    SceneManager.MoveGameObjectToScene(echo,scene);
                    Undo.RegisterCreatedObjectUndo(echo,"Create invisible cell sound");
                }
                Undo.SetTransformParent(echo.transform,null,"Keep sound fixed inside cell");
                Undo.RecordObject(echo.transform,"Position sound inside cell");
                echo.transform.position=position;
                AudioSource source=echo.GetComponent<AudioSource>();
                if (source==null) source=Undo.AddComponent<AudioSource>(echo);
                AudioLowPassFilter filter=echo.GetComponent<AudioLowPassFilter>();
                if (filter==null) filter=Undo.AddComponent<AudioLowPassFilter>(echo);
                LaborEchoEmitter emitter=echo.GetComponent<LaborEchoEmitter>();
                if (emitter==null) emitter=Undo.AddComponent<LaborEchoEmitter>(echo);
                Undo.RecordObjects(new UnityEngine.Object[]{source,filter,emitter},"Configure cell memory sound");
                source.clip=clips[i]; source.loop=true; source.playOnAwake=false;
                source.spatialBlend=1; source.rolloffMode=AudioRolloffMode.Linear;
                source.minDistance=1; source.maxDistance=6; source.dopplerLevel=0; source.volume=0;
                source.spread=0; source.reverbZoneMix=1;
                filter.cutoffFrequency=12000;
                emitter.baseVolume=i==0 ? .28f : .34f;
                emitter.audibleDistance=6; emitter.fadeSeconds=.8f;
                emitter.duckDuringScan=true; emitter.muffleBehindWalls=true;
                foreach (var item in new UnityEngine.Object[]{echo.transform,source,filter,emitter}) EditorUtility.SetDirty(item);
                reports[i]=new CellReport { door=door.name,soundObject=echo.name,clip=paths[i],floor=floor,
                    doorway=center,sourcePosition=position,intoCell=inward,channels=clips[i].channels,
                    duration=clips[i].length,baseVolume=emitter.baseVolume,minDistance=source.minDistance,
                    maxDistance=source.maxDistance,spatialBlend=source.spatialBlend,looping=source.loop,
                    fadesDuringScan=emitter.duckDuringScan,independentOfDoor=echo.transform.parent==null };
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save cell sounds.");
            Undo.CollapseUndoOperations(group);
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
        File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report { status="configured",scene=scene.path,
            backup=backup,cells=reports,entranceSoundRetained=all.Any(t=>t.name=="LaborEcho_RightBeforeHall"),
            message="Two original synthetic cell sound loops. Close approach fades in; scan ducks them; sources remain fixed when doors open." },true));
        File.WriteAllText(Marker,Version);
        Debug.Log("[PrisonCellAudio] Left watches and right stamping placed inside Door15 / Door14 and saved.");
    }
}
#endif
