using UnityEngine;
using System.Collections.Generic;

namespace MentalHealthApp.Tracking
{
    public class ParticipantAttentionTracker : MonoBehaviour
    {
        [Header("Speaker & Attention Tracking")]
        public int currentSpeakerIndex = -1;
        public float timeOrientedTowardSpeaker = 0f;
        public float timeOrientedAwayFromSpeaker = 0f;
        public int gazeSwitchCount = 0;
        public int uniqueParticipantsLookedAt = 0;

        private int lastObservedParticipantIndex = -1;
        private HashSet<int> participantsLookedAtSet = new HashSet<int>();

        public void UpdateAttention(int currentSpeaker, int currentlyLookedAtParticipant, float deltaTime)
        {
            currentSpeakerIndex = currentSpeaker;

            // Track gaze switches
            if (currentlyLookedAtParticipant != lastObservedParticipantIndex && currentlyLookedAtParticipant != -1)
            {
                gazeSwitchCount++;
                participantsLookedAtSet.Add(currentlyLookedAtParticipant);
            }
            lastObservedParticipantIndex = currentlyLookedAtParticipant;
            uniqueParticipantsLookedAt = participantsLookedAtSet.Count;

            // Track speaker attention
            if (currentSpeakerIndex != -1)
            {
                if (currentlyLookedAtParticipant == currentSpeakerIndex)
                {
                    timeOrientedTowardSpeaker += deltaTime;
                }
                else
                {
                    timeOrientedAwayFromSpeaker += deltaTime;
                }
            }
        }

        public void ResetMetrics()
        {
            currentSpeakerIndex = -1;
            timeOrientedTowardSpeaker = 0f;
            timeOrientedAwayFromSpeaker = 0f;
            gazeSwitchCount = 0;
            uniqueParticipantsLookedAt = 0;
            lastObservedParticipantIndex = -1;
            participantsLookedAtSet.Clear();
        }
    }
}
