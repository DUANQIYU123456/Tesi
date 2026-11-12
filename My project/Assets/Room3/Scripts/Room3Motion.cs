using UnityEngine;

namespace Xiaoduan.Room3
{
    /// <summary>Decorative movement only. Walkable platforms never move.</summary>
    public sealed class Room3Motion : MonoBehaviour
    {
        public Vector3 angularVelocity;
        public float bobAmplitude = .15f, bobFrequency = .15f, phase;
        Vector3 origin;
        Quaternion rotation;
        void OnEnable() { origin = transform.localPosition; rotation = transform.localRotation; }
        void Update()
        {
            float t = Time.time;
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(t * bobFrequency + phase) * bobAmplitude);
            transform.localRotation = rotation * Quaternion.Euler(angularVelocity * t);
        }
    }
}
