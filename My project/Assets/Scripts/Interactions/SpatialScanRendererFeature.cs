using UnityEngine;
using UnityEngine.Rendering.Universal;

// Reuse URP 17's built-in Render Graph implementation and its color/depth bindings.
public sealed class SpatialScanRendererFeature : FullScreenPassRendererFeature
{
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Application.isPlaying || SpatialScanEvent.Active == null || !SpatialScanEvent.Active.IsScanning ||
            renderingData.cameraData.camera != SpatialScanEvent.Active.ScanCamera) return;
        base.AddRenderPasses(renderer, ref renderingData);
    }
}
