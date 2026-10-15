#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NarrativeUIValidation
{
    const string Request="Library/XiaoduanNarrativeUITestV3.request";
    static NarrativeUIValidation()
    {
        EditorApplication.update+=Ready;
        EditorApplication.playModeStateChanged+=Changed;
    }
    static void Ready()
    {
        if (!File.Exists(Request) || !File.Exists("Library/XiaoduanNarrativeUI.completed") ||
            File.ReadAllText("Library/XiaoduanNarrativeUI.completed")!="narrative-ui-english-v2" || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("XiaoduanUITestRunning",false)) return;
        File.Delete(Request);
        SessionState.SetBool("XiaoduanUITestRunning",true);
        var gameView=Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameView!=null) EditorWindow.GetWindow(gameView).Show();
        EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool("XiaoduanUITestRunning",false)) return;
        if (state==PlayModeStateChange.EnteredPlayMode)
            new GameObject("Narrative Integration Test (temporary)").AddComponent<NarrativeUIValidationDriver>();
        if (state==PlayModeStateChange.EnteredEditMode) SessionState.SetBool("XiaoduanUITestRunning",false);
    }
}
#endif
