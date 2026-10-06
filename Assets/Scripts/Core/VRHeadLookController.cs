using UnityEngine;
using UnityEngine.XR;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MentalHealthApp.Core
{
    public class VRHeadLookController : MonoBehaviour
    {
        [Header("VR & Immersion Settings")]
        public bool isFirstPersonPOV = true;
        public float mouseSensitivity = 2.5f;
        public float minPitch = -85f;
        public float maxPitch = 85f;

        [Header("Meta Quest 3 6DoF & Seated Eye Scale")]
        public Vector3 seatEyeOffset = new Vector3(0, 1.18f, 0); // Seated 1:1 scale at table eye-level
        public bool enable6DoFPositionalTracking = true;
        public bool enable3DSpatialAudio = true;
        public float positionalDamping = 8.0f;

        [Header("VR Controller / Pointer Interaction")]
        public bool enableQuestControllerRay = true;
        public LayerMask uiLayerMask = -1;

        private Vector3 baseSeatPosition = new Vector3(-0.95f, 0, -1.35f);
        private Quaternion baseSeatRotation = Quaternion.identity;
        private float currentYaw = 0f;
        private float currentPitch = 0f;
        private bool isDragging = false;
        private Vector3 lastMousePosition;
        private AudioSource spatialAudioSource;

        private Vector3 target6DoFPosOffset = Vector3.zero;
        private Vector3 current6DoFPosOffset = Vector3.zero;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;

            SetupSpatialAudio();
        }

        private void SetupSpatialAudio()
        {
            spatialAudioSource = GetComponent<AudioSource>();
            if (spatialAudioSource == null)
            {
                spatialAudioSource = gameObject.AddComponent<AudioSource>();
            }
            spatialAudioSource.spatialBlend = 1.0f; // 100% 3D Spatial Audio for VR headsets
            spatialAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            spatialAudioSource.minDistance = 0.5f;
            spatialAudioSource.maxDistance = 15f;
            spatialAudioSource.dopplerLevel = 0f;
            spatialAudioSource.playOnAwake = false;
        }

        public void PlaySpatialAvatarChime(Vector3 avatarHeadPosition)
        {
            if (!enable3DSpatialAudio || spatialAudioSource == null) return;
            
            // Create a temporary spatial sound point at the speaking avatar's head position
            GameObject tempAudioObj = new GameObject("VR_SpatialAudio_Point");
            tempAudioObj.transform.position = avatarHeadPosition;
            AudioSource src = tempAudioObj.AddComponent<AudioSource>();
            src.spatialBlend = 1.0f;
            src.minDistance = 0.8f;
            src.maxDistance = 12.0f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.dopplerLevel = 0f;

            // Generate a subtle 3D spatial attention ping
            AudioClip pingClip = CreateSpatialPingClip();
            src.clip = pingClip;
            src.volume = 0.35f;
            src.Play();
            Destroy(tempAudioObj, 1.2f);
        }

        private AudioClip CreateSpatialPingClip()
        {
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.15f);
            float[] samples = new float[length];
            float freq = 587.33f; // D5 tone for gentle spatial alert

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 25.0f);
                samples[i] = Mathf.Sin(2.0f * Mathf.PI * freq * t) * envelope * 0.3f;
            }

            AudioClip clip = AudioClip.Create("VRPing", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void Update()
        {
            // 1. Meta Quest 3 Native 6DoF Headset Tracking
            bool isVRHeadsetTracked = false;
            if (XRSettings.isDeviceActive && XRSettings.enabled)
            {
                InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (headDevice.isValid)
                {
                    bool gotRot = headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion headRotation);
                    bool gotPos = headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition);

                    if (gotRot)
                    {
                        transform.localRotation = headRotation;
                        isVRHeadsetTracked = true;
                    }

                    if (gotPos && enable6DoFPositionalTracking)
                    {
                        target6DoFPosOffset = headPosition;
                        current6DoFPosOffset = Vector3.Lerp(current6DoFPosOffset, target6DoFPosOffset, Time.deltaTime * positionalDamping);
                        transform.position = baseSeatPosition + seatEyeOffset + current6DoFPosOffset;
                    }
                    else
                    {
                        transform.position = baseSeatPosition + seatEyeOffset;
                    }
                }
            }

            // 2. Meta Quest 3 Touch Controller & Trigger Selection Raycast
            CheckQuestControllerInput();

            if (isVRHeadsetTracked) return;

            // 3. Desktop / Mouse & Keyboard Fallback Controls
            float keyYawDelta = 0f;
            float keyPitchDelta = 0f;

            Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            if (keyboardType != null)
            {
                PropertyInfo currentKBProp = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                object currentKB = currentKBProp?.GetValue(null);
                if (currentKB != null)
                {
                    PropertyInfo leftKey = keyboardType.GetProperty("leftArrowKey") ?? keyboardType.GetProperty("aKey");
                    PropertyInfo rightKey = keyboardType.GetProperty("rightArrowKey") ?? keyboardType.GetProperty("dKey");
                    PropertyInfo upKey = keyboardType.GetProperty("upArrowKey") ?? keyboardType.GetProperty("wKey");
                    PropertyInfo downKey = keyboardType.GetProperty("downArrowKey") ?? keyboardType.GetProperty("sKey");

                    if (IsKeyPressed(currentKB, leftKey) || IsKeyPressed(currentKB, keyboardType.GetProperty("aKey"))) keyYawDelta -= 60f * Time.deltaTime;
                    if (IsKeyPressed(currentKB, rightKey) || IsKeyPressed(currentKB, keyboardType.GetProperty("dKey"))) keyYawDelta += 60f * Time.deltaTime;
                    if (IsKeyPressed(currentKB, upKey) || IsKeyPressed(currentKB, keyboardType.GetProperty("wKey"))) keyPitchDelta -= 40f * Time.deltaTime;
                    if (IsKeyPressed(currentKB, downKey) || IsKeyPressed(currentKB, keyboardType.GetProperty("sKey"))) keyPitchDelta += 40f * Time.deltaTime;
                }
            }

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

            if (isFirstPersonPOV)
            {
                if (isDragging)
                {
                    Vector3 delta = mousePos - lastMousePosition;
                    lastMousePosition = mousePos;

                    currentYaw += delta.x * mouseSensitivity * 0.1f;
                    currentPitch -= delta.y * mouseSensitivity * 0.1f;
                }

                currentYaw += keyYawDelta;
                currentPitch += keyPitchDelta;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

                transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
                transform.position = baseSeatPosition + seatEyeOffset;
            }
        }

        private void CheckQuestControllerInput()
        {
            if (!enableQuestControllerRay) return;

            // Check Right & Left Quest Touch Controllers
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            bool triggerPressed = false;
            if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTrig) && rightTrig) triggerPressed = true;
            if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTrig) && leftTrig) triggerPressed = true;

            // Perform Gaze / Quest Pointer Raycast from VR Head Camera
            if (triggerPressed || Input.GetMouseButtonDown(0))
            {
                Ray ray = new Ray(transform.position, transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                {
                    UnityEngine.UI.Button btn = hit.collider.GetComponent<UnityEngine.UI.Button>();
                    if (btn != null && btn.interactable)
                    {
                        btn.onClick.Invoke();
                    }
                }
            }
        }

        private bool IsKeyPressed(object kb, PropertyInfo keyProp)
        {
            if (kb == null || keyProp == null) return false;
            object keyControl = keyProp.GetValue(kb);
            if (keyControl != null)
            {
                PropertyInfo isPressedProp = keyControl.GetType().GetProperty("isPressed");
                if (isPressedProp != null) return (bool)isPressedProp.GetValue(keyControl);
            }
            return false;
        }

        private void GetCrossSystemMouseInput(out bool mousePressed, out bool mouseReleased, out Vector3 mousePos)
        {
            mousePressed = false;
            mouseReleased = false;
            mousePos = Vector3.zero;

            Type mouseType = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                PropertyInfo currentMouseProp = mouseType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
                object currentMouse = currentMouseProp?.GetValue(null);

                if (currentMouse != null)
                {
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

            try
            {
                mousePos = Input.mousePosition;
                mousePressed = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
                mouseReleased = Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1);
            }
            catch { }
        }

        public void SetSeatPOVPosition(Vector3 seatPos, Quaternion seatRot)
        {
            isFirstPersonPOV = true;
            baseSeatPosition = seatPos;
            baseSeatRotation = seatRot;
            transform.position = baseSeatPosition + seatEyeOffset;

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
