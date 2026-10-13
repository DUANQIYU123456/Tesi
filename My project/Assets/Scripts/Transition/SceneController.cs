using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class SceneController : Singleton<SceneController>
{
    public bool IsTransitioning { get; private set; }

    // The portals can create this manager; no extra scene object is required.
    public static SceneController GetOrCreate()
    {
        if (Instance != null) return Instance;
        return new GameObject("SceneController").AddComponent<SceneController>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void TransitionToDestination(TransitionPoint point)
    {
        if (point == null || IsTransitioning) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Portal: the player must have the Player tag.", point);
            return;
        }

        if (point.transitionType == TransitionPoint.TransitionType.SameScene)
        {
            PlacePlayer(player, SceneManager.GetActiveScene(), point.destinationTag);
            return;
        }

        string targetScene = point.sceneName == null ? "" : point.sceneName.Trim();
        if (string.IsNullOrEmpty(targetScene) || !Application.CanStreamedLevelBeLoaded(targetScene))
        {
            Debug.LogError("Portal: add " + targetScene + " to File > Build Profiles > Scene List and enable it.", point);
            return;
        }
        if (targetScene == SceneManager.GetActiveScene().name)
        {
            PlacePlayer(player, SceneManager.GetActiveScene(), point.destinationTag);
            return;
        }

        StartCoroutine(LoadRoom(targetScene, point.destinationTag, player));
    }

    private IEnumerator LoadRoom(string sceneName, TransitionDestination.DestinationTag tag, GameObject oldPlayer)
    {
        IsTransitioning = true;
        var sourceScene = oldPlayer.scene;
        var rigRoots = new HashSet<GameObject> { oldPlayer.transform.root.gameObject };
        // ROOM1 uses the Starter Assets first-person camera rig.
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.gameObject.scene == sourceScene)
            rigRoots.Add(mainCamera.transform.root.gameObject);
        var followCamera = GameObject.Find("PlayerFollowCamera");
        if (followCamera != null && followCamera.scene == sourceScene)
            rigRoots.Add(followCamera.transform.root.gameObject);

        foreach (var root in rigRoots) DontDestroyOnLoad(root);
        GameObject arrivedPlayer = null;
        Camera arrivedCamera = null;

        // This event runs after Awake/OnEnable, before the new scene's Start.
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = (scene, mode) =>
        {
            GameObject targetPlayer = null;
            foreach (var candidate in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (candidate.scene != scene) continue;
                if (targetPlayer != null)
                    Debug.LogWarning("Portal: multiple Player objects in " + scene.name + ". Keep only one.");
                else targetPlayer = candidate;
            }

            if (targetPlayer != null)
            {
                // A room with its own player uses its own camera rig too.
                foreach (var root in rigRoots)
                {
                    if (root == null) continue;
                    root.SetActive(false);
                    Destroy(root);
                }
            }
            else
            {
                // ROOM2 currently has no PlayerCapsule: carry the ROOM1 rig over.
                foreach (var root in rigRoots)
                    if (root != null) SceneManager.MoveGameObjectToScene(root, scene);
                targetPlayer = oldPlayer;
            }

            PlacePlayer(targetPlayer, scene, tag);
            arrivedPlayer = targetPlayer;
            // Keep the carried gameplay camera when the room has no player rig.
            Camera gameplayCamera = targetPlayer == oldPlayer ? mainCamera : null;
            if (gameplayCamera == null)
            {
                foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (camera.gameObject.scene == scene && camera.CompareTag("MainCamera") && camera.isActiveAndEnabled)
                    {
                        gameplayCamera = camera;
                        break;
                    }
            }
            ConfigureGameplayCamera(gameplayCamera, scene);
            arrivedCamera = gameplayCamera;
            foreach (var root in rigRoots)
                if (root != null && root.activeInHierarchy)
                    Debug.Log("Portal rig: " + root.name + " is in " + root.scene.name + ".", root);
        };

        AsyncOperation loading = null;
        SceneManager.sceneLoaded += onLoaded;
        try
        {
            loading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (Exception exception)
        {
            Debug.LogError("Portal: could not load " + sceneName + ". " + exception.Message);
        }

        if (loading != null) yield return loading;
        SceneManager.sceneLoaded -= onLoaded;
        if (loading == null && sourceScene.isLoaded)
            foreach (var root in rigRoots)
                if (root != null) SceneManager.MoveGameObjectToScene(root, sourceScene);

        yield return new WaitForSecondsRealtime(0.25f);
        if (arrivedPlayer != null)
            Debug.Log("Portal player: " + arrivedPlayer.name + " | scene=" + arrivedPlayer.scene.name
                + " | position=" + arrivedPlayer.transform.position.ToString("F2"), arrivedPlayer);
        if (arrivedCamera != null)
            Debug.Log("Portal camera: " + arrivedCamera.name + " | scene=" + arrivedCamera.gameObject.scene.name
                + " | position=" + arrivedCamera.transform.position.ToString("F2")
                + " | enabled=" + arrivedCamera.isActiveAndEnabled, arrivedCamera);
        IsTransitioning = false;
    }

    private static void ConfigureGameplayCamera(Camera gameplayCamera, Scene scene)
    {
        if (gameplayCamera == null)
        {
            Debug.LogError("Portal: no gameplay MainCamera found in " + scene.name + ".");
            return;
        }

        gameplayCamera.enabled = true;
        // Asset demo cameras also render Display 1 and can cover the player view.
        // Disable only competing screen cameras; keep RenderTexture cameras intact.
        foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (camera == gameplayCamera || camera.gameObject.scene != scene || !camera.enabled
                || camera.targetTexture != null || camera.targetDisplay != gameplayCamera.targetDisplay) continue;
            camera.enabled = false;
            Debug.Log("Portal: disabled competing screen camera " + camera.name + ".", camera);
        }

        var gameplayListener = gameplayCamera.GetComponent<AudioListener>();
        if (gameplayListener == null) gameplayListener = gameplayCamera.gameObject.AddComponent<AudioListener>();
        foreach (var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            listener.enabled = listener == gameplayListener;
    }

    private static void PlacePlayer(GameObject player, Scene scene, TransitionDestination.DestinationTag tag)
    {
        TransitionDestination destination = null;
        var markers = UnityEngine.Object.FindObjectsByType<TransitionDestination>(FindObjectsSortMode.None);
        foreach (var marker in markers)
        {
            if (marker.gameObject.scene != scene || marker.destinationTag != tag) continue;
            if (destination != null)
            {
                Debug.LogError("Portal: more than one " + tag + " destination in " + scene.name + ". Use a unique tag.");
                return;
            }
            destination = marker;
        }
        if (player == null || destination == null)
        {
            Debug.LogError("Portal: " + scene.name + " needs an active TransitionDestination with tag " + tag + ".");
            return;
        }

        var controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (wasEnabled) controller.enabled = false;
        Vector3 positionDelta = destination.transform.position - player.transform.position;
        var body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = destination.transform.position;
            body.rotation = destination.transform.rotation;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
        player.transform.SetPositionAndRotation(destination.transform.position, destination.transform.rotation);
        // Tell Cinemachine this was a teleport, so it does not ease across the rooms.
        foreach (var camera in UnityEngine.Object.FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsSortMode.None))
        {
            bool followsPlayer = camera.Follow != null && (camera.Follow == player.transform || camera.Follow.IsChildOf(player.transform));
            bool looksAtPlayer = camera.LookAt != null && (camera.LookAt == player.transform || camera.LookAt.IsChildOf(player.transform));
            if (!followsPlayer && !looksAtPlayer) continue;
            if (followsPlayer) camera.OnTargetObjectWarped(camera.Follow, positionDelta);
            if (looksAtPlayer && camera.LookAt != camera.Follow) camera.OnTargetObjectWarped(camera.LookAt, positionDelta);
            camera.PreviousStateIsValid = false;
        }
        Physics.SyncTransforms();
        if (wasEnabled) controller.enabled = true;
        Debug.Log("Portal: arrived at " + scene.name + " / " + tag + " | destination="
            + destination.transform.position.ToString("F2") + " | player=" + player.transform.position.ToString("F2"), player);
    }
}
