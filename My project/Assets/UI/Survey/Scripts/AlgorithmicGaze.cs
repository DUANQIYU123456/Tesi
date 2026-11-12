using UnityEngine;
using Xiaoduan.Room3;

namespace Xiaoduan.Survey
{
    // A silent mechanical witness. No extra render camera or audio listener.
    public sealed class AlgorithmicGaze:MonoBehaviour
    {
        public Transform head;
        public Renderer lens;
        public float response=4;
        Transform eye;
        Room3StoryFlow story;
        float nextFind;
        MaterialPropertyBlock appearance;
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        void Awake(){appearance=new MaterialPropertyBlock();story=FindFirstObjectByType<Room3StoryFlow>();}
        void LateUpdate()
        {
            if(Time.timeScale<=0)return;
            if(story!=null&&story.EndingChosen){if(lens!=null){appearance.SetColor(Emission,Color.black);lens.SetPropertyBlock(appearance);}return;}
            if(Time.unscaledTime>=nextFind||eye==null){nextFind=Time.unscaledTime+.5f;eye=Camera.main!=null?Camera.main.transform:null;}
            if(eye==null||head==null)return;
            Vector3 direction=transform.InverseTransformDirection(eye.position-head.position).normalized;
            float yaw=Mathf.Clamp(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-80,80);
            float pitch=Mathf.Clamp(-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg,-25,60);
            head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(pitch,yaw,0),1-Mathf.Exp(-response*Time.deltaTime));
            if(lens!=null){float pulse=.92f+.08f*Mathf.Sin(Time.time*1.4f);appearance.SetColor(Emission,new Color(.16f,1.15f,.44f)*pulse);lens.SetPropertyBlock(appearance);}
        }
    }
}
