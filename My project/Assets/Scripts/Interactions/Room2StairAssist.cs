using System;
using System.IO;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Small, clearance-checked steps for the carried first-person player in ROOM2.</summary>
[DefaultExecutionOrder(-20)]
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class Room2StairAssist : MonoBehaviour
{
    [Range(0.1f, 0.7f)] public float maximumStepHeight = 0.6f;
    [Range(30f, 60f)] public float walkableSlope = 55f;
    [Min(0.05f)] public float probeAhead = 0.22f;

    CharacterController controller;
    StarterAssetsInputs input;
    float originalStep, originalSlope, originalSkin;
    bool configured;
    float nextReport;
    readonly RaycastHit[] rayHits = new RaycastHit[24];
    readonly Collider[] overlaps = new Collider[32];

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<StarterAssetsInputs>();
        originalStep = controller.stepOffset;
        originalSlope = controller.slopeLimit;
        originalSkin = controller.skinWidth;
    }

    void Update()
    {
        bool inRoom2 = gameObject.scene.name == "ROOM2";
        if (!inRoom2)
        {
            Restore();
            return;
        }
        if (!configured)
        {
            controller.stepOffset = Mathf.Min(0.45f, controller.height * 0.3f);
            controller.slopeLimit = walkableSlope;
            controller.skinWidth = Mathf.Max(originalSkin, controller.radius * 0.1f);
            configured = true;
            Debug.Log("[Room2Stairs] Player capsule configured: step=" + controller.stepOffset +
                ", slope=" + controller.slopeLimit + ", skin=" + controller.skinWidth, this);
        }
        if (!controller.enabled || input == null || input.move.sqrMagnitude < 0.01f || input.jump ||
            !controller.isGrounded || controller.velocity.y > 0.1f) return;

        Vector3 direction = transform.right * input.move.x + transform.forward * input.move.y;
        direction.y = 0f;
        direction.Normalize();
        Vector3 scale = transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(controller.height * Mathf.Abs(scale.y), radius * 2f);
        Vector3 center = transform.TransformPoint(controller.center);
        Vector3 foot = center - Vector3.up * (height * 0.5f);
        float distance = radius + probeAhead;
        // A low riser must be present. Do not lift the player while walking on flat ground.
        if (!Cast(foot + Vector3.up * 0.12f, direction, distance, 0.025f, out RaycastHit riser)) return;
        if (riser.normal.y > 0.55f) return;

        Vector3 topProbe = foot + direction * distance + Vector3.up * (maximumStepHeight + 0.08f);
        if (!Cast(topProbe, Vector3.down, maximumStepHeight + 0.1f, 0f, out RaycastHit landing)) return;
        float rise = landing.point.y - foot.y;
        if (rise < 0.06f || rise > maximumStepHeight ||
            Vector3.Angle(landing.normal, Vector3.up) > walkableSlope) return;
        float lift = rise + controller.skinWidth + 0.015f;
        float queryRadius = Mathf.Max(0.05f, radius - controller.skinWidth - 0.01f);
        Vector3 bottom = center - Vector3.up * (height * 0.5f - radius);
        Vector3 top = center + Vector3.up * (height * 0.5f - radius);

        // Check the entire upward sweep, then both the current and destination capsule.
        if (CapsuleBlocked(bottom, top, queryRadius, Vector3.up, lift)) return;
        if (Occupied(bottom + Vector3.up * lift, top + Vector3.up * lift, queryRadius)) return;
        Vector3 offset = direction * distance + Vector3.up * lift;
        if (Occupied(bottom + offset, top + offset, queryRadius)) return;
        if (CapsuleBlocked(bottom + Vector3.up * lift, top + Vector3.up * lift,
            queryRadius, direction, distance)) return;

        // CharacterController.Move still respects collision; there is no teleport or noclip.
        controller.Move(Vector3.up * lift);
    }

    bool Cast(Vector3 origin, Vector3 direction, float distance, float radius, out RaycastHit nearest)
    {
        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(origin, radius, direction, rayHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(origin, direction, rayHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        nearest = default;
        float closest = float.PositiveInfinity;
        if (count == rayHits.Length) return false; // Crowded geometry: prefer no automatic step.
        for (int i = 0; i < count; i++)
        {
            if (Own(rayHits[i].collider) || rayHits[i].distance >= closest) continue;
            closest = rayHits[i].distance;
            nearest = rayHits[i];
        }
        return closest < float.PositiveInfinity;
    }

    bool CapsuleBlocked(Vector3 bottom, Vector3 top, float radius, Vector3 direction, float distance)
    {
        int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, direction, rayHits,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == rayHits.Length) return true;
        for (int i = 0; i < count; i++) if (!Own(rayHits[i].collider)) return true;
        return false;
    }

    bool Occupied(Vector3 bottom, Vector3 top, float radius)
    {
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return true;
        for (int i = 0; i < count; i++) if (!Own(overlaps[i])) return true;
        return false;
    }

    bool Own(Collider other) => other == null || other.transform == transform || other.transform.IsChildOf(transform);

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!configured || input == null || input.move.sqrMagnitude < 0.01f ||
            hit.normal.y > 0.55f || Time.unscaledTime < nextReport) return;
        nextReport = Time.unscaledTime + 0.8f;
#if UNITY_EDITOR
        string path = hit.collider.name;
        for (Transform parent = hit.collider.transform.parent; parent != null; parent = parent.parent)
            path = parent.name + "/" + path;
        try
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "../Library/XiaoduanStairContact.json"),
                JsonUtility.ToJson(new Contact { playerPosition = transform.position, collider = path,
                    contact = hit.point, normal = hit.normal, colliderType = hit.collider.GetType().Name,
                    playerSpeed = controller.velocity.magnitude, stepOffset = controller.stepOffset }, true));
        }
        catch (IOException) { }
#endif
    }

    [Serializable] sealed class Contact
    {
        public Vector3 playerPosition, contact, normal;
        public string collider, colliderType;
        public float playerSpeed, stepOffset;
    }

    void Restore()
    {
        if (!configured || controller == null) return;
        controller.stepOffset = originalStep;
        controller.slopeLimit = originalSlope;
        controller.skinWidth = originalSkin;
        configured = false;
    }
    void OnDisable() => Restore();
}

public static class Room2StairBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    public static void Attach()
    {
        foreach (var controller in UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
            if (controller.CompareTag("Player") && controller.GetComponent<StarterAssetsInputs>() != null &&
                controller.GetComponent<Room2StairAssist>() == null)
                controller.gameObject.AddComponent<Room2StairAssist>();
    }
}
