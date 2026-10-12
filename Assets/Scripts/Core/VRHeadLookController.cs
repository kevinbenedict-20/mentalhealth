using UnityEngine;
using UnityEngine.XR;
using System;
using System.Collections;
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
        public Vector3 seatEyeOffset = new Vector3(0, 0.94f, 0); // Natural eye socket level aligned with peer avatars
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

        private LineRenderer laserPointer;
        private bool wasTriggerPressedLastFrame = false;

        private void Awake()
        {
            AutoInitializeXRHeadset();
        }

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;

            SetupSpatialAudio();
            EnsureLaserPointer();
        }

        private void AutoInitializeXRHeadset()
        {
            try
            {
                XRSettings.enabled = true;

                Type xrGeneralType = Type.GetType("UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management");
                if (xrGeneralType != null)
                {
                    PropertyInfo instanceProp = xrGeneralType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    object instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        PropertyInfo managerProp = instance.GetType().GetProperty("Manager");
                        object manager = managerProp?.GetValue(instance);
                        if (manager != null)
                        {
                            MethodInfo startSubsystemsMethod = manager.GetType().GetMethod("StartSubsystems");
                            startSubsystemsMethod?.Invoke(manager, null);
                            Debug.Log("[VRXR] XR Subsystems started for Meta Quest headset display.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[VRXR] XR Auto-initialization note: " + ex.Message);
            }
        }

        private void SetupSpatialAudio()
        {
            spatialAudioSource = GetComponent<AudioSource>();
            if (spatialAudioSource == null)
            {
                spatialAudioSource = gameObject.AddComponent<AudioSource>();
            }
            spatialAudioSource.spatialBlend = 1.0f;
            spatialAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            spatialAudioSource.minDistance = 0.5f;
            spatialAudioSource.maxDistance = 15f;
            spatialAudioSource.dopplerLevel = 0f;
            spatialAudioSource.playOnAwake = false;
        }

        public void PlaySpatialAvatarChime(Vector3 avatarHeadPosition)
        {
            if (!enable3DSpatialAudio || spatialAudioSource == null) return;
            
            GameObject tempAudioObj = new GameObject("VR_SpatialAudio_Point");
            tempAudioObj.transform.position = avatarHeadPosition;
            AudioSource src = tempAudioObj.AddComponent<AudioSource>();
            src.spatialBlend = 1.0f;
            src.minDistance = 0.8f;
            src.maxDistance = 12.0f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.dopplerLevel = 0f;

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
            float freq = 587.33f;

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

        private void EnsureLaserPointer()
        {
            if (laserPointer == null)
            {
                GameObject laserObj = new GameObject("QuestLaserPointer");
                laserObj.transform.SetParent(transform, false);
                laserPointer = laserObj.AddComponent<LineRenderer>();
                laserPointer.startWidth = 0.005f;
                laserPointer.endWidth = 0.002f;
                laserPointer.material = new Material(Shader.Find("Sprites/Default"));
                laserPointer.startColor = new Color(0.2f, 0.8f, 1.0f, 0.8f);
                laserPointer.endColor = new Color(0.2f, 0.8f, 1.0f, 0.2f);
                laserPointer.positionCount = 2;
                laserPointer.enabled = false;
            }
        }

        private void Update()
        {
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

            CheckQuestControllerInput();

            if (isVRHeadsetTracked) return;

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
            EnsureLaserPointer();

            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            Ray ray;
            bool hasControllerRay = false;

            if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rPos) && rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rRot))
            {
                ray = new Ray(baseSeatPosition + seatEyeOffset + rPos, rRot * Vector3.forward);
                hasControllerRay = true;
            }
            else if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 lPos) && leftHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion lRot))
            {
                ray = new Ray(baseSeatPosition + seatEyeOffset + lPos, lRot * Vector3.forward);
                hasControllerRay = true;
            }
            else
            {
                ray = new Ray(transform.position, transform.forward);
            }

            bool isTriggerDown = false;
            if (rightHand.isValid)
            {
                if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rTrig) && rTrig) isTriggerDown = true;
                if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool rA) && rA) isTriggerDown = true;
            }
            if (leftHand.isValid)
            {
                if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool lTrig) && lTrig) isTriggerDown = true;
                if (leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool lX) && lX) isTriggerDown = true;
            }
            if (Input.GetMouseButton(0)) isTriggerDown = true;

            bool justPressed = isTriggerDown && !wasTriggerPressedLastFrame;
            wasTriggerPressedLastFrame = isTriggerDown;

            float rayLength = 5.0f;
            bool hitButton = false;

            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                rayLength = hit.distance;
                UnityEngine.UI.Button btn = hit.collider.GetComponentInParent<UnityEngine.UI.Button>();
                if (btn != null && btn.interactable)
                {
                    hitButton = true;
                    if (laserPointer != null)
                    {
                        laserPointer.startColor = new Color(0.2f, 1.0f, 0.4f, 0.9f);
                    }

                    if (justPressed)
                    {
                        Debug.Log("[Quest 3 Controller] Clicked Option Button: " + btn.name);
                        btn.onClick.Invoke();

                        if (rightHand.isValid)
                        {
                            HapticCapabilities caps;
                            if (rightHand.TryGetHapticCapabilities(out caps) && caps.supportsImpulse)
                            {
                                rightHand.SendHapticImpulse(0, 0.5f, 0.1f);
                            }
                        }
                    }
                }
            }

            if (laserPointer != null)
            {
                if (hasControllerRay || XRSettings.isDeviceActive)
                {
                    laserPointer.enabled = true;
                    if (!hitButton) laserPointer.startColor = new Color(0.2f, 0.8f, 1.0f, 0.7f);
                    laserPointer.SetPosition(0, ray.origin);
                    laserPointer.SetPosition(1, ray.origin + ray.direction * rayLength);
                }
                else
                {
                    laserPointer.enabled = false;
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

            // Direct level eye contact with other avatars across the conference table
            Vector3 lookTarget = new Vector3(0, transform.position.y, 0);
            Vector3 lookDir = (lookTarget - transform.position).normalized;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                currentYaw = targetRot.eulerAngles.y;
                currentPitch = 0f; // Level eye-line directly facing peer avatars across the table
                transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            }
        }
    }
}
