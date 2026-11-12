using UnityEngine;

namespace Xiaoduan.Room3
{
    // Portal mapping and projected screen sampling adapted from Zbajnek/urp-portals,
    // revision 13dcdb21d972d5f1f6df4a9506d198524bf044c2 (MIT, 2026 Zbajnek).
    // The controller, render-request scheduling and narrative graph are project-specific.
    public sealed class ResidualPortal : MonoBehaviour
    {
        public enum Purpose { Entrance, Arrival, Repetition, Release }
        public Purpose purpose;
        public ResidualPortal linked;
        public MeshRenderer screen;
        public Vector2 opening = new Vector2(3.2f,3.8f);
        public RenderTexture View { get; private set; }
        Material runtimeMaterial;
        public Material ScreenMaterial
        {
            get
            {
                if(runtimeMaterial==null)runtimeMaterial=screen.material;
                return runtimeMaterial;
            }
        }
        public Matrix4x4 Mapping => linked.transform.localToWorldMatrix *
            Matrix4x4.Rotate(Quaternion.Euler(0,180,0)) * transform.worldToLocalMatrix;
        public void SetView(RenderTexture view)
        {
            View=view;ScreenMaterial.SetTexture("_MainTex",view);
            ScreenMaterial.SetInt("displayMask",view!=null?1:0);
        }
        public bool Visible(Camera camera)
        {
            if(!isActiveAndEnabled||linked==null||!screen.enabled)return false;
            if(Vector3.Dot(camera.transform.position-transform.position,transform.forward)<-.04f)return false;
            return GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(camera),screen.bounds);
        }
        public bool Crossed(Vector3 before,Vector3 after)
        {
            if(linked==null||!isActiveAndEnabled)return false;
            Vector3 a=transform.InverseTransformPoint(before),b=transform.InverseTransformPoint(after);
            if(a.z<=0||b.z>0)return false;
            float t=a.z/(a.z-b.z);Vector3 crossing=Vector3.Lerp(a,b,t);
            return Mathf.Abs(crossing.x)<opening.x*.5f-.1f&&
                crossing.y> -opening.y*.5f-.2f&&crossing.y<opening.y*.5f;
        }
        public void ProtectNearPlane(Camera camera)
        {
            float half=camera.nearClipPlane*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad);
            float depth=new Vector3(half*camera.aspect,half,camera.nearClipPlane).magnitude+.015f;
            bool front=Vector3.Dot(camera.transform.position-transform.position,transform.forward)>0;
            screen.transform.localScale=new Vector3(opening.x,opening.y,depth);
            screen.transform.localPosition=Vector3.forward*(front?-.5f:.5f)*depth;
        }
        void OnDestroy(){if(runtimeMaterial!=null)Destroy(runtimeMaterial);}
    }
}
