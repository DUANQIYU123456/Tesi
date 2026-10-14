using UnityEngine;

/// <summary>Hinged prison door: proximity opening, delayed closing and floor separation.</summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ProximityPrisonDoor : MonoBehaviour
{
    public Transform player;
    [Min(0.5f)] public float openDistance = 3f;
    [Min(0.5f)] public float closeDistance = 3.8f;
    [Min(0f)] public float closeDelay = 0.75f;
    [Range(45f, 110f)] public float openAngle = 95f;
    [Min(10f)] public float rotationSpeed = 150f;
    [Min(0.5f)] public float verticalTolerance = 1.8f;
    [SerializeField] Quaternion closedLocalRotation = Quaternion.identity;
    [SerializeField] Vector3 doorwayCenterLocal = new Vector3(0.031f, 1.121f, 0.615f);
    [SerializeField] bool hasClosedPose;

    Rigidbody body;
    bool wantsOpen;
    float currentAngle;
    float direction = 1f;
    float awaySeconds;
    float nextSearch;
    public bool OpeningOrOpen => wantsOpen;

    public void SetClosedPose(Quaternion rotation, Vector3 center)
    {
        closedLocalRotation = rotation;
        doorwayCenterLocal = center;
        hasClosedPose = true;
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        if (!hasClosedPose)
        {
            closedLocalRotation = transform.localRotation;
            doorwayCenterLocal = GetComponent<BoxCollider>().center;
        }
        transform.localRotation = closedLocalRotation;
        body.rotation = transform.rotation;
        ResetState();
    }

    void ResetState()
    {
        wantsOpen = false;
        currentAngle = 0f;
        direction = 1f;
        awaySeconds = 0f;
    }

    Quaternion ClosedWorldRotation => transform.parent != null
        ? transform.parent.rotation * closedLocalRotation : closedLocalRotation;

    public Vector3 DoorwayCenter => transform.parent != null
        ? transform.parent.TransformPoint(transform.localPosition + closedLocalRotation *
            Vector3.Scale(doorwayCenterLocal, transform.localScale))
        : transform.position + closedLocalRotation * Vector3.Scale(doorwayCenterLocal, transform.localScale);

    void FixedUpdate()
    {
        if (player == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        bool present = player != null && player.gameObject.activeInHierarchy;
        Vector3 center = present ? player.position + Vector3.up : Vector3.zero;
        if (present && player.TryGetComponent<CharacterController>(out var capsule))
            center = player.TransformPoint(capsule.center);
        EvaluateProximity(center, present, Time.fixedDeltaTime);
        float target = wantsOpen ? openAngle * direction : 0f;
        float next = Mathf.MoveTowards(currentAngle, target, rotationSpeed * Time.fixedDeltaTime);
        if (Mathf.Abs(next - currentAngle) < 0.0001f) return;
        currentAngle = next;
        Quaternion local = closedLocalRotation * Quaternion.Euler(0f, currentAngle, 0f);
        body.MoveRotation(transform.parent != null ? transform.parent.rotation * local : local);
    }

    void EvaluateProximity(Vector3 playerCenter, bool present, float deltaTime)
    {
        Vector3 offset = playerCenter - DoorwayCenter;
        float horizontal = new Vector2(offset.x, offset.z).magnitude;
        bool sameFloor = present && Mathf.Abs(offset.y) <= verticalTolerance;
        float threshold = wantsOpen ? Mathf.Max(closeDistance, openDistance + 0.1f) : openDistance;
        bool nearby = sameFloor && horizontal <= threshold;
        if (nearby)
        {
            // Local +Z is the leaf width; +/-X is the side of this door's plane.
            // Keep the chosen direction until closed, so crossing the doorway cannot reverse the leaf.
            if (!wantsOpen && Mathf.Abs(currentAngle) < 1f)
            {
                float side = Vector3.Dot(playerCenter - transform.position, ClosedWorldRotation * Vector3.right);
                direction = side >= 0f ? -1f : 1f;
            }
            wantsOpen = true;
            awaySeconds = 0f;
        }
        else
        {
            awaySeconds += deltaTime;
            if (awaySeconds >= closeDelay) wantsOpen = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.5f);
        Gizmos.DrawWireSphere(DoorwayCenter, openDistance);
        Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(DoorwayCenter, closeDistance);
    }
}
