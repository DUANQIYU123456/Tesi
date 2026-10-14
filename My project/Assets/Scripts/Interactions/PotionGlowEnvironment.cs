using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Enables post processing on the gameplay camera, including the carried portal rig.</summary>
[DisallowMultipleComponent]
public sealed class PotionGlowEnvironment : MonoBehaviour
{
    Camera lastCamera;
    bool previousPostProcessing;
    bool previousHDR;
    LayerMask previousMask;
    float nextCheck;

    void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.5f;
        Camera current = Camera.main;
        if (current == null || current == lastCamera) return;
        Restore();
        if (!current.TryGetComponent<UniversalAdditionalCameraData>(out var data)) return;
        lastCamera = current;
        previousPostProcessing = data.renderPostProcessing;
        previousHDR = current.allowHDR;
        previousMask = data.volumeLayerMask;
        data.renderPostProcessing = true;
        data.volumeLayerMask |= 1 << gameObject.layer;
        current.allowHDR = true;
    }

    void Restore()
    {
        if (lastCamera != null)
        {
            if (lastCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            {
                data.renderPostProcessing = previousPostProcessing;
                data.volumeLayerMask = previousMask;
            }
            lastCamera.allowHDR = previousHDR;
        }
        lastCamera = null;
    }

    void OnDisable() => Restore();
}
