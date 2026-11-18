using System;
using UnityEngine;
using Xiaoduan.Survey;
namespace Xiaoduan.Room3
{
    // Anatomical witnesses are symbolic reconstructions, not likenesses of the named plaintiffs.
    [DefaultExecutionOrder(430)]
    public sealed class Room3Witness:MonoBehaviour
    {
        public Room3StoryFlow flow;
        public Transform[] joints;
        public Quaternion[] bindRotations;
        public int variation;
        public bool workStation;
        public Light chestLight;
        public Renderer[] surfaces;
        Quaternion[] rest;
        Transform spine,neck,head,upperL,lowerL,handL,upperR,lowerR,handR,middleR,indexR,pinkyR;
        float phase;
        MaterialPropertyBlock block;
        Transform Bone(string n)=>Array.Find(joints,t=>t.name==n);
        void Awake(){Cache();Pose(0,0);}
        public void PreviewPose(){Cache();Pose(0,0);}
        void Cache()
        {
            rest=new Quaternion[joints.Length];for(int i=0;i<joints.Length;i++)rest[i]=bindRotations!=null&&bindRotations.Length==joints.Length?bindRotations[i]:joints[i].localRotation;
            spine=Bone("spine_02");neck=Bone("neck_01");head=Bone("head");
            upperL=Bone("upperarm_l");lowerL=Bone("lowerarm_l");handL=Bone("hand_l");
            upperR=Bone("upperarm_r");lowerR=Bone("lowerarm_r");handR=Bone("hand_r");middleR=Bone("middle_03_r");indexR=Bone("index_01_r");pinkyR=Bone("pinky_01_r");
            block=new MaterialPropertyBlock();phase=variation*1.73f;
        }
        void Aim(Transform bone,Transform child,Vector3 localTarget)
        {var d=child.position-bone.position;var goal=transform.TransformPoint(localTarget)-bone.position;if(d.sqrMagnitude>.00001f&&goal.sqrMagnitude>.00001f)bone.rotation=Quaternion.FromToRotation(d,goal)*bone.rotation;}
        void Arm(Transform upper,Transform lower,Transform hand,Vector3 elbow,Vector3 palm)
        {Aim(upper,lower,elbow);Aim(lower,hand,palm);}
        void Pose(float effort,float time)
        {
            for(int i=0;i<joints.Length;i++)joints[i].localRotation=rest[i];
            float breath=Mathf.Sin(time*.8f+phase);
            spine.rotation=Quaternion.AngleAxis(3+variation%3*2+breath*.28f,transform.right)*spine.rotation;
            neck.rotation=Quaternion.AngleAxis(6+variation%2*4-effort*7,transform.right)*neck.rotation;
            head.rotation=Quaternion.AngleAxis((variation-2)*3,transform.up)*head.rotation;
            Vector3 elbowL=new Vector3(.22f,1.06f,-.015f),palmL=new Vector3(.2f,.83f,-.05f);
            Vector3 elbowR=new Vector3(-.22f,1.06f,-.015f),palmR=new Vector3(-.2f,.83f,-.05f);
            if(variation==0){elbowR=new Vector3(-.24f,1.06f,-.13f);palmR=new Vector3(-.07f,1.43f,-.11f);}
            if(variation==1){elbowL=new Vector3(.25f,1.02f,-.1f);palmL=new Vector3(-.08f,1.04f,-.17f);elbowR=new Vector3(-.23f,1.01f,-.1f);palmR=new Vector3(.04f,1.0f,-.2f);}
            if(variation==2){elbowL=new Vector3(.21f,1.08f,-.12f);palmL=new Vector3(.06f,1.26f,-.23f);}
            if(variation==3){elbowR=new Vector3(-.26f,1.05f,-.07f);palmR=new Vector3(.07f,.91f,-.19f);}
            if(workStation){elbowL=new Vector3(.25f,1.03f,-.17f);palmL=new Vector3(.16f,1.06f,-.41f);elbowR=new Vector3(-.25f,1.02f,-.17f);palmR=new Vector3(-.08f,1.04f,-.4f);effort=0;}
            var liftedElbow=new Vector3(-.29f,1.48f,-.075f);var liftedPalm=new Vector3(-.32f,1.77f,-.09f);
            float rise=Mathf.SmoothStep(0,1,effort);
            Arm(upperL,lowerL,handL,elbowL,palmL);
            Arm(upperR,lowerR,handR,Vector3.Lerp(elbowR,liftedElbow,rise),Vector3.Lerp(palmR,liftedPalm,rise));
            if(rise>.01f)
            {
                var before=handR.rotation;var middle=middleR;var wrist=Vector3.Lerp(palmR,liftedPalm,rise);
                Aim(handR,middle,wrist+Vector3.up*.18f);
                var axis=(middle.position-handR.position).normalized;
                var a=indexR.position-handR.position;var b=pinkyR.position-handR.position;
                var normal=Vector3.Cross(a,b).normalized;
                float turn=Vector3.SignedAngle(Vector3.ProjectOnPlane(normal,axis),Vector3.ProjectOnPlane(-transform.forward,axis),axis);
                handR.rotation=Quaternion.AngleAxis(turn+(variation-2)*3,axis)*handR.rotation;
                handR.rotation=Quaternion.Slerp(before,handR.rotation,rise);
            }
            foreach(var joint in joints)
                if(joint.name.Contains("_0")&&(joint.name.StartsWith("index")||joint.name.StartsWith("middle")||joint.name.StartsWith("ring")||joint.name.StartsWith("pinky")))
                    joint.localRotation*=Quaternion.Euler(0,0,(joint.name.EndsWith("_l")?1:-1)*(joint.name.Contains("_01")?5:9)*(1-rise*.7f));
        }
        void LateUpdate()
        {
            if(flow==null||SurveyHUD.BlocksWorld||flow.Reading||flow.ui.DecisionVisible||flow.EndingChosen)return;
            Pose(flow.OathOpened?1:flow.OathProgress,Time.time);
            float illumination=0; // The phosphor stays in the chest traces; faces remain physically shaded.
            foreach(var r in surfaces){r.GetPropertyBlock(block);block.SetColor("_EmissionColor",new Color(.12f,.65f,.29f)*illumination);r.SetPropertyBlock(block);}
        }
    }
}
