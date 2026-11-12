using UnityEngine;
using Xiaoduan.Narrative;
namespace Xiaoduan.Survey
{
    public sealed class SurveyScanReveal : MonoBehaviour
    {
        public Renderer surface;
        public NarrativeInspectable inspectable;
        bool discovered;
        MaterialPropertyBlock block;
        void Awake(){block=new MaterialPropertyBlock();}
        void Update()
        {
            bool scan=SpatialScanEvent.Active!=null&&SpatialScanEvent.Active.IsScanning;
            if(scan||SurveyProgress.Has(2))discovered=true;
            if(surface!=null)
            {
                surface.enabled=discovered;
                block.SetColor("_EmissionColor",new Color(.12f,.9f,.35f)*(scan?3:.5f));surface.SetPropertyBlock(block);
            }
            if(inspectable!=null)inspectable.enabled=discovered;
        }
    }
}
