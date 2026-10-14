#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SpatialScanSetup
{
    const string Version = "spatial-scan-20261014-v1";
    const string Marker = "Library/XiaoduanSpatialScan.completed";
    const string ReportPath = "Library/XiaoduanSpatialScan.json";
    const string MaterialPath = "Assets/Materials/SpatialScan/SpatialScan.mat";
    static bool running;
    static double nextCheck;

    [Serializable] sealed class Report
    {
        public string status, scene, entranceDoor, backup;
        public Vector3 triggerPosition, scanDirection;
        public string[] renderers, shaderErrors, checks;
        public bool shaderSupported, audioAssigned;
        public float startDelay, shockDuration;
    }

    static SpatialScanSetup() => EditorApplication.update += WhenReady;
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
        if (SceneManager.GetActiveScene().name != "ROOM2") return;
        running = true;
        try { Setup(); }
        catch (Exception error)
        {
            File.WriteAllText(ReportPath, "{\"status\":\"error\",\"message\":\"" +
                error.ToString().Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","").Replace("\n","\\n") + "\"}");
            Debug.LogException(error);
        }
        finally { running = false; EditorApplication.update -= WhenReady; }
    }

    [MenuItem("Tools/Xiaoduan/Setup Final Hall Spatial Scan")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "ROOM2" || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open ROOM2 in Edit mode first.");
        var objects = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var entrances = objects.Where(item => item.name == "SM_PrisonDoor43").ToArray();
        if (entrances.Length != 1) throw new InvalidOperationException("Expected one SM_PrisonDoor43 entrance.");
        Transform entrance = entrances[0];
        var door = entrance.GetComponent<ProximityPrisonDoor>();
        var box = entrance.GetComponent<BoxCollider>();
        if (box == null) throw new InvalidOperationException("Entrance door has no BoxCollider.");
        Shader shader = Shader.Find("Xiaoduan/Spatial Scan URP");
        if (shader == null) throw new InvalidOperationException("Scan shader has not imported.");
        string[] shaderErrors = Errors(shader);
        if (shaderErrors.Length > 0) throw new InvalidOperationException(string.Join("\n",shaderErrors));
        var rendererPaths = AssetDatabase.FindAssets("t:UniversalRendererData",new[] { "Assets/Settings" })
            .Select(AssetDatabase.GUIDToAssetPath).ToArray();
        if (rendererPaths.Length == 0) throw new InvalidOperationException("No project URP renderer assets found.");
        var audio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SpatialScan/SpatialScan_MRI_Prototype.wav");
        var impact = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SpatialScan/SpatialScan_Crossing.wav");
        if (audio == null || impact == null) throw new InvalidOperationException("Scan audio clips have not imported.");
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save ROOM2 before setup.");
        string backupFolder = "Library/ScanBackups/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(backupFolder);
        File.Copy(scene.path,backupFolder + "/ROOM2.unity",false);
        foreach (string path in rendererPaths)
        {
            File.Copy(path,backupFolder + "/" + Path.GetFileName(path),false);
            File.Copy(path + ".meta",backupFolder + "/" + Path.GetFileName(path) + ".meta",false);
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure final hall scan");
        Directory.CreateDirectory("Assets/Materials/SpatialScan");
        AssetDatabase.Refresh();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Final Hall Spatial Scan" };
            AssetDatabase.CreateAsset(material,MaterialPath);
        }
        foreach (string path in rendererPaths)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            Undo.RecordObject(renderer,"Add spatial scan render feature");
            var feature = renderer.rendererFeatures.OfType<SpatialScanRendererFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<SpatialScanRendererFeature>();
                feature.name = "Final Hall Spatial Scan";
                AssetDatabase.AddObjectToAsset(feature,renderer);
                renderer.rendererFeatures.Add(feature);
            }
            Undo.RecordObject(feature,"Configure scan color and depth inputs");
            feature.passMaterial = material;
            feature.passIndex = 0;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.bindDepthStencilAttachment = false;
            feature.SetActive(true);
            feature.Create();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            var rendererSerialized = new SerializedObject(renderer);
            var featureMap = rendererSerialized.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize = renderer.rendererFeatures.Count;
            for (int index = 0; index < renderer.rendererFeatures.Count; index++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(renderer.rendererFeatures[index],out string featureGuid,out long localId);
                featureMap.GetArrayElementAtIndex(index).longValue = localId;
            }
            rendererSerialized.ApplyModifiedPropertiesWithoutUndo();
            renderer.SetDirty();
        }

        Vector3 doorway = door != null ? door.DoorwayCenter : entrance.TransformPoint(box.center);
        Vector3 axis = Vector3.ProjectOnPlane(entrance.right,Vector3.up).normalized;
        var otherDoors = objects.Where(item => item != entrance && item.GetComponent<ProximityPrisonDoor>() != null).ToArray();
        if (otherDoors.Length == 0) throw new InvalidOperationException("Cannot determine the hall interior direction.");
        Vector3 hallCenter = Vector3.zero;
        foreach (var item in otherDoors) hallCenter += item.position;
        hallCenter /= otherDoors.Length;
        if (Vector3.Dot(axis,hallCenter - doorway) < 0f) axis = -axis;
        var triggers = objects.Where(item => item.name == "FinalHallScanTrigger").ToArray();
        if (triggers.Length > 1) throw new InvalidOperationException("More than one FinalHallScanTrigger.");
        GameObject trigger;
        if (triggers.Length == 1) trigger = triggers[0].gameObject;
        else
        {
            trigger = new GameObject("FinalHallScanTrigger");
            SceneManager.MoveGameObjectToScene(trigger,scene);
            Undo.RegisterCreatedObjectUndo(trigger,"Create final hall entrance trigger");
        }
        Undo.RecordObject(trigger.transform,"Position trigger inside entrance");
        trigger.transform.SetPositionAndRotation(doorway + axis * 1.8f,Quaternion.LookRotation(axis,Vector3.up));
        trigger.layer = 0;
        BoxCollider triggerBox = trigger.GetComponent<BoxCollider>();
        if (triggerBox == null) triggerBox = Undo.AddComponent<BoxCollider>(trigger);
        Undo.RecordObject(triggerBox,"Configure entrance trigger");
        triggerBox.center = Vector3.zero;
        triggerBox.size = new Vector3(2.2f,3f,1.2f);
        triggerBox.isTrigger = true;
        Rigidbody triggerBody = trigger.GetComponent<Rigidbody>();
        if (triggerBody == null) triggerBody = Undo.AddComponent<Rigidbody>(trigger);
        Undo.RecordObject(triggerBody,"Configure trigger physics");
        triggerBody.isKinematic = true;
        triggerBody.useGravity = false;
        AudioSource source = trigger.GetComponent<AudioSource>();
        if (source == null) source = Undo.AddComponent<AudioSource>(trigger);
        Undo.RecordObject(source,"Assign mechanical MRI-inspired pulse");
        source.clip = audio;
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 0.22f;
        source.spatialBlend = 0.3f;
        source.minDistance = 4f;
        source.maxDistance = 45f;
        SpatialScanEvent effect = trigger.GetComponent<SpatialScanEvent>();
        if (effect == null) effect = Undo.AddComponent<SpatialScanEvent>(trigger);
        Undo.RecordObject(effect,"Configure synchronized scan event");
        effect.startDelay = 2f;
        effect.shockDuration = 0.5f;
        effect.scanAudio = source;
        effect.crossingSound = impact;
        foreach (UnityEngine.Object item in new UnityEngine.Object[] { trigger.transform,triggerBox,triggerBody,source,effect })
            EditorUtility.SetDirty(item);
        string[] checks = Verify();
        Undo.CollapseUndoOperations(group);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scan event could not save ROOM2.");
        shaderErrors = Errors(shader);
        File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report { status = shaderErrors.Length == 0 ? "configured" : "shader-errors",
            scene = scene.path, entranceDoor = entrance.name, triggerPosition = trigger.transform.position,
            scanDirection = axis, backup = backupFolder, renderers = rendererPaths, shaderErrors = shaderErrors,
            shaderSupported = shader.isSupported, audioAssigned = source.clip != null && effect.crossingSound != null,
            startDelay = effect.startDelay, shockDuration = effect.shockDuration, checks = checks },true));
        File.WriteAllText(Marker,Version);
        Debug.Log("[SpatialScan] Configured SM_PrisonDoor43 entrance: 2-second delay, camera-crossing shock, scan and audio.");
        SceneView.RepaintAll();
    }

    static string[] Errors(Shader shader) => ShaderUtil.GetShaderMessages(shader)
        .Where(message => message.severity.ToString() == "Error").Select(message => message.message).ToArray();
    static string[] Verify()
    {
        if (!SpatialScanEvent.CrossedPlane(0.4f,-0.2f) || !SpatialScanEvent.CrossedPlane(-0.4f,0.2f) ||
            SpatialScanEvent.CrossedPlane(2f,1f) || SpatialScanEvent.CrossedPlane(-2f,-1f))
            throw new InvalidOperationException("Scan-camera crossing verification failed.");
        return new[] { "Forward and backward camera crossings detected", "Same-side camera movement does not trigger impact",
            "Built-in URP full-screen pass supplies color and requested depth textures", "Renderer feature is gated to the active gameplay camera" };
    }
}
#endif
