using UnityEngine;
using System;

namespace MentalHealthApp.Tracking
{
    [Serializable]
    public class RealtimeBehaviorSnapshot
    {
        public float timestamp;
        public int currentSpeakerIndex;
        public string currentSpeakerName;
        public bool userFacingSpeaker;
        public float gazeDurationOnSpeaker;
        public string headMovementLevel;
        public float headSpeed;
        public bool userSpeaking;
        public float responseLatency;
        public int gazeSwitchCount;
        public bool handTrackingAvailable;
        public bool facialTrackingAvailable;
    }

    public class RealtimeBehaviorTracker : MonoBehaviour
    {
        public RealtimeBehaviorSnapshot currentSnapshot = new RealtimeBehaviorSnapshot();

        public void UpdateSnapshot(
            int currentSpeaker,
            string speakerName,
            bool facingSpeaker,
            float gazeDuration,
            string movementLevel,
            float speed,
            bool isUserSpeaking,
            float latency,
            int switches,
            bool handAvailable,
            bool facialAvailable)
        {
            currentSnapshot.timestamp = Time.time;
            currentSnapshot.currentSpeakerIndex = currentSpeaker;
            currentSnapshot.currentSpeakerName = speakerName;
            currentSnapshot.userFacingSpeaker = facingSpeaker;
            currentSnapshot.gazeDurationOnSpeaker = gazeDuration;
            currentSnapshot.headMovementLevel = movementLevel;
            currentSnapshot.headSpeed = speed;
            currentSnapshot.userSpeaking = isUserSpeaking;
            currentSnapshot.responseLatency = latency;
            currentSnapshot.gazeSwitchCount = switches;
            currentSnapshot.handTrackingAvailable = handAvailable;
            currentSnapshot.facialTrackingAvailable = facialAvailable;
        }

        public string ExportTelemetryJson()
        {
            return JsonUtility.ToJson(currentSnapshot);
        }
    }
}
