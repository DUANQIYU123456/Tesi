using UnityEngine;

namespace Xiaoduan.Room3
{
    public sealed class Room3StoryNode : MonoBehaviour
    {
        public enum Kind { MedicalRecord, CourtRecord, Decision, LaborLedger }
        public Kind kind;
        public Transform focus;
        public string label;
    }
}

