using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MentalHealthApp.Generators;
using MentalHealthApp.Assessment;
using MentalHealthApp.UI;
using MentalHealthApp.Core;

namespace MentalHealthApp.Discussion
{
    public enum DiscussionPhase
    {
        PreSession,
        Introduction,
        TopicDiscussion,
        AssessedStudentTurn,
        PeerAIResponseTurn,
        PostSession,
        Completed
    }

    public enum CameraVRMode
    {
        FirstPersonPOV,
        SpeakerFocus,
        Overview
    }

    public class GroupDiscussionManager : MonoBehaviour
    {
        [Header("State")]
        public DiscussionPhase currentPhase = DiscussionPhase.PreSession;
        public CameraVRMode currentVRMode = CameraVRMode.FirstPersonPOV;
        public int currentTopicIndex = 0;
        public int currentSpeakerIndex = 0;
        public float phaseTimer = 0f;

        [Header("References")]
        public ModernMeetingRoomGenerator roomGenerator;
        public AvatarGenerator avatarGenerator;
        public StudentWellnessTracker wellnessTracker;
        public WellnessUIController uiController;
        public GeminiDiscussionAgent geminiAgent;
        public VRHeadLookController vrHeadLook;

        private Coroutine discussionRoutine;
        private Coroutine cameraFocusRoutine;

        private Vector3 defaultCamPos = new Vector3(0, 1.48f, -2.85f);
        private Quaternion defaultCamRot = Quaternion.Euler(11f, 0, 0);

        private List<string> recentDialogueHistory = new List<string>();

        private string[] topics = new string[]
        {
            "Managing Academic Workload & Exam Stress",
            "Effective Strategies for Team Collaboration & Communication",
            "Maintaining Personal Well-being, Rest & Study-Life Balance",
            "Navigating Peer Pressure & Mental Health Resilience",
            "Overcoming Academic Burnout & Building Positive Habits"
        };

        private string[] avatarPersonas = new string[]
        {
            "Supportive & empathetic group discussion moderator and leader",                                // Seat 0 - Maya
            "Kevin - College student sharing genuine personal reflections and insights",                   // Seat 1 - Kevin
            "Attentive student sharing practical time-management and workload breakdown techniques",        // Seat 2 - Karan
            "Analytical student focusing on active listening, clear team roles, and structured collaboration",// Seat 3 - Arjun
            "Compassionate psychology student emphasizing personal boundaries, rest, and mental wellness"   // Seat 4 - Priya
        };

        private Color[] badgeColors = new Color[]
        {
            new Color(0.65f, 0.4f, 0.9f),  // Maya - Purple
            new Color(0.4f, 0.85f, 1.0f),  // Alex - Cyan
            new Color(0.3f, 0.75f, 0.45f), // Karan - Green
            new Color(0.3f, 0.55f, 0.9f),  // Arjun - Blue
            new Color(0.85f, 0.35f, 0.4f)  // Priya - Burgundy
        };

        [Header("Tracking & Memory References")]
        public MentalHealthApp.Tracking.RealtimeVRTrackingManager trackingManager;
        public GDQuestionHistory questionHistory;

        public void StartSession()
        {
            if (geminiAgent == null)
            {
                geminiAgent = GetComponent<GeminiDiscussionAgent>();
                if (geminiAgent == null) geminiAgent = gameObject.AddComponent<GeminiDiscussionAgent>();
            }

            if (trackingManager == null)
            {
                trackingManager = GetComponent<MentalHealthApp.Tracking.RealtimeVRTrackingManager>();
                if (trackingManager == null) trackingManager = gameObject.AddComponent<MentalHealthApp.Tracking.RealtimeVRTrackingManager>();
            }

            if (questionHistory == null)
            {
                questionHistory = GetComponent<GDQuestionHistory>();
                if (questionHistory == null) questionHistory = gameObject.AddComponent<GDQuestionHistory>();
            }

            questionHistory.ClearHistory();

            if (string.IsNullOrEmpty(geminiAgent.apiKey))
            {
                geminiAgent.apiKey = string.Empty;
            }

            EnsureVRController();

            recentDialogueHistory.Clear();
            currentPhase = DiscussionPhase.Introduction;

            // 1. Lock camera strictly into VR First-Person POV at Seat 1 (Kevin)
            SwitchToVRSeatPOV();

            // 2. Start Real-time Camera & Gaze Tracking
            if (trackingManager != null)
            {
                trackingManager.StartTracking(Camera.main, avatarGenerator);
            }

            if (discussionRoutine != null) StopCoroutine(discussionRoutine);
            discussionRoutine = StartCoroutine(RunDiscussionLoop());
        }

        private void EnsureVRController()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                vrHeadLook = mainCam.GetComponent<VRHeadLookController>();
                if (vrHeadLook == null) vrHeadLook = mainCam.gameObject.AddComponent<VRHeadLookController>();
            }
        }

        public void SwitchToVRSeatPOV()
        {
            EnsureVRController();
            currentVRMode = CameraVRMode.FirstPersonPOV;

            // Seat 1 - Assessed Student Kevin's seat position
            Vector3 seatPos = new Vector3(-0.95f, 0, -1.35f);
            if (avatarGenerator != null && avatarGenerator.generatedAvatars.Count > 1 && avatarGenerator.generatedAvatars[1].avatarRoot != null)
            {
                seatPos = avatarGenerator.generatedAvatars[1].avatarRoot.transform.position;
            }

            if (vrHeadLook != null)
            {
                vrHeadLook.SetSeatPOVPosition(seatPos, Quaternion.identity);
            }
        }

        private IEnumerator RunDiscussionLoop()
        {
            // Introduction Phase
            string introText = "Welcome everyone to today's student mental health and well-being discussion! We'll explore academic balance, teamwork, and personal wellness together.";
            UpdateScreen("WELCOME TO MENTAL HEALTH DISCUSSION", "Phase 1: Session Overview\n\nToday we are exploring strategies for academic success, collaborative communication, and student well-being.", "STATUS: INTRODUCING SESSION");

            if (avatarGenerator != null && avatarGenerator.generatedAvatars.Count > 0)
            {
                StudentAvatarData facilitator = avatarGenerator.generatedAvatars[0]; // Maya
                currentSpeakerIndex = 0;
                if (trackingManager != null) trackingManager.activeSpeakerIndex = 0;

                OrientHeadsTowards(facilitator.headTransform.position);
                SetAvatarExpression(facilitator, AvatarExpression.Speaking);
                SetGroupExpressionExcept(facilitator, AvatarExpression.Empathetic);

                ShowSpeechBubble(facilitator, introText);
                if (uiController != null) uiController.ShowSubtitle(facilitator.studentName + " (Leader)", introText, badgeColors[0]);
                recentDialogueHistory.Add(facilitator.studentName + ": " + introText);
                if (questionHistory != null) questionHistory.AddStatement(introText);
            }
            yield return new WaitForSeconds(5f);
            if (uiController != null) uiController.HideSubtitle();
            if (avatarGenerator != null && avatarGenerator.generatedAvatars.Count > 0)
            {
                SetAvatarExpression(avatarGenerator.generatedAvatars[0], AvatarExpression.Neutral);
            }

            // Loop through Topics
            for (int t = 0; t < topics.Length; t++)
            {
                currentTopicIndex = t;
                currentPhase = DiscussionPhase.TopicDiscussion;

                string topicHeader = "TOPIC " + (t + 1) + ": " + topics[t];
                UpdateScreen(topicHeader, "Please listen to your peers and reflect on your experience.", "STATUS: ACTIVE DISCUSSION");

                // Dynamic AI Turns for Peers (Seats 2, 3, 4)
                for (int p = 2; p < avatarGenerator.generatedAvatars.Count && p <= 4; p++)
                {
                    currentSpeakerIndex = p;
                    if (trackingManager != null) trackingManager.activeSpeakerIndex = p;

                    StudentAvatarData peerAvatar = avatarGenerator.generatedAvatars[p];
                    Color badgeCol = (p < badgeColors.Length) ? badgeColors[p] : Color.cyan;

                    OrientHeadsTowards(peerAvatar.headTransform.position);
                    SetAvatarExpression(peerAvatar, AvatarExpression.Speaking);
                    SetGroupExpressionExcept(peerAvatar, AvatarExpression.Thinking);

                    bool aiGenerated = false;
                    string dialogueText = "";

                    if (p == 4) // Seat 4 asks a question to prompt group & student
                    {
                        yield return geminiAgent.GeneratePeerQuestion(
                            peerAvatar.studentName,
                            avatarPersonas[p],
                            topics[t],
                            recentDialogueHistory,
                            (res) =>
                            {
                                dialogueText = res;
                                aiGenerated = true;
                            }
                        );
                        if (questionHistory != null) questionHistory.AddQuestion(dialogueText, p, topics[t], QuestionIntent.Clarification, 2);
                    }
                    else // Seats 2 & 3 share peer AI statements
                    {
                        yield return geminiAgent.GeneratePeerStatement(
                            peerAvatar.studentName,
                            avatarPersonas[p],
                            topics[t],
                            recentDialogueHistory,
                            (res) =>
                            {
                                dialogueText = res;
                                aiGenerated = true;
                            }
                        );
                        if (questionHistory != null) questionHistory.AddStatement(dialogueText);
                    }

                    if (!aiGenerated || string.IsNullOrEmpty(dialogueText))
                    {
                        dialogueText = "Sharing open perspectives and staying supportive makes a huge difference in managing college stress.";
                    }

                    ShowSpeechBubble(peerAvatar, dialogueText);
                    if (uiController != null) uiController.ShowSubtitle(peerAvatar.studentName, dialogueText, badgeCol);
                    recentDialogueHistory.Add(peerAvatar.studentName + ": " + dialogueText);

                    wellnessTracker.RecordPeerTurn();
                    yield return new WaitForSeconds(5.5f);
                    HideSpeechBubble(peerAvatar);
                    if (uiController != null) uiController.HideSubtitle();
                    SetAvatarExpression(peerAvatar, AvatarExpression.Empathetic);
                }

                // Assessed Student's Turn (Seat 1 - Kevin / Player)
                currentPhase = DiscussionPhase.AssessedStudentTurn;
                currentSpeakerIndex = 1;
                if (trackingManager != null)
                {
                    trackingManager.activeSpeakerIndex = 1;
                    trackingManager.isUserSpeaking = true;
                    trackingManager.userResponseLatency = Time.time;
                }

                StudentAvatarData assessedAvatar = avatarGenerator.generatedAvatars[1];

                // Real-time Reaction: All avatar participants turn their heads to face Kevin (Seat 1)!
                OrientHeadsTowards(assessedAvatar.headTransform.position);
                SetGroupExpressionExcept(assessedAvatar, AvatarExpression.Empathetic);

                UpdateScreen(topicHeader, "YOUR TURN: Select or type your response to participate in the discussion.", "STATUS: AWAITING YOUR INPUT");

                bool responseGiven = false;
                string studentResponseText = "";

                uiController.ShowResponsePrompt(topics[t], (selectedOptionText, confidenceRating) =>
                {
                    studentResponseText = selectedOptionText;
                    wellnessTracker.RecordAssessedStudentResponse(selectedOptionText, confidenceRating);

                    if (trackingManager != null)
                    {
                        trackingManager.userResponseLatency = Time.time - trackingManager.userResponseLatency;
                    }

                    SetAvatarExpression(assessedAvatar, AvatarExpression.Speaking);
                    ShowSpeechBubble(assessedAvatar, selectedOptionText);
                    if (uiController != null) uiController.ShowSubtitle(assessedAvatar.studentName + " (You)", selectedOptionText, badgeColors[1]);
                    recentDialogueHistory.Add("Kevin: " + selectedOptionText);
                    if (questionHistory != null) questionHistory.AddStatement(selectedOptionText);
                    responseGiven = true;
                });

                // Wait for student to submit input (via option button or custom typed field)
                while (!responseGiven)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(4f);
                HideSpeechBubble(assessedAvatar);
                if (uiController != null) uiController.HideSubtitle();
                SetAvatarExpression(assessedAvatar, AvatarExpression.Neutral);

                if (trackingManager != null) trackingManager.isUserSpeaking = false;

                // Realistic Interruption + AI Peer Response Turn (Leader Maya - Seat 0)
                currentPhase = DiscussionPhase.PeerAIResponseTurn;
                currentSpeakerIndex = 0;
                if (trackingManager != null) trackingManager.activeSpeakerIndex = 0;

                StudentAvatarData leaderAvatar = avatarGenerator.generatedAvatars[0]; // Maya

                OrientHeadsTowards(leaderAvatar.headTransform.position);
                SetAvatarExpression(leaderAvatar, AvatarExpression.Speaking);
                SetGroupExpressionExcept(leaderAvatar, AvatarExpression.Empathetic);

                UpdateScreen(topicHeader, "Maya is reflecting on your thoughts...", "STATUS: AI PEER GENERATING RESPONSE");

                bool aiResponded = false;
                string aiText = "";

                yield return geminiAgent.GeneratePeerResponse(
                    "Maya (Discussion Leader)",
                    avatarPersonas[0],
                    topics[t],
                    studentResponseText,
                    (resText) =>
                    {
                        aiText = resText;
                        aiResponded = true;
                    }
                );

                if (!aiResponded || string.IsNullOrEmpty(aiText))
                {
                    aiText = "That's a really valuable perspective! Building those healthy habits early makes a huge difference in managing university stress.";
                }

                ShowSpeechBubble(leaderAvatar, aiText);
                if (uiController != null) uiController.ShowSubtitle(leaderAvatar.studentName + " (Leader)", aiText, badgeColors[0]);
                recentDialogueHistory.Add(leaderAvatar.studentName + ": " + aiText);
                if (questionHistory != null) questionHistory.AddStatement(aiText);

                UpdateScreen(topicHeader, "Maya: \"" + aiText + "\"", "STATUS: AI PEER RESPONDED");
                yield return new WaitForSeconds(6f);
                HideSpeechBubble(leaderAvatar);
                if (uiController != null) uiController.HideSubtitle();
                SetAvatarExpression(leaderAvatar, AvatarExpression.Empathetic);
            }

            // Post-Discussion Phase
            currentPhase = DiscussionPhase.PostSession;
            if (trackingManager != null)
            {
                trackingManager.StopTracking();
                if (wellnessTracker != null && trackingManager.attentionTracker != null && trackingManager.headTracker != null && trackingManager.handTracker != null)
                {
                    wellnessTracker.RecordTrackingSnapshot(
                        trackingManager.attentionTracker.timeOrientedTowardSpeaker,
                        trackingManager.attentionTracker.timeOrientedAwayFromSpeaker,
                        trackingManager.attentionTracker.gazeSwitchCount,
                        trackingManager.headTracker.totalHeadDistanceTraveled,
                        trackingManager.headTracker.headMovementLevel,
                        trackingManager.handTracker.handTrackingAvailable,
                        trackingManager.handTracker.facialTrackingAvailable
                    );
                }
            }

            UpdateScreen("DISCUSSION COMPLETED", "Thank you for participating!\n\nPlease complete the brief post-session reflection.", "STATUS: WRAPPING UP");

            yield return new WaitForSeconds(3f);
            uiController.ShowPostAssessmentModal();
        }

        private void SetAvatarExpression(StudentAvatarData avatar, AvatarExpression expr)
        {
            if (avatar != null && avatar.avatarRoot != null)
            {
                var exprCtrl = avatar.avatarRoot.GetComponent<AvatarExpressionController>();
                if (exprCtrl != null)
                {
                    exprCtrl.SetExpression(expr);
                }
            }
        }

        private void SetGroupExpressionExcept(StudentAvatarData activeSpeaker, AvatarExpression expr)
        {
            if (avatarGenerator == null || avatarGenerator.generatedAvatars == null) return;
            foreach (var a in avatarGenerator.generatedAvatars)
            {
                if (a != activeSpeaker && a.avatarRoot != null)
                {
                    var exprCtrl = a.avatarRoot.GetComponent<AvatarExpressionController>();
                    if (exprCtrl != null)
                    {
                        exprCtrl.SetExpression(expr);
                    }
                }
            }
        }

        private void FocusCameraOnAvatar(StudentAvatarData avatar)
        {
            if (Camera.main == null || avatar == null || avatar.headTransform == null) return;
            currentVRMode = CameraVRMode.SpeakerFocus;

            Vector3 avatarHeadPos = avatar.headTransform.position + new Vector3(0, 0.05f, 0);
            Vector3 avatarForward = avatar.avatarRoot != null ? avatar.avatarRoot.transform.forward : avatar.headTransform.forward;
            
            // Position camera directly in front of the avatar's face (1.15m in front of their head vector)
            Vector3 targetCamPos = avatarHeadPos + avatarForward * 1.15f + new Vector3(0, 0.05f, 0);
            Quaternion targetCamRot = Quaternion.LookRotation((avatarHeadPos - targetCamPos).normalized);

            if (cameraFocusRoutine != null) StopCoroutine(cameraFocusRoutine);
            cameraFocusRoutine = StartCoroutine(SmoothMoveCamera(targetCamPos, targetCamRot, 0.9f));
        }

        private void ResetCameraToOverview()
        {
            if (Camera.main == null) return;
            currentVRMode = CameraVRMode.Overview;
            if (cameraFocusRoutine != null) StopCoroutine(cameraFocusRoutine);
            cameraFocusRoutine = StartCoroutine(SmoothMoveCamera(defaultCamPos, defaultCamRot, 0.9f));
        }

        private IEnumerator SmoothMoveCamera(Vector3 targetPos, Quaternion targetRot, float duration)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) yield break;

            Vector3 startPos = mainCam.transform.position;
            Quaternion startRot = mainCam.transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                mainCam.transform.position = Vector3.Lerp(startPos, targetPos, t);
                mainCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            mainCam.transform.position = targetPos;
            mainCam.transform.rotation = targetRot;
        }

        private void OrientHeadsTowards(Vector3 targetPos)
        {
            if (avatarGenerator == null || avatarGenerator.generatedAvatars == null) return;

            foreach (var a in avatarGenerator.generatedAvatars)
            {
                if (a.headTransform != null)
                {
                    Vector3 dir = (targetPos - a.headTransform.position).normalized;
                    if (dir != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                        a.headTransform.rotation = Quaternion.Slerp(a.headTransform.rotation, targetRot, 0.4f);
                    }
                }
            }
        }

        private void ShowSpeechBubble(StudentAvatarData avatar, string text)
        {
            if (avatar != null && avatar.speechBubbleObj != null)
            {
                avatar.speechBubbleObj.SetActive(false);
                if (avatar.speechBubbleText != null)
                {
                    avatar.speechBubbleText.text = "\"" + text + "\"";
                }
            }
        }

        private void HideSpeechBubble(StudentAvatarData avatar)
        {
            if (avatar != null && avatar.speechBubbleObj != null)
            {
                avatar.speechBubbleObj.SetActive(false);
            }
        }

        private void UpdateScreen(string title, string body, string status)
        {
            if (roomGenerator != null)
            {
                if (roomGenerator.presentationTitleText != null) roomGenerator.presentationTitleText.text = title;
                if (roomGenerator.presentationBodyText != null) roomGenerator.presentationBodyText.text = body;
                if (roomGenerator.presentationStatusText != null) roomGenerator.presentationStatusText.text = status;
            }
        }
    }
}
