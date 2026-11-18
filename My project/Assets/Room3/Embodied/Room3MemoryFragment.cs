using UnityEngine;
namespace Xiaoduan.Room3
{
    public sealed class Room3MemoryFragment : MonoBehaviour
    {
        public string title,retainedText;
        public Transform focus,visual;
        public bool Retained {get;internal set;}
        public float Progress {get;internal set;}
        Vector3 origin;
        float motionTime;
        Room3StoryFlow story;
        void Start(){origin=visual.localPosition;story=FindFirstObjectByType<Room3StoryFlow>();}
        void Update()
        {
            if(Xiaoduan.Survey.SurveyHUD.BlocksWorld||story!=null&&(story.Reading||story.ui.DecisionVisible||story.EndingChosen))return;
            if(story!=null&&story.ReducedMotion){visual.localPosition=origin;visual.localRotation=Quaternion.identity;return;}
            motionTime+=Time.deltaTime;
            visual.localPosition=origin+Vector3.up*(Mathf.Sin(motionTime*.65f+transform.position.z)*.025f);
            visual.localRotation=Quaternion.Euler(0,Mathf.Sin(motionTime*.3f)*3,Mathf.Sin(motionTime*.55f));
        }
    }
}
