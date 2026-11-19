using System;
using UnityEngine;
using Xiaoduan.Survey;

namespace Xiaoduan.Room3
{
    // Navigation marks use existing footprint art. Progress remains owned by the story.
    [DefaultExecutionOrder(11000)]
    public sealed class Room3FootprintGuide : MonoBehaviour
    {
        [Serializable]
        public sealed class Route
        {
            public string label;
            public Vector3[] points;
            public Renderer[] marks;
            public float[] distances;
        }
        public Room3StoryFlow flow;
        public Room3EmbodiedExpansion entrance;
        public Route[] routes;
        public int ActiveRoute { get; private set; } = -1;
        public int VisibleMarks { get; private set; }
        public string CurrentDestination => ActiveRoute >= 0 ? routes[ActiveRoute].label : "";
        Transform player;
        float findAt;
        MaterialPropertyBlock properties;
        static readonly int Opacity = Shader.PropertyToID("_Opacity");
        static readonly int Tint = Shader.PropertyToID("_Tint");

        void Awake()
        {
            properties = new MaterialPropertyBlock();
            HideAll();
        }

        int SelectRoute()
        {
            if (flow == null || entrance == null || flow.EndingChosen) return -1;
            if (!entrance.DoorOpened) return 0;
            if (!flow.WorkerView || !flow.MedicalDecoded) return 1;
            var archive = flow.recursiveArchive;
            if (archive != null && archive.Inside)
                return archive.RecordDecoded ? 5 : archive.CanRead ? 4 : 3;
            if (archive != null && !archive.RecordDecoded) return 2;
            if (!flow.BridgeLocked) return 6;
            if (!flow.BridgeReady) return -1; // Never invite walking onto an unlocked deck.
            if (!flow.CourtDecoded) return 7;
            if (!flow.OathOpened) return 8;
            return 9;
        }

        void LateUpdate()
        {
            if (player == null || Time.unscaledTime >= findAt)
            {
                findAt = Time.unscaledTime + .5f;
                var candidate = GameObject.FindGameObjectWithTag("Player");
                player = candidate != null ? candidate.transform : null;
            }
            bool hidden = player == null || player.gameObject.scene != gameObject.scene ||
                flow == null || flow.Reading || flow.ui.DecisionVisible || SurveyHUD.BlocksWorld ||
                flow.EndingChosen;
            int wanted = hidden ? -1 : SelectRoute();
            if (wanted != ActiveRoute)
            {
                HideAll();
                ActiveRoute = wanted;
            }
            VisibleMarks = 0;
            if (ActiveRoute < 0 || ActiveRoute >= routes.Length) return;
            Route route = routes[ActiveRoute];
            float progress = ClosestDistance(route.points, player.position);
            // Fixed brightness: no strobes, timed pulses, or RMB requirement.
            Color color = flow.OathOpened ? new Color(.78f, .89f, .62f, 1) :
                new Color(.10f, .87f, .38f, 1);
            for (int i = 0; i < route.marks.Length; i++)
            {
                Renderer mark = route.marks[i];
                float ahead = route.distances[i] - progress;
                float distance = Vector3.Distance(mark.transform.position, player.position);
                float alpha = distance < 14f && ahead > -2.2f && ahead < 12f
                    ? Mathf.Min(Mathf.InverseLerp(14f, 9f, distance),
                        Mathf.InverseLerp(-2.2f, -.4f, ahead)) * .88f : 0;
                mark.enabled = alpha > .015f;
                if (!mark.enabled) continue;
                properties.Clear();
                properties.SetFloat(Opacity, alpha);
                properties.SetColor(Tint, color);
                mark.SetPropertyBlock(properties);
                VisibleMarks++;
            }
        }

        static float ClosestDistance(Vector3[] points, Vector3 position)
        {
            float closest = float.PositiveInfinity, result = 0, travelled = 0;
            Vector2 p = new Vector2(position.x, position.z);
            for (int i = 1; i < points.Length; i++)
            {
                Vector2 a = new Vector2(points[i-1].x, points[i-1].z);
                Vector2 delta = new Vector2(points[i].x-a.x, points[i].z-a.y);
                float length = delta.magnitude;
                float t = length > .001f ? Mathf.Clamp01(Vector2.Dot(p-a, delta) / (length*length)) : 0;
                float sqr = (p-a-delta*t).sqrMagnitude;
                if (sqr < closest) { closest = sqr; result = travelled+length*t; }
                travelled += length;
            }
            return result;
        }

        void HideAll()
        {
            if (routes == null) return;
            foreach (Route route in routes)
                if (route.marks != null)
                    foreach (Renderer mark in route.marks) if (mark != null) mark.enabled = false;
            VisibleMarks = 0;
        }
        void OnDisable() { HideAll(); ActiveRoute = -1; }
    }
}
