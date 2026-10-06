using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MentalHealthApp.Generators;

namespace MentalHealthApp.Tracking
{
    public class RealtimeVRTrackingManager : MonoBehaviour
    {
        [Header("Configuration")]
        public float samplingInterval = 0.1f; // 10 Hz real-time sampling

        [Header("Child Component References")]
        public VRGazeEstimator gazeEstimator;
        public VRHeadMovementTracker headTracker;
        public VRHandMovementTracker handTracker;
        public ParticipantAttentionTracker attentionTracker;
        public RealtimeBehaviorTracker behaviorTracker;

        [Header("Scene References")]
        public Camera vrCamera;
        public AvatarGenerator avatarGenerator;
        public int activeSpeakerIndex = -1;
        public bool isUserSpeaking = false;
        public float userResponseLatency = 0f;

        private Coroutine trackingRoutine;
        private List<Transform> participantTransforms = new List<Transform>();

        private void Awake()
        {
            EnsureComponents();
        }

        public void EnsureComponents()
        {
            if (gazeEstimator == null) gazeEstimator = GetOrAddComponent<VRGazeEstimator>();
            if (headTracker == null) headTracker = GetOrAddComponent<VRHeadMovementTracker>();
            if (handTracker == null) handTracker = GetOrAddComponent<VRHandMovementTracker>();
            if (attentionTracker == null) attentionTracker = GetOrAddComponent<ParticipantAttentionTracker>();
            if (behaviorTracker == null) behaviorTracker = GetOrAddComponent<RealtimeBehaviorTracker>();
        }

        public void StartTracking(Camera mainCam, AvatarGenerator avatars)
        {
            EnsureComponents();
            vrCamera = mainCam;
            avatarGenerator = avatars;

            if (vrCamera != null)
            {
                gazeEstimator.Initialize(vrCamera);
                headTracker.Initialize(vrCamera.transform);
            }
            handTracker.Initialize();

            CacheParticipantTransforms();

            if (trackingRoutine != null) StopCoroutine(trackingRoutine);
            trackingRoutine = StartCoroutine(TrackingLoop());
        }

        public void StopTracking()
        {
            if (trackingRoutine != null)
            {
                StopCoroutine(trackingRoutine);
                trackingRoutine = null;
            }
        }

        public void CacheParticipantTransforms()
        {
            participantTransforms.Clear();
            if (avatarGenerator != null && avatarGenerator.generatedAvatars != null)
            {
                foreach (var a in avatarGenerator.generatedAvatars)
                {
                    if (a.avatarRoot != null)
                    {
                        participantTransforms.Add(a.avatarRoot.transform);
                    }
                    else
                    {
                        participantTransforms.Add(null);
                    }
                }
            }
        }

        private IEnumerator TrackingLoop()
        {
            while (true)
            {
                if (vrCamera != null)
                {
                    // 1. Sample Head Movement
                    headTracker.Sample(vrCamera.transform, samplingInterval);

                    // 2. Sample Hand Movement & Hardware Flags
                    handTracker.Sample(samplingInterval);

                    // 3. Determine Gaze Orientation Target
                    int lookedAtParticipant = gazeEstimator.DetermineTargetParticipant(participantTransforms, out float smallestAngle);

                    // 4. Update Speaker Attention Metrics
                    attentionTracker.UpdateAttention(activeSpeakerIndex, lookedAtParticipant, samplingInterval);

                    // 5. Update Snapshot for Assessment Pipeline
                    bool isFacingSpeaker = (lookedAtParticipant == activeSpeakerIndex);
                    string speakerName = (activeSpeakerIndex >= 0 && avatarGenerator != null && activeSpeakerIndex < avatarGenerator.generatedAvatars.Count)
                        ? avatarGenerator.generatedAvatars[activeSpeakerIndex].studentName
                        : "None";

                    behaviorTracker.UpdateSnapshot(
                        activeSpeakerIndex,
                        speakerName,
                        isFacingSpeaker,
                        attentionTracker.timeOrientedTowardSpeaker,
                        headTracker.headMovementLevel,
                        headTracker.currentHeadSpeed,
                        isUserSpeaking,
                        userResponseLatency,
                        attentionTracker.gazeSwitchCount,
                        handTracker.handTrackingAvailable,
                        handTracker.facialTrackingAvailable
                    );
                }

                yield return new WaitForSeconds(samplingInterval);
            }
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            T comp = GetComponent<T>();
            if (comp == null) comp = gameObject.AddComponent<T>();
            return comp;
        }
    }
}
