using UnityEngine;
using UnityEngine.XR;
using System;
using System.Reflection;

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

            // Safe cross-input system mouse reading without throwing InvalidOperationException
            bool mousePressed = false;
            bool mouseReleased = false;
            Vector3 mousePos = Vector3.zero;

            GetCrossSystemMouseInput(out mousePressed, out mouseReleased, out mousePos);

            if (mousePressed)
            {
                isDragging = true;
                lastMousePosition = mousePos;
            }
            if (mouseReleased)
            {
                isDragging = false;
            }

            if (isDragging && isFirstPersonPOV)
            {
                Vector3 delta = mousePos - lastMousePosition;
                lastMousePosition = mousePos;

                currentYaw += delta.x * mouseSensitivity * 0.1f;
                currentPitch -= delta.y * mouseSensitivity * 0.1f;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

                transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            }
        }

        private void GetCrossSystemMouseInput(out bool mousePressed, out bool mouseReleased, out Vector3 mousePos)
        {
            mousePressed = false;
            mouseReleased = false;
            mousePos = Vector3.zero;

            // Try New Input System via Reflection first to prevent InvalidOperationException
            Type mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                PropertyInfo currentMouseProp = mouseType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                object currentMouse = currentMouseProp?.GetValue(null);

                if (currentMouse != null)
                {
                    // Read position
                    PropertyInfo positionProp = mouseType.GetProperty("position");
                    object posControl = positionProp?.GetValue(currentMouse);
                    if (posControl != null)
                    {
                        MethodInfo readValueMethod = posControl.GetType().GetMethod("ReadValue");
                        if (readValueMethod != null)
                        {
                            Vector2 val = (Vector2)readValueMethod.Invoke(posControl, null);
                            mousePos = new Vector3(val.x, val.y, 0);
                        }
                    }

                    // Read mouse button press
                    PropertyInfo rightButtonProp = mouseType.GetProperty("rightButton");
                    PropertyInfo leftButtonProp = mouseType.GetProperty("leftButton");
                    object btnControl = rightButtonProp?.GetValue(currentMouse) ?? leftButtonProp?.GetValue(currentMouse);
                    if (btnControl != null)
                    {
                        PropertyInfo pressedProp = btnControl.GetType().GetProperty("wasPressedThisFrame");
                        PropertyInfo releasedProp = btnControl.GetType().GetProperty("wasReleasedThisFrame");
                        PropertyInfo isPressedProp = btnControl.GetType().GetProperty("isPressed");

                        if (pressedProp != null && (bool)pressedProp.GetValue(btnControl)) mousePressed = true;
                        if (releasedProp != null && (bool)releasedProp.GetValue(btnControl)) mouseReleased = true;
                        if (isPressedProp != null && (bool)isPressedProp.GetValue(btnControl) && !mousePressed) mousePressed = true;
                    }
                    return;
                }
            }

            // Legacy Input fallback wrapped safely
            try
            {
                mousePos = Input.mousePosition;
                mousePressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
                mouseReleased = Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1);
            }
            catch
            {
                // Swallowed safely if New Input System package is active
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
