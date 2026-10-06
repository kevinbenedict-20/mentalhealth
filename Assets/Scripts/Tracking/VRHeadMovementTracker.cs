using UnityEngine;

namespace MentalHealthApp.Tracking
{
    public class VRHeadMovementTracker : MonoBehaviour
    {
        [Header("Movement Metrics")]
        public float totalHeadDistanceTraveled = 0f;
        public float totalHeadRotationDegrees = 0f;
        public float currentHeadSpeed = 0f; // m/s
        public float currentAngularSpeed = 0f; // deg/s

        public int suddenHeadMovementsCount = 0;
        public float stillnessDuration = 0f;
        public string headMovementLevel = "Low"; // "Low", "Moderate", "High"

        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private bool isInitialized = false;

        public void Initialize(Transform headTransform)
        {
            if (headTransform != null)
            {
                lastPosition = headTransform.position;
                lastRotation = headTransform.rotation;
                isInitialized = true;
            }
        }

        public void Sample(Transform headTransform, float deltaTime)
        {
            if (!isInitialized || headTransform == null || deltaTime <= 0.0001f)
            {
                Initialize(headTransform);
                return;
            }

            Vector3 currentPos = headTransform.position;
            Quaternion currentRot = headTransform.rotation;

            float dist = Vector3.Distance(currentPos, lastPosition);
            float rotDeg = Quaternion.Angle(currentRot, lastRotation);

            currentHeadSpeed = dist / deltaTime;
            currentAngularSpeed = rotDeg / deltaTime;

            totalHeadDistanceTraveled += dist;
            totalHeadRotationDegrees += rotDeg;

            // Sudden movement threshold check
            if (currentAngularSpeed > 140f || currentHeadSpeed > 1.2f)
            {
                suddenHeadMovementsCount++;
            }

            // Stillness check
            if (currentHeadSpeed < 0.05f && currentAngularSpeed < 8f)
            {
                stillnessDuration += deltaTime;
            }
            else
            {
                stillnessDuration = 0f;
            }

            // Categorize head movement level
            if (currentAngularSpeed > 60f || currentHeadSpeed > 0.4f)
            {
                headMovementLevel = "High";
            }
            else if (currentAngularSpeed > 20f || currentHeadSpeed > 0.12f)
            {
                headMovementLevel = "Moderate";
            }
            else
            {
                headMovementLevel = "Low";
            }

            lastPosition = currentPos;
            lastRotation = currentRot;
        }

        public void ResetMetrics()
        {
            totalHeadDistanceTraveled = 0f;
            totalHeadRotationDegrees = 0f;
            currentHeadSpeed = 0f;
            currentAngularSpeed = 0f;
            suddenHeadMovementsCount = 0;
            stillnessDuration = 0f;
            headMovementLevel = "Low";
            isInitialized = false;
        }
    }
}
