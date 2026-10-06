using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace MentalHealthApp.Tracking
{
    public class AvatarHandIKController : MonoBehaviour
    {
        [Header("Target Arms & Palms")]
        public Transform leftForearm;
        public Transform leftPalm;
        public Transform rightForearm;
        public Transform rightPalm;

        [Header("VR Camera Eye Point")]
        public Camera vrCamera;

        [Header("Hand Tracking Settings")]
        public Vector3 leftHandRestOffset = new Vector3(-0.25f, -0.35f, 0.45f);
        public Vector3 rightHandRestOffset = new Vector3(0.25f, -0.35f, 0.45f);
        public float ikSmoothing = 12f;

        private Vector3 targetLeftPos;
        private Quaternion targetLeftRot;
        private Vector3 targetRightPos;
        private Quaternion targetRightRot;

        private bool isVRLeftTracked = false;
        private bool isVRRightTracked = false;

        private void Start()
        {
            if (vrCamera == null) vrCamera = Camera.main;
            FindHandsIfNull();
        }

        public void FindHandsIfNull()
        {
            if (leftPalm == null)
            {
                Transform lp = transform.Find("Palm_-1");
                if (lp != null) leftPalm = lp;
            }
            if (rightPalm == null)
            {
                Transform rp = transform.Find("Palm_1");
                if (rp != null) rightPalm = rp;
            }
            if (leftForearm == null)
            {
                Transform lf = transform.Find("Forearm_-1");
                if (lf != null) leftForearm = lf;
            }
            if (rightForearm == null)
            {
                Transform rf = transform.Find("Forearm_1");
                if (rf != null) rightForearm = rf;
            }
        }

        private void LateUpdate()
        {
            if (vrCamera == null) vrCamera = Camera.main;
            FindHandsIfNull();

            // 1. Read Native VR Controller Position & Rotation
            SampleVRControllers();

            // 2. Apply Hand Movement in 1st Person POV
            UpdateHandTransforms();
        }

        private void SampleVRControllers()
        {
            isVRLeftTracked = false;
            isVRRightTracked = false;

            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            if (leftDevice.isValid && leftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 lPos) && leftDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion lRot))
            {
                isVRLeftTracked = true;
                targetLeftPos = transform.TransformPoint(lPos);
                targetLeftRot = transform.rotation * lRot;
            }

            if (rightDevice.isValid && rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rPos) && rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rRot))
            {
                isVRRightTracked = true;
                targetRightPos = transform.TransformPoint(rPos);
                targetRightRot = transform.rotation * rRot;
            }

            // Desktop Fallback: If VR controller is not connected, calculate dynamic hand position from camera view + mouse movement
            if (!isVRLeftTracked && vrCamera != null)
            {
                targetLeftPos = vrCamera.transform.TransformPoint(leftHandRestOffset);
                targetLeftRot = vrCamera.transform.rotation * Quaternion.Euler(15f, 20f, -10f);
            }

            if (!isVRRightTracked && vrCamera != null)
            {
                Vector3 currentMousePos = GetCrossSystemMousePos();
                Vector3 mouseNormalized = new Vector3((currentMousePos.x / Screen.width) - 0.5f, (currentMousePos.y / Screen.height) - 0.5f, 0);
                Vector3 dynamicRightOffset = rightHandRestOffset + new Vector3(mouseNormalized.x * 0.25f, mouseNormalized.y * 0.18f, 0);
                
                targetRightPos = vrCamera.transform.TransformPoint(dynamicRightOffset);
                targetRightRot = vrCamera.transform.rotation * Quaternion.Euler(15f + (mouseNormalized.y * -20f), -20f + (mouseNormalized.x * 20f), 10f);
            }
        }

        private Vector3 GetCrossSystemMousePos()
        {
            // Try New Input System via Reflection first to prevent InvalidOperationException
            try
            {
                System.Type mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
                if (mouseType != null)
                {
                    System.Reflection.PropertyInfo currentMouseProp = mouseType.GetProperty("current", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object currentMouse = currentMouseProp?.GetValue(null);

                    if (currentMouse != null)
                    {
                        System.Reflection.PropertyInfo positionProp = mouseType.GetProperty("position");
                        object posControl = positionProp?.GetValue(currentMouse);
                        if (posControl != null)
                        {
                            System.Reflection.MethodInfo readValueMethod = posControl.GetType().GetMethod("ReadValue");
                            if (readValueMethod != null)
                            {
                                Vector2 val = (Vector2)readValueMethod.Invoke(posControl, null);
                                return new Vector3(val.x, val.y, 0);
                            }
                        }
                    }
                }
            }
            catch { }

            // Fallback to legacy Input safely
            try
            {
                return Input.mousePosition;
            }
            catch
            {
                return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            }
        }

        private void UpdateHandTransforms()
        {
            float t = Time.deltaTime * ikSmoothing;

            if (leftPalm != null)
            {
                leftPalm.position = Vector3.Lerp(leftPalm.position, targetLeftPos, t);
                leftPalm.rotation = Quaternion.Slerp(leftPalm.rotation, targetLeftRot, t);

                if (leftForearm != null)
                {
                    Vector3 midPoint = (transform.position + leftPalm.position) * 0.5f;
                    leftForearm.position = Vector3.Lerp(leftForearm.position, midPoint, t);
                    leftForearm.rotation = Quaternion.Slerp(leftForearm.rotation, Quaternion.LookRotation((leftPalm.position - leftForearm.position).normalized), t);
                }
            }

            if (rightPalm != null)
            {
                rightPalm.position = Vector3.Lerp(rightPalm.position, targetRightPos, t);
                rightPalm.rotation = Quaternion.Slerp(rightPalm.rotation, targetRightRot, t);

                if (rightForearm != null)
                {
                    Vector3 midPoint = (transform.position + rightPalm.position) * 0.5f;
                    rightForearm.position = Vector3.Lerp(rightForearm.position, midPoint, t);
                    rightForearm.rotation = Quaternion.Slerp(rightForearm.rotation, Quaternion.LookRotation((rightPalm.position - rightForearm.position).normalized), t);
                }
            }
        }
    }
}
