using TMPro;
using UnityEngine;

namespace Xiaoduan.Narrative
{
    [CreateAssetMenu(menuName="Xiaoduan/Narrative/UI Theme")]
    public sealed class NarrativeTheme : ScriptableObject
    {
        public TMP_FontAsset font;
        public Color text=new Color(.94f,.93f,.86f);
        public Color muted=new Color(.64f,.69f,.65f);
        public Color accent=new Color(.55f,.91f,.47f);
        public Color panel=new Color(.035f,.055f,.048f,.96f);
        public string enterLabel="Enter";
        public string examineLabel="Examine";
        public string thresholdLabel="THRESHOLD";
        public string objectLabel="OBSERVATION";
        public string portalTitle="The light beyond";
        public string closeLabel="Close";
        public string returnLabel="E / ESC  -  Return to exploration";
        public string room1Label="01  /  THE TOWN";
        public string room2Label="02  /  THE PRISON";
        public string introLabel="WASD  Move     MOUSE  Look     E  Interact";
        public float fadeSeconds=.22f;
        public float promptSize=30f;
        public float captionSize=20f;
        public float titleSize=48f;
        public float bodySize=30f;
    }
}
