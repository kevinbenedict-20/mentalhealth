using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace MentalHealthApp.Core
{
    public class VRHeadLookController : MonoBehaviour
    {
        [Header("VR & Look Settings")]
        public bool isFirstPersonPOV = true;
        public float mouseSensitivity = 2.5f;
        public float minPitch = -75f;
        public float maxPitch = 75f;

        [Header("VR Seat Reference")]
        public Transform studentSeatTransform;
        public Vector3 seatEyeOffset = new Vector3(0, 1.35f, 0);

        private float currentYaw = 0f;
        private float currentPitch = 0f;
        private bool isDragging = false;
        private Vector3 lastMousePosition;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;
        }

        private void Update()
        {
            // Check for Native VR Headset tracking first
            if (XRSettings.isDeviceActive && XRSettings.enabled)
            {
                InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (headDevice.isValid && headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation))
                {
                    transform.localRotation = headRotation;
                    return;
                }
            }

            // Desktop VR Simulation: Mouse Drag / Right-Click Look Around
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging && isFirstPersonPOV)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;

                currentYaw += delta.x * mouseSensitivity * 0.1f;
                currentPitch -= delta.y * mouseSensitivity * 0.1f;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

                transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            }
        }

        public void SetSeatPOVPosition(Vector3 seatPos, Quaternion seatRot)
        {
            isFirstPersonPOV = true;
            transform.position = seatPos + seatEyeOffset;

            // Face table center
            Vector3 lookDir = (Vector3.zero - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                currentYaw = targetRot.eulerAngles.y;
                currentPitch = 5f;
                transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            }
        }
    }
}
