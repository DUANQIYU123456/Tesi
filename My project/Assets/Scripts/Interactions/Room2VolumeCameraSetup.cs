using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Enables the ROOM2 look on the camera rig carried through the portal.</summary>
public sealed class Room2VolumeCameraSetup : MonoBehaviour
{
    Camera configuredCamera;
    UniversalAdditionalCameraData configuredData;
    bool originalPost,originalHDR;
    LayerMask originalMask;

    void LateUpdate()
    {
        Camera camera=Camera.main;
        if (camera==null || camera.gameObject.scene!=gameObject.scene) return;
        if (camera==configuredCamera) return;
        Restore();
        var data=camera.GetComponent<UniversalAdditionalCameraData>();
        if (data==null) return;
        configuredCamera=camera;
        configuredData=data;
        originalPost=data.renderPostProcessing;
        originalHDR=camera.allowHDR;
        originalMask=data.volumeLayerMask;
        data.renderPostProcessing=true;
        data.volumeLayerMask|=1<<gameObject.layer;
        camera.allowHDR=true;
    }
    void Restore()
    {
        if (configuredData!=null)
        {
            configuredData.renderPostProcessing=originalPost;
            configuredData.volumeLayerMask=originalMask;
        }
        if (configuredCamera!=null) configuredCamera.allowHDR=originalHDR;
        configuredCamera=null;
        configuredData=null;
    }
    void OnDisable() => Restore();
}
