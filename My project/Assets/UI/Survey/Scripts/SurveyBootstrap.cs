using UnityEngine;
namespace Xiaoduan.Survey
{
    // Scene-visible entry point; the shared canvas itself persists across portals.
    public sealed class SurveyBootstrap:MonoBehaviour
    {
        public GameObject interfacePrefab;
        void Awake(){if(SurveyHUD.Instance==null&&interfacePrefab!=null)Instantiate(interfacePrefab);}
    }
}
