using UnityEngine;
using Unity.Cinemachine;

namespace Xiaoduan.Room3
{
    public sealed class Room3CameraInterference : CinemachineExtension
    {
        [Range(0,1)] public float strength;
        public float downwardDegrees;
        public bool reducedMotion;
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Noise || reducedMotion) return;
            float t=Time.time;
            var offset=new Vector3(downwardDegrees+Mathf.Sin(t*27)*strength*.55f,
                Mathf.Sin(t*33.7f)*strength*.35f,Mathf.Sin(t*19.4f)*strength*.3f);
            state.OrientationCorrection*=Quaternion.Euler(offset);
        }
    }
}

