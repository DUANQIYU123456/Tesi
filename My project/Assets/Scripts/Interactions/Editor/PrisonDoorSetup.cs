#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PrisonDoorSetup
{
    const string DoorGuid = "abc00000000003547591637436657053";
    const string GridGuid = "abc00000000004129727590911923548";
    const string Version = "prison-doors-20261014-v1";
    const string Marker = "Library/XiaoduanPrisonDoors.completed";
    const string ReportPath = "Library/XiaoduanPrisonDoors.json";
    static double nextCheck;
    static bool running;

    [Serializable] sealed class DoorInfo
    {
        public string door, frame;
        public Vector3 oldPosition, position;
        public float oldAngle, closedAngle;
    }
    [Serializable] sealed class Report
    {
        public string status, scene, backup, message;
        public int count;
        public DoorInfo[] doors;
        public string[] checks;
    }

    static PrisonDoorSetup() => EditorApplication.update += ConfigureWhenReady;

    static void ConfigureWhenReady()
    {
        if (running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 3;
        if (File.Exists(Marker) && File.ReadAllText(Marker) == Version)
        {
            EditorApplication.update -= ConfigureWhenReady;
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.name != "ROOM2") return;
        running = true;
        try { Setup(); }
        catch (Exception error)
        {
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { status = "error", message = error.ToString() }, true));
            Debug.LogException(error);
        }
        finally { running = false; EditorApplication.update -= ConfigureWhenReady; }
    }

    static bool Matches(GameObject instance, string guid)
    {
        var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        return source != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)) == guid;
    }

    [MenuItem("Tools/Xiaoduan/Setup Automatic Prison Doors in ROOM2")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "ROOM2")
            throw new InvalidOperationException("Open ROOM2 in Edit mode to configure the prison doors.");
        var lods = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LODGroup>(true)).ToArray();
        var doors = lods.Where(lod => Matches(lod.gameObject, DoorGuid)).Select(lod => lod.gameObject).ToArray();
        var frames = lods.Where(lod => Matches(lod.gameObject, GridGuid)).Select(lod => lod.transform).ToArray();
        if (doors.Length == 0 || frames.Length == 0) throw new InvalidOperationException("Door/frame prefabs not found.");

        // The vendor pairs SM_PrisonDoorN with SM_PrisonGridN, including a fallen door.
        // Preflight every mapping before mutating the user's scene.
        var pairs = new Dictionary<GameObject, Transform>();
        foreach (var door in doors)
        {
            string frameName = door.name == "SM_Door_Prison" ? "SM_PrisonGrid41" :
                door.name.Replace("SM_PrisonDoor", "SM_PrisonGrid");
            var matches = frames.Where(frame => frame.name == frameName).ToArray();
            Transform frame = matches.Length == 1 ? matches[0] : frames.OrderBy(f =>
                Vector3.SqrMagnitude(f.position - door.transform.position)).FirstOrDefault();
            if (frame == null || (matches.Length != 1 && Vector3.Distance(frame.position, door.transform.position) > 3f))
                throw new InvalidOperationException("No reliable doorframe match for " + door.name);
            pairs.Add(door, frame);
        }
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save ROOM2 before setup.");
        string backup = "Library/DoorBackups/ROOM2_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        File.Copy(scene.path, backup, false);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure automatic prison doors");
        var results = new List<DoorInfo>();
        foreach (var pair in pairs)
        {
            GameObject door = pair.Key;
            Transform frame = pair.Value;
            Transform hinge = door.transform;
            Vector3 originalPosition = hinge.position;
            float originalAngle = hinge.localEulerAngles.y;
            Undo.RecordObject(hinge, "Seat and close prison door");
            // The gap begins 1.3 local metres along each frame, matching the intact doors.
            hinge.SetPositionAndRotation(frame.TransformPoint(new Vector3(0f, 0f, 1.3f)), frame.rotation);
            foreach (var item in door.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(item.gameObject, "Allow moving door rendering");
                GameObjectUtility.SetStaticEditorFlags(item.gameObject, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
            }
            Rigidbody body = door.GetComponent<Rigidbody>();
            if (body == null) body = Undo.AddComponent<Rigidbody>(door);
            Undo.RecordObject(body, "Configure kinematic hinged door");
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            BoxCollider collider = door.GetComponent<BoxCollider>();
            if (collider == null) throw new InvalidOperationException("Door collider missing: " + door.name);
            Undo.RecordObject(collider, "Keep the iron door solid");
            collider.enabled = true;
            collider.isTrigger = false;
            ProximityPrisonDoor automatic = door.GetComponent<ProximityPrisonDoor>();
            if (automatic == null) automatic = Undo.AddComponent<ProximityPrisonDoor>(door);
            Undo.RecordObject(automatic, "Store closed hinge pose");
            automatic.SetClosedPose(hinge.localRotation, collider.center);
            foreach (UnityEngine.Object item in new UnityEngine.Object[] { hinge, body, collider, automatic })
            {
                EditorUtility.SetDirty(item);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            }
            results.Add(new DoorInfo { door = door.name, frame = frame.name,
                oldPosition = originalPosition, position = hinge.position, oldAngle = originalAngle,
                closedAngle = hinge.localEulerAngles.y });
        }
        string[] checks = VerifyBehavior(doors[0].GetComponent<ProximityPrisonDoor>());
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save automatic prison doors.");
        AssetDatabase.SaveAssets();
        File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { status = "configured", count = results.Count,
            scene = scene.path, backup = backup, doors = results.ToArray(), checks = checks,
            message = "All matched prison doors are initially closed, use kinematic hinge motion, and retain colliders." }, true));
        File.WriteAllText(Marker, Version);
        Debug.Log("[PrisonDoors] Configured and saved " + results.Count + " proximity doors. Backup: " + backup);
        SceneView.RepaintAll();
    }

    static string[] VerifyBehavior(ProximityPrisonDoor door)
    {
        var evaluate = typeof(ProximityPrisonDoor).GetMethod("EvaluateProximity", BindingFlags.Instance | BindingFlags.NonPublic);
        var reset = typeof(ProximityPrisonDoor).GetMethod("ResetState", BindingFlags.Instance | BindingFlags.NonPublic);
        var direction = typeof(ProximityPrisonDoor).GetField("direction", BindingFlags.Instance | BindingFlags.NonPublic);
        var checks = new List<string>();
        void Sample(Vector3 point, float seconds) => evaluate.Invoke(door, new object[] { point, true, seconds });
        void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Door verification failed: " + message);
            checks.Add(message);
        }
        Vector3 center = door.DoorwayCenter;
        Vector3 normal = door.transform.right;
        try
        {
            reset.Invoke(door, null);
            Sample(center + normal, 0.02f);
            Expect(door.OpeningOrOpen, "Near player opens door");
            float firstDirection = (float)direction.GetValue(door);
            Sample(center - normal, 0.02f);
            Expect(door.OpeningOrOpen && (float)direction.GetValue(door) == firstDirection, "Crossing doorway keeps opening direction");
            Sample(center + normal * 3.4f, 0.02f);
            Expect(door.OpeningOrOpen, "Distance hysteresis holds door open");
            Sample(center + normal * 5f, 0.1f);
            Expect(door.OpeningOrOpen, "Short departure respects close delay");
            Sample(center + normal * 5f, 1f);
            Expect(!door.OpeningOrOpen, "Leaving closes door after delay");
            reset.Invoke(door, null);
            Sample(center + Vector3.up * 7.6f, 1f);
            Expect(!door.OpeningOrOpen, "Player on another floor cannot open door");
            Sample(center - normal, 0.02f);
            Expect(door.OpeningOrOpen && (float)direction.GetValue(door) != firstDirection, "Door opens away from either approach side");
            return checks.ToArray();
        }
        finally { reset.Invoke(door, null); }
    }
}
#endif
