using UnityEngine;
namespace Xiaoduan.Room3
{
    public sealed class Room3CorePulse : MonoBehaviour
    {
        public Renderer[] edges;
        MaterialPropertyBlock properties;
        void Awake(){properties=new MaterialPropertyBlock();}
        void Update()
        {
            properties.SetColor("_EmissionColor",new Color(.65f,3.6f,.7f)*(.84f+.16f*Mathf.Sin(Time.time*2.1f)));
            foreach(var edge in edges)if(edge!=null)edge.SetPropertyBlock(properties);
        }
    }
}
