using UnityEngine;
using System;
using System.Collections.Generic;

namespace MentalHealthApp.Tracking
{
    public class VRGazeEstimator : MonoBehaviour
    {
        [Header("Gaze Estimation Parameters")]
        public float gazeAngleThreshold = 22.0f; // degrees
        public float gazeAwayThreshold = 35.0f;  // degrees
        public float minimumAttentionDuration = 0.5f; // seconds

        // Hardware info
        public bool hasTrueEyeTracking = false;
        public string gazeTrackingLabel = "HEAD ORIENTATION / GAZE ESTIMATION";

        private Camera vrCamera;

        public void Initialize(Camera cam)
        {
            vrCamera = cam;
            // Check if eye-tracking hardware feature is available
            hasTrueEyeTracking = false; // Default unless explicit eye tracker hardware is detected
            gazeTrackingLabel = hasTrueEyeTracking ? "TRUE EYE TRACKING" : "HEAD ORIENTATION / GAZE ESTIMATION";
        }

        public bool IsLookingAtTarget(Vector3 targetPosition, out float angle)
        {
            angle = 180f;
            if (vrCamera == null) return false;

            Vector3 camPos = vrCamera.transform.position;
            Vector3 camForward = vrCamera.transform.forward;

            Vector3 dirToTarget = (targetPosition - camPos).normalized;
            angle = Vector3.Angle(camForward, dirToTarget);

            return angle <= gazeAngleThreshold;
        }

        public int DetermineTargetParticipant(List<Transform> participantTransforms, out float smallestAngle)
        {
            smallestAngle = 180f;
            int bestIndex = -1;

            if (vrCamera == null || participantTransforms == null) return -1;

            for (int i = 0; i < participantTransforms.Count; i++)
            {
                Transform t = participantTransforms[i];
                if (t == null) continue;

                Vector3 targetHeadPos = t.position + new Vector3(0, 0.96f, 0); // Head height
                if (IsLookingAtTarget(targetHeadPos, out float angle))
                {
                    if (angle < smallestAngle)
                    {
                        smallestAngle = angle;
                        bestIndex = i;
                    }
                }
            }

            return bestIndex;
        }
    }
}
