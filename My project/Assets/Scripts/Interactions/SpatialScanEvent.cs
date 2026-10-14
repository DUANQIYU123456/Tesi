using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>An entrance scan, synchronized to the moving wave's actual camera crossing.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class SpatialScanEvent : MonoBehaviour
{
    public float startDelay = 2f;
    public float scanSpeed = 14f;
    public float scanDistance = 75f;
    [Min(0.1f)] public float scanDuration = 5f;
    public bool repeatScans = true;
    [Min(0f)] public float repeatDelay = 15f;
    public float scanStartsBehindEntrance = 5f;
    public float scanBandWidth = 1f;
    public float scanTrailLength = 5f;
    public float gridSize = 0.85f;
    [Range(0.001f,0.03f)] public float gridThickness = 0.004f;
    [ColorUsage(false,true)] public Color scanColor = new Color(0.08f,1.2f,0.28f);
    [Min(0f)] public float scanBloomIntensity = 0.65f;
    public float shockDuration = 0.5f;
    [Range(0f,1f)] public float shockStrength = 1f;
    public bool screenShockEnabled = true;
    public bool oneShot = true;
    public AudioSource scanAudio;
    public AudioClip crossingSound;

    public static SpatialScanEvent Active { get; private set; }
    public Camera ScanCamera { get; private set; }
    public bool IsScanning { get; private set; }
    bool played;
    bool sequenceRunning;
    Volume shockVolume;
    VolumeProfile shockProfile;
    Volume bloomVolume;
    VolumeProfile bloomProfile;
    float shockRemaining;
    Vector3 cameraPosition;
    Quaternion cameraRotation;
    bool cameraOffsetApplied;
    bool cameraPrepared;
    bool previousPost;
    CameraOverrideOption previousDepth;
    bool previousHDR;
    LayerMask previousVolumeMask;
    UniversalAdditionalCameraData cameraData;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active = null; Shader.SetGlobalFloat("_XDScanFade",0f); Shader.SetGlobalFloat("_XDScanShock",0f); }

    void OnTriggerEnter(Collider other)
    {
        for (Transform item = other.transform; item != null; item = item.parent)
            if (item.CompareTag("Player")) { BeginScan(); return; }
    }

    [ContextMenu("Preview Scan (Play Mode)")]
    public void BeginScan()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || sequenceRunning || Active != null || (oneShot && played)) return;
        ScanCamera = Camera.main;
        if (ScanCamera == null) { Debug.LogWarning("[SpatialScan] No gameplay camera available.",this); return; }
        Active = this;
        played = true;
        sequenceRunning = true;
        StartCoroutine(ScanSequence());
    }

    IEnumerator ScanSequence()
    {
        yield return new WaitForSeconds(Mathf.Max(0f,startDelay));
        while (isActiveAndEnabled)
        {
            if (ScanCamera == null) break;
            yield return Scan();
            if (!repeatScans) break;
            // Release Active during the quiet interval so nearby cell audio can return.
            yield return new WaitForSeconds(Mathf.Max(0f,repeatDelay));
            while (Active != null && Active != this) yield return null;
            ScanCamera = Camera.main;
            if (ScanCamera == null) break;
            Active = this;
        }
        Cleanup();
        sequenceRunning = false;
    }

    IEnumerator Scan()
    {
        PrepareCamera();
        IsScanning = true;
        Vector3 axis = transform.forward.normalized;
        Vector3 origin = transform.position - axis * scanStartsBehindEntrance;
        Shader.SetGlobalVector("_XDScanOrigin",origin);
        Shader.SetGlobalVector("_XDScanDirection",axis);
        Shader.SetGlobalColor("_XDScanColor",scanColor);
        Shader.SetGlobalFloat("_XDScanBand",scanBandWidth);
        Shader.SetGlobalFloat("_XDScanTail",scanTrailLength);
        Shader.SetGlobalFloat("_XDGridSize",gridSize);
        Shader.SetGlobalFloat("_XDGridThickness",gridThickness);
        if (scanAudio != null) scanAudio.Play();
        float travel = 0f;
        float previousSide = Vector3.Dot(ScanCamera.transform.position - origin,axis);
        bool cameraCrossed = false;
        float end = scanDistance + scanTrailLength;
        float duration = Mathf.Max(0.1f,scanDuration);
        float elapsed = 0f;
        // Keep the same spatial reach; duration determines the wave's speed.
        scanSpeed = end / duration;
        Shader.SetGlobalFloat("_XDScanTravel",0f);
        Shader.SetGlobalFloat("_XDScanShock",0f);
        while (elapsed < duration && ScanCamera != null)
        {
            elapsed = Mathf.Min(duration,elapsed + Time.deltaTime);
            travel = end * elapsed / duration;
            float side = Vector3.Dot(ScanCamera.transform.position - origin,axis) - travel;
            if (!cameraCrossed && CrossedPlane(previousSide,side))
            {
                cameraCrossed = true;
                if (screenShockEnabled) shockRemaining = Mathf.Max(0.01f,shockDuration);
                if (scanAudio != null && crossingSound != null) scanAudio.PlayOneShot(crossingSound,0.65f);
                Debug.Log("[SpatialScan] Green wave crossed gameplay camera; shock duration=" + shockDuration,this);
            }
            previousSide = side;
            shockRemaining = Mathf.Max(0f,shockRemaining - Time.deltaTime);
            float shock = shockRemaining > 0f ? shockStrength * Mathf.Clamp01(shockRemaining / Mathf.Max(shockDuration * 0.25f,0.01f)) : 0f;
            if (shockVolume != null) shockVolume.weight = shock;
            Shader.SetGlobalFloat("_XDScanShock",shock);
            Shader.SetGlobalFloat("_XDScanTravel",travel);
            float fade = Mathf.Clamp01((duration - elapsed) / Mathf.Min(0.5f,duration));
            Shader.SetGlobalFloat("_XDScanFade",fade);
            if (bloomVolume != null) bloomVolume.weight = fade;
            yield return null;
        }
        Cleanup();
    }

    public static bool CrossedPlane(float previous, float current) =>
        (previous > 0f && current <= 0f) || (previous < 0f && current >= 0f);

    void PrepareCamera()
    {
        cameraPrepared = true;
        cameraData = ScanCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            previousPost = cameraData.renderPostProcessing;
            previousDepth = cameraData.requiresDepthOption;
            previousVolumeMask = cameraData.volumeLayerMask;
            cameraData.renderPostProcessing = true;
            cameraData.requiresDepthTexture = true;
            cameraData.volumeLayerMask |= 1 << gameObject.layer;
        }
        previousHDR = ScanCamera.allowHDR;
        ScanCamera.allowHDR = true;
        var bloomObject = new GameObject("Scan Soft Bloom (runtime)");
        bloomObject.transform.SetParent(transform,false);
        bloomObject.layer = gameObject.layer;
        bloomVolume = bloomObject.AddComponent<Volume>();
        bloomVolume.isGlobal = true;
        bloomVolume.priority = 200f;
        bloomVolume.weight = 1f;
        bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = bloomProfile.Add<Bloom>();
        bloom.intensity.Override(scanBloomIntensity);
        bloom.threshold.Override(1.1f);
        bloom.scatter.Override(0.65f);
        bloom.clamp.Override(12f);
        bloomVolume.sharedProfile = bloomProfile;
        var volumeObject = new GameObject("Scan Camera Shock (runtime)");
        volumeObject.transform.SetParent(transform,false);
        volumeObject.layer = gameObject.layer;
        shockVolume = volumeObject.AddComponent<Volume>();
        shockVolume.isGlobal = true;
        shockVolume.priority = 250f;
        shockVolume.weight = 0f;
        shockProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        var chromatic = shockProfile.Add<ChromaticAberration>();
        chromatic.intensity.Override(1f);
        var grain = shockProfile.Add<FilmGrain>();
        grain.type.Override(FilmGrainLookup.Medium2);
        grain.intensity.Override(0.65f);
        grain.response.Override(0.7f);
        shockVolume.sharedProfile = shockProfile;
        RenderPipelineManager.beginCameraRendering += BeginCamera;
        RenderPipelineManager.endCameraRendering += EndCamera;
    }

    void BeginCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != ScanCamera || shockRemaining <= 0f || !screenShockEnabled) return;
        cameraPosition = camera.transform.position;
        cameraRotation = camera.transform.rotation;
        float envelope = Mathf.Clamp01(shockRemaining / Mathf.Max(shockDuration * 0.25f,0.01f)) * shockStrength;
        float x = Mathf.PerlinNoise(Time.time * 28f,0.1f) * 2f - 1f;
        float y = Mathf.PerlinNoise(0.3f,Time.time * 31f) * 2f - 1f;
        camera.transform.position += camera.transform.right * (x * 0.045f * envelope) +
            camera.transform.up * (y * 0.035f * envelope);
        camera.transform.rotation = cameraRotation * Quaternion.Euler(y * 1.8f * envelope,x * 1.5f * envelope,x * 1.2f * envelope);
        cameraOffsetApplied = true;
    }

    void EndCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != ScanCamera || !cameraOffsetApplied) return;
        camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
        cameraOffsetApplied = false;
    }

    void Cleanup()
    {
        IsScanning = false;
        shockRemaining = 0f;
        RenderPipelineManager.beginCameraRendering -= BeginCamera;
        RenderPipelineManager.endCameraRendering -= EndCamera;
        if (ScanCamera != null && cameraPrepared)
        {
            if (cameraOffsetApplied) ScanCamera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            ScanCamera.allowHDR = previousHDR;
        }
        cameraOffsetApplied = false;
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = previousPost;
            cameraData.requiresDepthOption = previousDepth;
            cameraData.volumeLayerMask = previousVolumeMask;
        }
        if (shockVolume != null) Destroy(shockVolume.gameObject);
        if (shockProfile != null)
        {
            foreach (var component in shockProfile.components) Destroy(component);
            Destroy(shockProfile);
        }
        shockVolume = null;
        shockProfile = null;
        if (bloomVolume != null) Destroy(bloomVolume.gameObject);
        if (bloomProfile != null)
        {
            foreach (var component in bloomProfile.components) Destroy(component);
            Destroy(bloomProfile);
        }
        bloomVolume = null;
        bloomProfile = null;
        cameraData = null;
        cameraPrepared = false;
        if (scanAudio != null) scanAudio.Stop();
        if (Active == this)
        {
            Shader.SetGlobalFloat("_XDScanFade",0f);
            Shader.SetGlobalFloat("_XDScanShock",0f);
            Active = null;
        }
    }
    void OnDisable()
    {
        StopAllCoroutines();
        Cleanup();
        sequenceRunning = false;
    }
}
