using UnityEngine;
using UnityEngine.Rendering.Universal;
using Xiaoduan.Narrative;

namespace Xiaoduan.Room3
{
    public sealed class Room3Environment : MonoBehaviour
    {
        public int rendererIndex;
        public Transform arrival, checkpoint;
        public Transform dial;
        public bool reachedCore;
        Camera configuredCamera;
        Transform player;
        float nextFind;
        void Update()
        {
            if (Time.unscaledTime >= nextFind)
            {
                nextFind = Time.unscaledTime + .5f;
                var candidate = GameObject.FindGameObjectWithTag("Player");
                player = candidate != null ? candidate.transform : null;
                var camera = Camera.main;
                if (camera != null && camera.gameObject.scene == gameObject.scene && camera != configuredCamera)
                {
                    configuredCamera = camera;
                    var data = camera.GetUniversalAdditionalCameraData();
                    data.SetRenderer(rendererIndex);
                    data.renderPostProcessing = true;
                    data.volumeLayerMask = ~0;
                    camera.allowHDR = true;
                    camera.farClipPlane = 230;
                    camera.nearClipPlane = .08f;
                    camera.backgroundColor = new Color(.025f,.039f,.043f);
                    camera.clearFlags = CameraClearFlags.SolidColor;
                }
                var hud = FindFirstObjectByType<NarrativeHUD>();
                if (hud != null && hud.gameObject.scene == gameObject.scene)
                    hud.roomText.text = "03  /  THE RESIDUAL ARCHIVE";
            }
            if (player == null || player.gameObject.scene != gameObject.scene) return;
            if (player.position.z > 34 && Mathf.Abs(player.position.x) < 8 && player.position.y > -1) reachedCore = true;
            if (player.position.y < -18) ReturnToPath();
        }
        public void ReturnToPath()
        {
            if (player == null) return;
            var destination = reachedCore ? checkpoint : arrival;
            if (destination == null) return;
            var cc = player.GetComponent<CharacterController>();
            bool enabled = cc != null && cc.enabled;
            if (enabled) cc.enabled = false;
            player.SetPositionAndRotation(destination.position, destination.rotation);
            Physics.SyncTransforms();
            if (enabled) cc.enabled = true;
        }
        void OnDisable()
        {
            // The portal can carry this rig into ROOM2, which needs its own scanner renderer.
            if (configuredCamera == null) return;
            var data = configuredCamera.GetComponent<UniversalAdditionalCameraData>();
            if (data != null) data.SetRenderer(-1);
            configuredCamera.clearFlags = CameraClearFlags.Skybox;
            configuredCamera.farClipPlane = 1000;
            configuredCamera.nearClipPlane = .1f;
        }
    }
}
