using UnityEngine;

namespace Xiaoduan.Narrative
{
    [DisallowMultipleComponent]
    public sealed class NarrativeInspectable : MonoBehaviour
    {
        public NarrativeEntry entry;
        public Renderer focusRenderer;
        [Min(.5f)] public float interactionDistance=3f;
        public Bounds FocusBounds => focusRenderer!=null ? focusRenderer.bounds : new Bounds(transform.position,Vector3.one*.3f);
        public bool CanInspect => isActiveAndEnabled && gameObject.activeInHierarchy && entry!=null;
    }
}
