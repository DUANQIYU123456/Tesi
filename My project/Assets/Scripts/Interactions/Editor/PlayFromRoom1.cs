#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Starts the actual gameplay sequence from ROOM1 even while editing ROOM2.</summary>
[InitializeOnLoad]
public static class PlayFromRoom1
{
    const string StartPath = "Assets/Scenes/ROOM1.unity";
    const string ReportPath = "Library/XiaoduanPlayStart.json";
    static bool applied;

    [Serializable] sealed class Report
    {
        public string startScene;
        public string editingScene;
        public bool room1EnabledInBuild;
        public bool room2EnabledInBuild;
        public Vector3 room2Arrival;
        public Vector3 potionGroupCenter;
        public int potionCount;
        public int room2PlayerCount;
        public string message;
    }

    static PlayFromRoom1()
    {
        EditorApplication.update += ApplyWhenReady;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void ApplyWhenReady()
    {
        if (applied || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;
        SetStartScene();
    }

    [MenuItem("Tools/Xiaoduan/Play Starts From ROOM1")]
    public static void SetStartScene()
    {
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartPath);
        if (scene == null)
        {
            Debug.LogError("[PlayFromRoom1] ROOM1 is missing: " + StartPath);
            applied = true;
            return;
        }
        EditorSceneManager.playModeStartScene = scene;
        applied = true;
        var report = new Report { startScene = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            editingScene = SceneManager.GetActiveScene().path,
            message = "Editor Play starts at ROOM1. Stop Play returns to the scene being edited. ROOM2 uses the player carried by the portal." };
        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.enabled && buildScene.path == StartPath) report.room1EnabledInBuild = true;
            if (buildScene.enabled && buildScene.path == "Assets/Scenes/ROOM2.unity") report.room2EnabledInBuild = true;
        }
        Scene room2 = SceneManager.GetSceneByName("ROOM2");
        if (room2.isLoaded)
        {
            foreach (GameObject root in room2.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    if (item.CompareTag("Player")) report.room2PlayerCount++;
                foreach (var marker in root.GetComponentsInChildren<TransitionDestination>(true))
                    if (marker.destinationTag == TransitionDestination.DestinationTag.ENTER)
                        report.room2Arrival = marker.transform.position;
                foreach (var potion in root.GetComponentsInChildren<ProximityPotionGlow>(true))
                {
                    report.potionGroupCenter += potion.transform.position;
                    report.potionCount++;
                }
            }
            if (report.potionCount > 0) report.potionGroupCenter /= report.potionCount;
        }
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log("[PlayFromRoom1] Configured: Play starts in ROOM1, then use the portal to ROOM2.");
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            applied = false;
            EditorApplication.delayCall += SetStartScene;
        }
        if (state == PlayModeStateChange.EnteredPlayMode)
            Debug.Log("[PlayFromRoom1] Running " + SceneManager.GetActiveScene().name);
    }
}
#endif
