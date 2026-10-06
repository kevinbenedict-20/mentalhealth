using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace MentalHealthApp.Tracking
{
    public class VRHandMovementTracker : MonoBehaviour
    {
        [Header("Hardware Detection")]
        public bool handTrackingAvailable = false;
        public bool fullBodyTrackingAvailable = false;
        public bool facialTrackingAvailable = false;

        public string handTrackingStatus = "Standard Controller Input";
        public string facialTrackingStatus = "Facial-expression data not available on current hardware.";

        [Header("Hand Metrics")]
        public float leftHandMovementSpeed = 0f;
        public float rightHandMovementSpeed = 0f;
        public int gestureFrequencyCount = 0;
        public float totalHandDistanceTraveled = 0f;

        private Vector3 lastLeftPos;
        private Vector3 lastRightPos;
        private bool isInitialized = false;

        public void Initialize()
        {
            DetectHardwareCapabilities();
        }

        public void DetectHardwareCapabilities()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);

            handTrackingAvailable = devices.Count > 0;
            fullBodyTrackingAvailable = false; // Do not fake full body leg/torso data per requirements
            facialTrackingAvailable = false;   // Do not fake facial tracking data per requirements

            handTrackingStatus = handTrackingAvailable ? "VR Controllers Active" : "Mouse/Head Orientation Active";
            facialTrackingStatus = "Facial-expression data not available on current hardware.";
        }

        public void Sample(float deltaTime)
        {
            if (!handTrackingAvailable) return;

            InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (leftController.isValid && leftController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPos))
            {
                if (isInitialized && deltaTime > 0.0001f)
                {
                    leftHandMovementSpeed = Vector3.Distance(leftPos, lastLeftPos) / deltaTime;
                    totalHandDistanceTraveled += Vector3.Distance(leftPos, lastLeftPos);
                }
                lastLeftPos = leftPos;
            }

            if (rightController.isValid && rightController.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos))
            {
                if (isInitialized && deltaTime > 0.0001f)
                {
                    rightHandMovementSpeed = Vector3.Distance(rightPos, lastRightPos) / deltaTime;
                    totalHandDistanceTraveled += Vector3.Distance(rightPos, lastRightPos);
                }
                lastRightPos = rightPos;
            }

            if (leftHandMovementSpeed > 0.3f || rightHandMovementSpeed > 0.3f)
            {
                gestureFrequencyCount++;
            }

            isInitialized = true;
        }

        public void ResetMetrics()
        {
            totalHandDistanceTraveled = 0f;
            leftHandMovementSpeed = 0f;
            rightHandMovementSpeed = 0f;
            gestureFrequencyCount = 0;
            isInitialized = false;
        }
    }
}
