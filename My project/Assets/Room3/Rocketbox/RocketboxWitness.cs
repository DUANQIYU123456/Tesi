using System;
using UnityEngine;
using Xiaoduan.Survey;
namespace Xiaoduan.Room3
{
    // Symbolic witnesses. The source rig is preserved; targets are solved within its measured reach.
    [DefaultExecutionOrder(440)]
    public sealed class RocketboxWitness:MonoBehaviour
    {
        public Room3StoryFlow flow;
        public Transform[] joints;
        public Quaternion[] bind;
        public int variation;
        public bool suspended,workStation;
        public Vector3 anchor;
        public Quaternion orientation;
        Transform spine,neck,head,clavicle,ul,ll,hl,ur,lr,hr,thigh,calf,foot;
        readonly Transform[,] fingers=new Transform[5,3];
        readonly Transform[,] leftFingers=new Transform[5,3];
        float poseTime,effortNow;
        public float AppliedEffort {get;private set;}
        Transform B(string n)=>Array.Find(joints,t=>t!=null&&t.name=="Bip01 "+n);
        void Awake(){Cache();}
        void Cache()
        {
            spine=B("Spine2");neck=B("Neck");head=B("Head");clavicle=B("R Clavicle");
            ul=B("L UpperArm");ll=B("L Forearm");hl=B("L Hand");ur=B("R UpperArm");lr=B("R Forearm");hr=B("R Hand");
            thigh=B("R Thigh");calf=B("R Calf");foot=B("R Foot");
            for(int f=0;f<5;f++)for(int p=0;p<3;p++){fingers[f,p]=B("R Finger"+f+(p==0?"":p.ToString()));leftFingers[f,p]=B("L Finger"+f+(p==0?"":p.ToString()));}
        }
        void SolveArm(Transform shoulder,Transform elbow,Transform wrist,Vector3 localTarget,Vector3 localPole)
        {
            if(shoulder==null||elbow==null||wrist==null)return;
            Vector3 a=shoulder.position,target=transform.TransformPoint(localTarget),delta=target-a;
            float upper=Vector3.Distance(a,elbow.position),lower=Vector3.Distance(elbow.position,wrist.position);
            if(upper<.001f||lower<.001f||delta.sqrMagnitude<.00001f)return;
            float reach=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.012f,(upper+lower)*(workStation?.94f:.985f));
            Vector3 direction=delta.normalized;
            Vector3 pole=transform.TransformPoint(localPole)-a;
            pole=Vector3.ProjectOnPlane(pole,direction);
            if(pole.sqrMagnitude<.00001f)pole=Vector3.ProjectOnPlane(transform.forward,direction);
            pole.Normalize();
            float along=(upper*upper-lower*lower+reach*reach)/(2*reach);
            Vector3 solvedElbow=a+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            shoulder.rotation=Quaternion.FromToRotation(elbow.position-a,solvedElbow-a)*shoulder.rotation;
            Vector3 wanted=a+direction*reach;
            elbow.rotation=Quaternion.FromToRotation(wrist.position-elbow.position,wanted-elbow.position)*elbow.rotation;
        }
        void Palm(float rise)
        {
            if(hr==null||fingers[1,0]==null||fingers[2,0]==null||fingers[4,0]==null)return;
            Vector3 along=fingers[2,0].position-hr.position;
            Vector3 span=fingers[1,0].position-fingers[4,0].position;
            Vector3 normal=Vector3.Cross(span,along).normalized;
            if(along.sqrMagnitude>.00001f&&normal.sqrMagnitude>.1f)
            {
                Quaternion basis=Quaternion.LookRotation(along,normal);
                Vector3 fingerDirection=transform.TransformDirection(new Vector3((variation-2)*.025f,1,-.025f));
                Quaternion desired=Quaternion.LookRotation(fingerDirection,transform.forward);
                Quaternion rotated=desired*Quaternion.Inverse(basis)*hr.rotation;
                hr.rotation=Quaternion.Slerp(hr.rotation,rotated,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.9f,rise)));
            }
            span=(fingers[1,0].position-fingers[4,0].position).normalized;
            // Relaxed fingers retain a small flexion at full oath instead of a flat paddle.
            float curl=Mathf.Lerp(20,4,rise);
            for(int f=0;f<5;f++)for(int p=0;p<3;p++)if(fingers[f,p]!=null)
                fingers[f,p].rotation=Quaternion.AngleAxis((f==0?.36f:1)*(p==0?1:p==1?.65f:.35f)*curl,span)*fingers[f,p].rotation;
        }
        public void Preview(float effort){Cache();effortNow=effort;Pose(effort,0);}
        void RelaxLeftHand()
        {
            if(hl==null||leftFingers[2,0]==null||leftFingers[1,0]==null||leftFingers[4,0]==null)return;
            Vector3 along=leftFingers[2,0].position-hl.position;
            Vector3 span=leftFingers[1,0].position-leftFingers[4,0].position;
            Vector3 normal=Vector3.Cross(span,along).normalized;
            if(along.sqrMagnitude>.00001f&&normal.sqrMagnitude>.1f){
                Vector3 direction=variation%5==1?new Vector3(.9f,-.1f,.03f):variation%5==3?new Vector3(.8f,.15f,.03f):new Vector3(.1f,-1,.04f);
                Quaternion desired=Quaternion.LookRotation(transform.TransformDirection(direction),-transform.forward);
                hl.rotation=Quaternion.RotateTowards(hl.rotation,desired*Quaternion.Inverse(Quaternion.LookRotation(along,normal))*hl.rotation,35);
            }
            span=(leftFingers[1,0].position-leftFingers[4,0].position).normalized;
            for(int f=0;f<5;f++)for(int p=0;p<3;p++)if(leftFingers[f,p]!=null)leftFingers[f,p].rotation=Quaternion.AngleAxis((f==0?.35f:1)*(p==0?14:p==1?10:6),span)*leftFingers[f,p].rotation;
        }
        void WorkPalm(Transform hand,Transform[,] digits,bool precision=false)
        {
            if(hand==null||digits[1,0]==null||digits[2,0]==null||digits[4,0]==null)return;
            Vector3 along=digits[2,0].position-hand.position;
            Vector3 span=digits[1,0].position-digits[4,0].position;
            Vector3 normal=Vector3.Cross(span,along).normalized;
            if(along.sqrMagnitude>.00001f&&normal.sqrMagnitude>.1f)
            {
                var current=Quaternion.LookRotation(along,normal);
                var desired=Quaternion.LookRotation(transform.forward,transform.up);
                hand.rotation=desired*Quaternion.Inverse(current)*hand.rotation;
            }
            span=(digits[1,0].position-digits[4,0].position).normalized;
            for(int f=0;f<5;f++)for(int p=0;p<3;p++)if(digits[f,p]!=null)
                digits[f,p].rotation=Quaternion.AngleAxis((f==0?.3f:1)*(p==0?(precision?22:9):p==1?(precision?18:6):(precision?12:3)),span)*digits[f,p].rotation;
        }
        void Pose(float effort,float time)
        {
            AppliedEffort=effort;
            for(int i=0;i<joints.Length;i++)if(joints[i]!=null)joints[i].localRotation=bind[i];
            float phase=variation*1.79f,amount=flow!=null&&flow.ReducedMotion?0:1;
            transform.localPosition=anchor+Vector3.up*(suspended?Mathf.Sin(time*.32f+phase)*.026f*amount:0);
            transform.localRotation=orientation*Quaternion.Euler(0,Mathf.Sin(time*.21f+phase)*.35f*amount,0);
            float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp((variation%5)*.028f,1,effort));
            if(spine!=null)spine.rotation=Quaternion.AngleAxis((workStation?5.0f:2.2f)+variation%3*.6f-rise*1.5f+Mathf.Sin(time*.7f+phase)*.18f*amount,transform.right)*spine.rotation;
            if(neck!=null)neck.rotation=Quaternion.AngleAxis(-rise*1.2f,transform.right)*neck.rotation;
            if(head!=null)head.rotation=Quaternion.AngleAxis(4+variation%3*1.2f-rise*5,transform.right)*head.rotation;
            if(clavicle!=null)clavicle.rotation=Quaternion.AngleAxis(5.5f*rise,transform.forward)*clavicle.rotation;
            Vector3 pl=new Vector3(-.23f,.86f,.09f),pr=new Vector3(.23f,.86f,.10f);
            if(variation%5==0)pr=new Vector3(.09f,1.36f,.22f);
            if(variation%5==1){pl=new Vector3(.01f,1.02f,.23f);pr=new Vector3(-.04f,.98f,.23f);}
            if(variation%5==3)pl=new Vector3(-.08f,1.22f,.24f);
            if(suspended&&variation>4){pl=new Vector3(-.28f,.82f,.08f);pr=new Vector3(.27f,.80f,.10f);if(variation%3==0)pr=new Vector3(.17f,.91f,.16f);}
            if(workStation)
            {
                float task=time*.85f+phase;
                pl=new Vector3(-.135f,1.025f,.29f);pr=new Vector3(.10f+Mathf.Sin(task)*.012f*amount,1.03f+Mathf.Sin(task*2)*.003f*amount,.30f+Mathf.Cos(task)*.012f*amount);
                rise=0;
            }
            Vector3 intermediate=new Vector3(.38f,1.35f,.24f);
            Vector3 raised=new Vector3(.29f+(variation%3)*.022f,1.78f+(variation%2)*.025f,.085f+(variation%3)*.012f);
            Vector3 target=rise<.4f?Vector3.Lerp(pr,intermediate,Mathf.SmoothStep(0,1,rise/.4f)):Vector3.Lerp(intermediate,raised,Mathf.SmoothStep(0,1,(rise-.4f)/.6f));
            SolveArm(ul,ll,hl,pl,new Vector3(-.39f,1.13f,.12f));
            SolveArm(ur,lr,hr,target,Vector3.Lerp(new Vector3(.38f,1.13f,.13f),new Vector3(.51f,1.45f,.10f),rise));
            if(workStation){WorkPalm(hl,leftFingers);WorkPalm(hr,fingers,true);}
            else {Palm(rise);RelaxLeftHand();}
            if(suspended&&variation>4&&thigh!=null&&calf!=null&&foot!=null)
            {thigh.rotation=Quaternion.AngleAxis(5+variation%3*4,transform.right)*thigh.rotation;calf.rotation=Quaternion.AngleAxis(-9-variation%3*3,transform.right)*calf.rotation;foot.rotation=Quaternion.AngleAxis(8,transform.right)*foot.rotation;}
        }
        void LateUpdate()
        {
            if(flow==null||SurveyHUD.BlocksWorld||flow.Reading||flow.ui.DecisionVisible||flow.EndingChosen)return;
            float target=workStation||variation>4?0:flow.OathOpened?1:flow.OathProgress;
            effortNow=Mathf.MoveTowards(effortNow,target,Time.deltaTime/(flow.ReducedMotion?.18f:.26f));
            poseTime+=Time.deltaTime;Pose(effortNow,poseTime);
        }
    }
}
