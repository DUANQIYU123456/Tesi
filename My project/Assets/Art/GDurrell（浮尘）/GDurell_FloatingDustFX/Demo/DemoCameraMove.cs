using System.Collections.Generic;
using UnityEngine;

namespace GDurell
{
    public class DemoCameraMove : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("List of Transforms acting as waypoints.")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Tooltip("Movement speed towards the waypoint.")]
        [SerializeField] private float moveSpeed = 1f;

        [Tooltip("Rotation speed towards the waypoint direction.")]
        [SerializeField] private float rotationSpeed = 1f;

        [Tooltip("Minimum distance to consider a waypoint reached.")]
        [SerializeField] private float reachThreshold = 0.2f;

        private int currentIndex = 0;

        private void Update()
        {
            if (waypoints == null || waypoints.Count == 0) return; // Exit if no waypoints are assigned

            Transform targetWaypoint = waypoints[currentIndex]; // Get the current active waypoint

            // Move straight towards the target waypoint position
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, moveSpeed * Time.deltaTime);

            // Rotate smoothly to match the movement direction
            Vector3 direction = (targetWaypoint.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Check if we reached the current waypoint to switch to the next one
            if (Vector3.Distance(transform.position, targetWaypoint.position) <= reachThreshold)
            {
                currentIndex = (currentIndex + 1) % waypoints.Count;
            }
        }
    }
}