using UnityEngine;
namespace Xiaoduan.Room3
{
    [DefaultExecutionOrder(400)]
    public sealed class Room3ArtDirection:MonoBehaviour
    {
        public Room3EmbodiedExpansion expansion;
        public Transform pressureNeedle;
        public Transform[] coolingRotors;
        Camera eye;
        Color originalBackground;
        void Start(){eye=Camera.main;if(eye!=null){originalBackground=eye.backgroundColor;eye.backgroundColor=new Color(.006f,.009f,.012f);}}
        void Update()
        {
            var f=expansion.flow;if(Xiaoduan.Survey.SurveyHUD.BlocksWorld||f.Reading||f.ui.DecisionVisible||f.EndingChosen)return;
            if(pressureNeedle!=null)pressureNeedle.localRotation=Quaternion.Slerp(pressureNeedle.localRotation,Quaternion.Euler(0,0,135-expansion.Pressure*270),Time.deltaTime*8);
            foreach(var rotor in coolingRotors)rotor.Rotate(0,0,Time.deltaTime*(expansion.fragments[1].Retained?4:23),Space.Self);
        }
        void LateUpdate(){if(eye!=null&&!expansion.flow.EndingChosen)eye.backgroundColor=new Color(.006f,.009f,.012f);}
        void OnDisable(){if(eye!=null)eye.backgroundColor=originalBackground;}
    }
}
