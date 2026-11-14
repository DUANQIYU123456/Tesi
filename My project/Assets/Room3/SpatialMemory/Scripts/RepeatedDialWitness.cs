using UnityEngine;
using TMPro;
namespace Xiaoduan.Room3
{
    public sealed class RepeatedDialWitness:MonoBehaviour
    {
        public Room3RecursiveArchive memory;
        public Transform hand;
        public GameObject firstCrack,secondCrack;
        public TMP_Text plaque;
        float angle;
        int last=-1;
        void Update()
        {
            int phase=memory.RecordDecoded?3:memory.Passes;
            if(phase!=last)
            {
                last=phase;firstCrack.SetActive(phase>=1);secondCrack.SetActive(phase>=2);
                plaque.text="INSPECTION 07\n"+(phase==0?"TIME ALLOCATED":phase==1?"TIME REPEATED":phase==2?"TIME UNACCOUNTED":"TIME RETAINED");
            }
            float target=Mathf.Min(phase,2)*90;
            angle=memory.flow.ReducedMotion?target:Mathf.MoveTowardsAngle(angle,target,Time.deltaTime*100);
            hand.localRotation=Quaternion.Euler(0,0,-angle);
        }
    }
}
