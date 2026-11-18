using UnityEngine;
namespace Xiaoduan.Room3
{
    [DefaultExecutionOrder(450)]
    public sealed class RocketboxAtmosphere:MonoBehaviour
    {
        public Room3StoryFlow flow;
        public Renderer[] ceiling;
        MaterialPropertyBlock block;
        void Awake(){block=new MaterialPropertyBlock();}
        void LateUpdate()
        {
            var c=new Color(.8f,.007f,.004f);
            if(flow.ui.DecisionVisible){if(flow.embodied.Preview==0)c=Color.black;else if(flow.embodied.Preview==1)c=new Color(.1f,.8f,.25f);}
            if(flow.EndingChosen)c=Color.black;
            block.SetColor("_BaseColor",c);foreach(var r in ceiling)if(r!=null)r.SetPropertyBlock(block);
        }
    }
}
