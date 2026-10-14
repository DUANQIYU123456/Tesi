using UnityEngine;

/// <summary>A quiet, localized labor memory before the final hall entrance.</summary>
[RequireComponent(typeof(AudioSource),typeof(AudioLowPassFilter))]
[DisallowMultipleComponent]
public sealed class LaborEchoEmitter : MonoBehaviour
{
    public Transform player;
    [Range(0f,1f)] public float baseVolume = 0.3f;
    [Min(1f)] public float audibleDistance = 6f;
    [Min(0.1f)] public float fadeSeconds = 0.8f;
    public bool duckDuringScan = true;
    public bool muffleBehindWalls = true;
    AudioSource source;
    AudioLowPassFilter filter;
    float nextPlayerSearch;
    float nextOcclusionCheck;
    float occlusionGain = 1f;
    bool occluded;
    readonly RaycastHit[] hits = new RaycastHit[16];

    void Awake()
    {
        source = GetComponent<AudioSource>();
        filter = GetComponent<AudioLowPassFilter>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1f;
        source.maxDistance = audibleDistance;
        source.dopplerLevel = 0f;
        source.volume = 0f;
    }

    void Update()
    {
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.unscaledTime + 0.5f;
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        Camera camera = Camera.main;
        bool valid = player != null && player.gameObject.activeInHierarchy && camera != null && source.clip != null;
        Vector3 listener = valid ? camera.transform.position : Vector3.zero;
        bool close = valid && Vector3.Distance(listener,transform.position) < audibleDistance + 0.5f;
        bool scan = duckDuringScan && SpatialScanEvent.Active != null;
        if (close && Time.unscaledTime >= nextOcclusionCheck)
        {
            nextOcclusionCheck = Time.unscaledTime + 0.2f;
            occluded = false;
            if (muffleBehindWalls)
            {
                Vector3 delta = listener - transform.position;
                int count = Physics.RaycastNonAlloc(transform.position,delta.normalized,hits,
                    delta.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Transform item = hits[i].collider.transform;
                    if (item == transform || item.IsChildOf(transform) || item == player || item.IsChildOf(player)) continue;
                    occluded = true;
                    break;
                }
            }
            occlusionGain = occluded ? 0.65f : 1f;
        }
        float target = close && !scan ? baseVolume * occlusionGain : 0f;
        if (target > 0f && !source.isPlaying)
        {
            // Reapproaching samples the continuing rhythm instead of restarting at its first beat.
            source.time = Mathf.Repeat(Time.time,source.clip.length);
            source.Play();
        }
        source.volume = Mathf.MoveTowards(source.volume,target,baseVolume * Time.deltaTime / Mathf.Max(fadeSeconds,0.1f));
        filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency,occluded ? 2200f : 12000f,Time.deltaTime * 5f);
        if (target <= 0f && source.volume < 0.001f && source.isPlaying) source.Stop();
    }

    void OnDisable() { if (source != null) source.Stop(); }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f,0.8f,1f,0.45f);
        Gizmos.DrawWireSphere(transform.position,audibleDistance);
    }
}
