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

/// <summary>Applies the finer luminous scan look to the already configured entrance.</summary>
[InitializeOnLoad]
public static class SpatialScanVisualTuning
{
    const string Version = "scan-thin-soft-bloom-v2";
    const string Marker = "Library/XiaoduanScanVisualTuning.completed";
    const string ReportPath = "Library/XiaoduanScanVisualTuning.json";
    static double nextCheck;
    static bool running;
    [Serializable] sealed class Report
    {
        public string status, backup, message;
        public float oldThickness, thickness, gridSize, tail, bloomIntensity, startDelay, shockDuration;
        public bool shaderSupported, hdrEnabled;
        public string[] shaderErrors, rendererInputs;
    }
    static SpatialScanVisualTuning() => EditorApplication.update += WhenReady;
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
        try { Apply(); }
        catch (Exception error)
        {
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report { status = "error", message = error.ToString() },true));
            Debug.LogException(error);
        }
        finally { running = false; EditorApplication.update -= WhenReady; }
    }

    [MenuItem("Tools/Xiaoduan/Apply Fine Glowing Scan Look")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play before applying the scan look.");
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/ROOM2.unity");
        bool openedForTuning = !scene.isLoaded;
        if (openedForTuning) scene = EditorSceneManager.OpenScene("Assets/Scenes/ROOM2.unity",OpenSceneMode.Additive);
        try { ApplyToScene(scene); }
        finally { if (openedForTuning && scene.isLoaded) EditorSceneManager.CloseScene(scene,true); }
    }

    static void ApplyToScene(Scene scene)
    {
        var effects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SpatialScanEvent>(true)).ToArray();
        if (effects.Length != 1) throw new InvalidOperationException("Expected one configured scan entrance.");
        SpatialScanEvent effect = effects[0];
        Shader shader = Shader.Find("Xiaoduan/Spatial Scan URP");
        if (shader == null) throw new InvalidOperationException("Scan shader is not imported.");
        string[] errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
        if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n",errors));
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null || !pipeline.supportsHDR) throw new InvalidOperationException("Scan glow requires the existing URP HDR mode.");
        var paths = AssetDatabase.FindAssets("t:UniversalRendererData",new[] { "Assets/Settings" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save ROOM2 before tuning.");
        string backup = "Library/ScanVisualBackups/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(backup);
        File.Copy(scene.path,backup + "/ROOM2.unity",false);
        foreach (string path in paths) File.Copy(path,backup + "/" + Path.GetFileName(path),false);
        float originalThickness = effect.gridThickness;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Finer luminous scan lines");
        Undo.RecordObject(effect,"Tune luminous scan grid");
        effect.gridThickness = 0.004f;
        effect.gridSize = 0.85f;
        effect.scanBandWidth = 1f;
        effect.scanTrailLength = 5f;
        effect.scanColor = new Color(0.08f,1.2f,0.28f);
        effect.scanBloomIntensity = 0.65f;
        EditorUtility.SetDirty(effect);
        var inputs = new System.Collections.Generic.List<string>();
        foreach (string path in paths)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var feature = renderer.rendererFeatures.OfType<SpatialScanRendererFeature>().FirstOrDefault();
            if (feature == null) throw new InvalidOperationException("Scan feature missing: " + path);
            Undo.RecordObject(feature,"Use stable surface normals for scan lines");
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            EditorUtility.SetDirty(feature);
            renderer.SetDirty();
            inputs.Add(path + ": " + feature.requirements);
        }
        Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scan tuning couldn't save ROOM2.");
        errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
        File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report { status = "configured", backup = backup,
            oldThickness = originalThickness, thickness = effect.gridThickness, gridSize = effect.gridSize,
            tail = effect.scanTrailLength, bloomIntensity = effect.scanBloomIntensity,
            startDelay = effect.startDelay, shockDuration = effect.shockDuration,
            hdrEnabled = pipeline.supportsHDR, shaderSupported = shader.isSupported,
            shaderErrors = errors, rendererInputs = inputs.ToArray(),
            message = "Fine antialiased cores, soft halos, temporary Bloom, blended surface projections and dimmer scan trails." },true));
        File.WriteAllText(Marker,Version);
        Debug.Log("[SpatialScan] Fine glowing scan look saved; trigger and impact timing preserved.");
    }
}
#endif
