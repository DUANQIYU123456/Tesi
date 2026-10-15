using UnityEngine;
namespace Xiaoduan.Narrative
{
    [CreateAssetMenu(menuName="Xiaoduan/Narrative/Observation")]
    public sealed class NarrativeEntry : ScriptableObject
    {
        public string title="Green liquid";
        public string category="SEALED VESSEL / 01";
        [TextArea(4,12)] public string body="A sealed bottle. The liquid brightens as you approach.\n\nThe light reveals your presence. It does not reveal what is inside.";
    }
}
