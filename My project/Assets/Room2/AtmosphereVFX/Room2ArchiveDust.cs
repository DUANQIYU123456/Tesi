using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using Xiaoduan.Survey;
namespace Xiaoduan.Atmosphere
{
    public sealed class Room2ArchiveDust:MonoBehaviour
    {
        public VisualEffect[] pockets;
        public float[] opacities;
        IEnumerator Start()
        {
            yield return null;
            for(int i=0;i<pockets.Length;i++)if(pockets[i]!=null){pockets[i].pause=false;pockets[i].Simulate(1f/30,60);}
        }
        void LateUpdate()
        {
            bool modal=SurveyHUD.BlocksWorld||(SurveyHUD.Instance!=null&&SurveyHUD.Instance.IsReading);
            bool reduced=SurveyProgress.ReducedMotion;
            for(int i=0;i<pockets.Length;i++)if(pockets[i]!=null){
                var effect=pockets[i];effect.pause=modal||reduced;
                float target=modal?0:opacities[i]*(reduced?.6f:1);
                effect.SetFloat("Alpha",Mathf.MoveTowards(effect.GetFloat("Alpha"),target,Time.unscaledDeltaTime*.6f));
            }
        }
    }
}
