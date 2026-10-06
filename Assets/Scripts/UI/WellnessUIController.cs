using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using MentalHealthApp.Assessment;
using MentalHealthApp.Discussion;

namespace MentalHealthApp.UI
{
    public class WellnessUIController : MonoBehaviour
    {
        [Header("References")]
        public StudentWellnessTracker wellnessTracker;
        public GroupDiscussionManager discussionManager;

        [Header("UI Panels")]
        public GameObject preAssessmentPanel;
        public GameObject responsePromptPanel;
        public GameObject postAssessmentPanel;
        public GameObject resultsDashboardPanel;
        public GameObject subtitlePanel;
        public GameObject chatHistoryPanel;

        [Header("Pre-Assessment Controls")]
        public TMP_InputField studentIdInput;
        public TMP_InputField geminiApiKeyInput;
        public Slider preStressSlider;
        public TextMeshProUGUI preStressValText;
        public Slider preConfidenceSlider;
        public TextMeshProUGUI preConfidenceValText;

        [Header("Response Prompt Controls")]
        public TextMeshProUGUI promptTopicText;
        public Button option1Btn;
        public TextMeshProUGUI option1Text;
        public Button option2Btn;
        public TextMeshProUGUI option2Text;
        public Button option3Btn;
        public TextMeshProUGUI option3Text;
        public TMP_InputField customResponseInput;
        public Button sendCustomBtn;

        [Header("Subtitle Controls")]
        public Image subtitleBadgeImage;
        public TextMeshProUGUI subtitleSpeakerText;
        public TextMeshProUGUI subtitleBodyText;

        [Header("Chat History Log Controls")]
        public TextMeshProUGUI chatLogContentText;
        public Button toggleChatBtn;
        private string accumulatedChatLog = "";

        [Header("Post-Assessment Controls")]
        public Slider postMoodSlider;
        public TextMeshProUGUI postMoodValText;
        public Slider postConfidenceSlider;
        public TextMeshProUGUI postConfidenceValText;

        [Header("Results Dashboard Controls")]
        public TextMeshProUGUI overallScoreText;
        public TextMeshProUGUI focusScoreText;
        public TextMeshProUGUI commScoreText;
        public TextMeshProUGUI socialScoreText;
        public TextMeshProUGUI feedbackTipsText;
        public TextMeshProUGUI exportStatusText;

        private Action<string, int> onResponseSubmittedCallback;
        private Coroutine typewriterRoutine;

        public void BuildUICanvas()
        {
            EnsureEventSystemExists();

            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Clear any old panels to prevent duplicate orphaned windows
            ClearExistingPanels();

            // Create Modal Panels
            preAssessmentPanel = CreatePanel("PreAssessmentPanel", new Vector2(680, 640), new Vector2(0.5f, 0.5f), Vector2.zero);
            responsePromptPanel = CreatePanel("ResponsePromptPanel", new Vector2(850, 560), new Vector2(0.5f, 0.5f), Vector2.zero);
            postAssessmentPanel = CreatePanel("PostAssessmentPanel", new Vector2(650, 550), new Vector2(0.5f, 0.5f), Vector2.zero);
            resultsDashboardPanel = CreatePanel("ResultsDashboardPanel", new Vector2(850, 720), new Vector2(0.5f, 0.5f), Vector2.zero);

            SetupPreAssessmentUI();
            SetupResponsePromptUI();
            SetupPostAssessmentUI();
            SetupResultsDashboardUI();
            SetupSubtitleUI();
            SetupChatHistoryUI();
            SetupVRViewControlsUI();

            // Default State
            preAssessmentPanel.SetActive(true);
            responsePromptPanel.SetActive(false);
            postAssessmentPanel.SetActive(false);
            resultsDashboardPanel.SetActive(false);
            if (subtitlePanel != null) subtitlePanel.SetActive(false);
            if (chatHistoryPanel != null) chatHistoryPanel.SetActive(false);
        }

        private void ClearExistingPanels()
        {
            List<GameObject> existingChildren = new List<GameObject>();
            foreach (Transform child in transform)
            {
                existingChildren.Add(child.gameObject);
            }
            foreach (GameObject child in existingChildren)
            {
                DestroyImmediate(child);
            }
        }

        private void EnsureEventSystemExists()
        {
            GameObject es = GameObject.Find("EventSystem");
            if (es == null)
            {
                es = new GameObject("EventSystem");
            }

            EventSystem evtSys = es.GetComponent<EventSystem>();
            if (evtSys == null) evtSys = es.AddComponent<EventSystem>();

            System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                StandaloneInputModule oldMod = es.GetComponent<StandaloneInputModule>();
                if (oldMod != null) DestroyImmediate(oldMod);

                if (es.GetComponent(inputSystemModuleType) == null)
                {
                    es.AddComponent(inputSystemModuleType);
                }
            }
            else
            {
                if (es.GetComponent<StandaloneInputModule>() == null)
                {
                    es.AddComponent<StandaloneInputModule>();
                }
            }
        }

        private GameObject CreatePanel(string name, Vector2 size, Vector2 anchor, Vector2 pos)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
            bg.raycastTarget = true;

            return panel;
        }

        private void SetupSubtitleUI()
        {
            subtitlePanel = CreatePanel("SubtitlePanel", new Vector2(1240, 115), new Vector2(0.5f, 0f), new Vector2(0, 75));
            Image bg = subtitlePanel.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

            // Badge Indicator
            GameObject badgeObj = new GameObject("SpeakerBadge");
            badgeObj.transform.SetParent(subtitlePanel.transform, false);
            RectTransform bRect = badgeObj.AddComponent<RectTransform>();
            bRect.anchoredPosition = new Vector2(-585, 30);
            bRect.sizeDelta = new Vector2(24, 24);
            subtitleBadgeImage = badgeObj.AddComponent<Image>();
            subtitleBadgeImage.color = new Color(0.4f, 0.85f, 1.0f);

            GameObject spkObj = AddText(subtitlePanel, "SPEAKER NAME", 20, new Color(0.4f, 0.85f, 1.0f), new Vector2(1120, 30), new Vector2(15, 30));
            subtitleSpeakerText = spkObj.GetComponent<TextMeshProUGUI>();
            subtitleSpeakerText.fontStyle = FontStyles.Bold;
            subtitleSpeakerText.alignment = TextAlignmentOptions.Left;

            GameObject bodyObj = AddText(subtitlePanel, "\"Dialogue content text shows here...\"", 18, Color.white, new Vector2(1180, 55), new Vector2(0, -18));
            subtitleBodyText = bodyObj.GetComponent<TextMeshProUGUI>();
            subtitleBodyText.alignment = TextAlignmentOptions.Left;
        }

        public void ShowSubtitle(string speakerName, string text, Color badgeColor = default)
        {
            if (badgeColor == default) badgeColor = new Color(0.4f, 0.85f, 1.0f);

            if (subtitlePanel != null)
            {
                subtitlePanel.SetActive(true);
                if (subtitleBadgeImage != null) subtitleBadgeImage.color = badgeColor;
                if (subtitleSpeakerText != null) subtitleSpeakerText.text = speakerName.ToUpper();

                if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
                typewriterRoutine = StartCoroutine(TypewriterText(text));
            }

            AppendToChatLog(speakerName, text);
        }

        private IEnumerator TypewriterText(string text)
        {
            if (subtitleBodyText == null) yield break;

            subtitleBodyText.text = "\"\"";
            string current = "";
            for (int i = 0; i < text.Length; i++)
            {
                current += text[i];
                subtitleBodyText.text = "\"" + current + "\"";
                yield return new WaitForSeconds(0.018f);
            }
            subtitleBodyText.text = "\"" + text + "\"";
        }

        public void HideSubtitle()
        {
            if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
            if (subtitlePanel != null)
            {
                subtitlePanel.SetActive(false);
            }
        }

        private void SetupVRViewControlsUI()
        {
            // VR Mode Button on top left
            GameObject vrBtnObj = new GameObject("VRSeatPOVButton");
            vrBtnObj.transform.SetParent(transform, false);
            RectTransform vrRect = vrBtnObj.AddComponent<RectTransform>();
            vrRect.anchorMin = new Vector2(0, 1);
            vrRect.anchorMax = new Vector2(0, 1);
            vrRect.anchoredPosition = new Vector2(130, -50);
            vrRect.sizeDelta = new Vector2(210, 45);

            Image img = vrBtnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.45f, 0.68f, 0.92f);
            Button vrBtn = vrBtnObj.AddComponent<Button>();
            vrBtn.targetGraphic = img;

            GameObject tObj = AddText(vrBtnObj, "🥽 VR Seat 360°", 16, Color.white, new Vector2(200, 35), Vector3.zero);
            tObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            vrBtn.onClick.AddListener(() =>
            {
                if (discussionManager != null)
                {
                    discussionManager.SwitchToVRSeatPOV();
                }
            });
        }

        private void SetupChatHistoryUI()
        {
            // Toggle Chat Log Button on top right
            GameObject btnObj = new GameObject("ToggleChatButton");
            btnObj.transform.SetParent(transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 1);
            btnRect.anchorMax = new Vector2(1, 1);
            btnRect.anchoredPosition = new Vector2(-120, -50);
            btnRect.sizeDelta = new Vector2(180, 45);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.32f, 0.9f);
            toggleChatBtn = btnObj.AddComponent<Button>();
            toggleChatBtn.targetGraphic = img;

            GameObject tObj = AddText(btnObj, "💬 Discussion Log", 16, Color.white, new Vector2(170, 35), Vector3.zero);
            tObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            // Chat History Panel
            chatHistoryPanel = CreatePanel("ChatHistoryPanel", new Vector2(480, 600), new Vector2(1f, 0.5f), new Vector2(-260, 0));
            chatHistoryPanel.SetActive(false);

            AddHeader(chatHistoryPanel, "GROUP DISCUSSION LOG");

            GameObject textObj = AddText(chatHistoryPanel, "Discussion started...\n", 16, new Color(0.9f, 0.92f, 0.95f), new Vector2(440, 500), new Vector2(0, -25));
            chatLogContentText = textObj.GetComponent<TextMeshProUGUI>();
            chatLogContentText.alignment = TextAlignmentOptions.TopLeft;

            toggleChatBtn.onClick.AddListener(() =>
            {
                chatHistoryPanel.SetActive(!chatHistoryPanel.activeSelf);
            });
        }

        public void AppendToChatLog(string speaker, string message)
        {
            accumulatedChatLog += "<b>" + speaker + ":</b> " + message + "\n\n";
            if (chatLogContentText != null)
            {
                chatLogContentText.text = accumulatedChatLog;
            }
        }

        private void SetupPreAssessmentUI()
        {
            AddHeader(preAssessmentPanel, "STUDENT WELLNESS PRE-SESSION");

            // Student ID Field
            GameObject idObj = new GameObject("IDInput");
            idObj.transform.SetParent(preAssessmentPanel.transform, false);
            RectTransform idRect = idObj.AddComponent<RectTransform>();
            idRect.anchoredPosition = new Vector3(0, 180, 0);
            idRect.sizeDelta = new Vector2(520, 45);
            Image idBg = idObj.AddComponent<Image>();
            idBg.color = new Color(0.18f, 0.22f, 0.28f);
            studentIdInput = idObj.AddComponent<TMP_InputField>();

            GameObject placeholder = AddText(idObj, "Enter Student Name (Default: Kevin)", 16, Color.gray, new Vector2(500, 35));
            studentIdInput.placeholder = placeholder.GetComponent<TextMeshProUGUI>();

            GameObject textArea = AddText(idObj, "", 18, Color.white, new Vector2(500, 35));
            studentIdInput.textComponent = textArea.GetComponent<TextMeshProUGUI>();

            // Optional Gemini API Key Field
            GameObject keyObj = new GameObject("APIKeyInput");
            keyObj.transform.SetParent(preAssessmentPanel.transform, false);
            RectTransform keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.anchoredPosition = new Vector3(0, 125, 0);
            keyRect.sizeDelta = new Vector2(520, 45);
            Image keyBg = keyObj.AddComponent<Image>();
            keyBg.color = new Color(0.18f, 0.22f, 0.28f);
            geminiApiKeyInput = keyObj.AddComponent<TMP_InputField>();
            geminiApiKeyInput.contentType = TMP_InputField.ContentType.Password;

            GameObject keyPlaceholder = AddText(keyObj, "Gemini API Key (Optional for Live AI Peers)", 16, Color.gray, new Vector2(500, 35));
            geminiApiKeyInput.placeholder = keyPlaceholder.GetComponent<TextMeshProUGUI>();

            GameObject keyTextArea = AddText(keyObj, "", 18, Color.white, new Vector2(500, 35));
            geminiApiKeyInput.textComponent = keyTextArea.GetComponent<TextMeshProUGUI>();

            // Auto-load existing secure key if stored locally or in environment variable
            if (discussionManager != null && discussionManager.geminiAgent != null)
            {
                string loadedKey = discussionManager.geminiAgent.LoadSecureAPIKey();
                if (!string.IsNullOrEmpty(loadedKey))
                {
                    geminiApiKeyInput.text = loadedKey;
                }
            }

            // Pre-Stress Slider Label & Slider
            AddText(preAssessmentPanel, "Pre-Session Stress Level (1 = Low, 10 = High):", 18, Color.white, new Vector2(500, 30), new Vector2(0, 40));
            preStressSlider = AddSlider(preAssessmentPanel, new Vector2(0, 5), 1, 10, 3);
            preStressValText = AddText(preAssessmentPanel, "3 / 10", 18, new Color(0.4f, 0.85f, 1.0f), new Vector2(100, 30), new Vector2(280, 5)).GetComponent<TextMeshProUGUI>();
            preStressSlider.onValueChanged.AddListener((v) => preStressValText.text = (int)v + " / 10");

            // Pre-Confidence Slider Label & Slider
            AddText(preAssessmentPanel, "Communication Confidence (1 = Low, 10 = High):", 18, Color.white, new Vector2(500, 30), new Vector2(0, -65));
            preConfidenceSlider = AddSlider(preAssessmentPanel, new Vector2(0, -100), 1, 10, 8);
            preConfidenceValText = AddText(preAssessmentPanel, "8 / 10", 18, new Color(0.4f, 0.85f, 1.0f), new Vector2(100, 30), new Vector2(280, -100)).GetComponent<TextMeshProUGUI>();
            preConfidenceSlider.onValueChanged.AddListener((v) => preConfidenceValText.text = (int)v + " / 10");

            // Submit Button
            Button startBtn = AddButton(preAssessmentPanel, "START GROUP DISCUSSION", new Vector2(0, -210), new Vector2(400, 55));
            startBtn.onClick.AddListener(() =>
            {
                Debug.Log("Start Group Discussion Button Clicked!");

                if (discussionManager != null && discussionManager.geminiAgent != null)
                {
                    if (geminiApiKeyInput != null && !string.IsNullOrEmpty(geminiApiKeyInput.text))
                    {
                        discussionManager.geminiAgent.SaveSecureAPIKey(geminiApiKeyInput.text.Trim());
                    }
                    else
                    {
                        discussionManager.geminiAgent.LoadSecureAPIKey();
                    }
                }

                if (wellnessTracker != null)
                {
                    wellnessTracker.InitializeSession(studentIdInput != null && !string.IsNullOrEmpty(studentIdInput.text) ? studentIdInput.text : "Kevin", (int)preStressSlider.value, (int)preConfidenceSlider.value);
                }

                // Hide & disable all PreAssessmentPanel instances completely
                if (preAssessmentPanel != null) preAssessmentPanel.SetActive(false);
                var allPrePanels = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var p in allPrePanels)
                {
                    if (p.name == "PreAssessmentPanel") p.SetActive(false);
                }

                if (discussionManager != null)
                {
                    discussionManager.StartSession();
                }
            });
        }

        private void SetupResponsePromptUI()
        {
            AddHeader(responsePromptPanel, "YOUR TURN TO SHARE IN THE GD");
            promptTopicText = AddText(responsePromptPanel, "Topic Prompt...", 20, Color.white, new Vector2(780, 50), new Vector2(0, 205)).GetComponent<TextMeshProUGUI>();

            // Quick Option Buttons
            option1Btn = AddButton(responsePromptPanel, "Option A", new Vector2(0, 130), new Vector2(780, 48));
            option1Text = option1Btn.GetComponentInChildren<TextMeshProUGUI>();

            option2Btn = AddButton(responsePromptPanel, "Option B", new Vector2(0, 70), new Vector2(780, 48));
            option2Text = option2Btn.GetComponentInChildren<TextMeshProUGUI>();

            option3Btn = AddButton(responsePromptPanel, "Option C", new Vector2(0, 10), new Vector2(780, 48));
            option3Text = option3Btn.GetComponentInChildren<TextMeshProUGUI>();

            option1Btn.onClick.AddListener(() => OnOptionClicked(option1Text.text));
            option2Btn.onClick.AddListener(() => OnOptionClicked(option2Text.text));
            option3Btn.onClick.AddListener(() => OnOptionClicked(option3Text.text));

            // Custom Free-Form Response Field & Button ("How to communicate with other avatars in the GD")
            AddText(responsePromptPanel, "── OR TYPE YOUR OWN CUSTOM MESSAGE TO THE GROUP ──", 16, new Color(0.4f, 0.85f, 1.0f), new Vector2(780, 30), new Vector2(0, -45));

            GameObject customObj = new GameObject("CustomResponseInput");
            customObj.transform.SetParent(responsePromptPanel.transform, false);
            RectTransform cRect = customObj.AddComponent<RectTransform>();
            cRect.anchoredPosition = new Vector3(-90, -100, 0);
            cRect.sizeDelta = new Vector2(580, 50);
            Image cBg = customObj.AddComponent<Image>();
            cBg.color = new Color(0.18f, 0.22f, 0.28f);
            customResponseInput = customObj.AddComponent<TMP_InputField>();

            GameObject ph = AddText(customObj, "Type what you'd like to say to the group...", 16, Color.gray, new Vector2(560, 40));
            customResponseInput.placeholder = ph.GetComponent<TextMeshProUGUI>();

            GameObject ta = AddText(customObj, "", 18, Color.white, new Vector2(560, 40));
            customResponseInput.textComponent = ta.GetComponent<TextMeshProUGUI>();

            // Submit custom message when pressing Enter key
            customResponseInput.onSubmit.AddListener((str) =>
            {
                if (!string.IsNullOrEmpty(str.Trim()))
                {
                    customResponseInput.text = "";
                    OnOptionClicked(str.Trim());
                }
            });

            sendCustomBtn = AddButton(responsePromptPanel, "SEND MESSAGE", new Vector2(275, -100), new Vector2(180, 50));
            sendCustomBtn.onClick.AddListener(() =>
            {
                if (customResponseInput != null && !string.IsNullOrEmpty(customResponseInput.text.Trim()))
                {
                    string msg = customResponseInput.text.Trim();
                    customResponseInput.text = "";
                    OnOptionClicked(msg);
                }
            });
        }

        public void ShowResponsePrompt(string topic, Action<string, int> callback)
        {
            onResponseSubmittedCallback = callback;
            promptTopicText.text = "Topic: " + topic;

            if (topic.Contains("Workload"))
            {
                option1Text.text = "I prioritize breaking tasks into daily sub-goals to stay calm and organized.";
                option2Text.text = "I set strict study boundaries and block out time for rest and relaxation.";
                option3Text.text = "I reach out to peers or mentors early whenever assignment doubts arise.";
            }
            else if (topic.Contains("Collaboration"))
            {
                option1Text.text = "I value clear roles and encouraging all team members to share ideas freely.";
                option2Text.text = "I focus on active listening and building agreement on core project goals.";
                option3Text.text = "I establish open feedback channels to resolve differences constructively.";
            }
            else
            {
                option1Text.text = "I prioritize regular physical movement and maintaining steady sleep schedules.";
                option2Text.text = "I practice quick mindfulness check-ins between heavy study sessions.";
                option3Text.text = "I make sure to balance academic effort with social & family connection.";
            }

            responsePromptPanel.SetActive(true);
        }

        private void OnOptionClicked(string optionText)
        {
            responsePromptPanel.SetActive(false);
            if (onResponseSubmittedCallback != null)
            {
                onResponseSubmittedCallback.Invoke(optionText, 8);
            }
        }

        private void SetupPostAssessmentUI()
        {
            AddHeader(postAssessmentPanel, "POST-SESSION SELF-REFLECTION");

            AddText(postAssessmentPanel, "Post-Session Mood (1 = Low, 10 = Great):", 18, Color.white, new Vector2(500, 30), new Vector2(0, 80));
            postMoodSlider = AddSlider(postAssessmentPanel, new Vector2(0, 40), 1, 10, 8);
            postMoodValText = AddText(postAssessmentPanel, "8 / 10", 18, new Color(0.4f, 0.85f, 1.0f), new Vector2(100, 30), new Vector2(280, 40)).GetComponent<TextMeshProUGUI>();
            postMoodSlider.onValueChanged.AddListener((v) => postMoodValText.text = (int)v + " / 10");

            AddText(postAssessmentPanel, "Post-Session Confidence (1 = Low, 10 = High):", 18, Color.white, new Vector2(500, 30), new Vector2(0, -40));
            postConfidenceSlider = AddSlider(postAssessmentPanel, new Vector2(0, -80), 1, 10, 9);
            postConfidenceValText = AddText(postAssessmentPanel, "9 / 10", 18, new Color(0.4f, 0.85f, 1.0f), new Vector2(100, 30), new Vector2(280, -80)).GetComponent<TextMeshProUGUI>();
            postConfidenceSlider.onValueChanged.AddListener((v) => postConfidenceValText.text = (int)v + " / 10");

            Button viewResultsBtn = AddButton(postAssessmentPanel, "VIEW WELLNESS RESULTS DASHBOARD", new Vector2(0, -180), new Vector2(450, 55));
            viewResultsBtn.onClick.AddListener(() =>
            {
                if (wellnessTracker != null)
                {
                    wellnessTracker.FinalizeSession((int)postMoodSlider.value, (int)postConfidenceSlider.value);
                }
                postAssessmentPanel.SetActive(false);
                ShowResultsDashboard();
            });
        }

        public void ShowPostAssessmentModal()
        {
            postAssessmentPanel.SetActive(true);
        }

        private void SetupResultsDashboardUI()
        {
            AddHeader(resultsDashboardPanel, "SESSION WELLNESS RESULTS DASHBOARD");

            overallScoreText = AddText(resultsDashboardPanel, "OVERALL WELLNESS INDEX: --", 24, new Color(0.3f, 1.0f, 0.6f), new Vector2(750, 45), new Vector2(0, 240)).GetComponent<TextMeshProUGUI>();

            focusScoreText = AddText(resultsDashboardPanel, "Focus Index: --", 18, Color.white, new Vector2(360, 35), new Vector2(-200, 180)).GetComponent<TextMeshProUGUI>();
            commScoreText = AddText(resultsDashboardPanel, "Communication Ease: --", 18, Color.white, new Vector2(360, 35), new Vector2(200, 180)).GetComponent<TextMeshProUGUI>();
            socialScoreText = AddText(resultsDashboardPanel, "Social Comfort: --", 18, Color.white, new Vector2(720, 35), new Vector2(0, 135)).GetComponent<TextMeshProUGUI>();

            GameObject card = AddText(resultsDashboardPanel, "Supportive Feedback & Growth Tips:\n...", 16, new Color(0.9f, 0.95f, 1.0f), new Vector2(780, 210), new Vector2(0, 0));
            feedbackTipsText = card.GetComponent<TextMeshProUGUI>();
            feedbackTipsText.alignment = TextAlignmentOptions.TopLeft;

            Button exportBtn = AddButton(resultsDashboardPanel, "EXPORT ANONYMIZED CSV REPORT", new Vector2(0, -190), new Vector2(450, 50));
            exportStatusText = AddText(resultsDashboardPanel, "", 14, new Color(0.4f, 0.85f, 1.0f), new Vector2(750, 30), new Vector2(0, -245)).GetComponent<TextMeshProUGUI>();

            exportBtn.onClick.AddListener(() =>
            {
                if (wellnessTracker != null)
                {
                    string path = wellnessTracker.ExportCSV();
                    exportStatusText.text = "CSV Exported successfully to:\n" + path;
                }
            });
        }

        private void ShowResultsDashboard()
        {
            if (wellnessTracker == null) return;
            var data = wellnessTracker.currentData;

            overallScoreText.text = string.Format("SESSION WELLNESS INDEX: {0:F0} / 100", data.overallWellnessScore);
            focusScoreText.text = string.Format("Focus Index: {0:F0}%", data.focusScore);
            commScoreText.text = string.Format("Communication Ease: {0:F0}%", data.communicationEaseScore);
            socialScoreText.text = string.Format("Social Comfort: {0:F0}%", data.socialEngagementScore);

            string tipsCombined = "REAL-TIME BEHAVIORAL TRACKING SUMMARY:\n" +
                string.Format("• Tracking Mode: {0}\n", data.gazeTrackingLabel) +
                string.Format("• Speaker Gaze Attention: {0:F1}s | Looking Away: {1:F1}s | Gaze Switches: {2}\n", data.gazeSpeakerAttentionTime, data.gazeAwayTime, data.gazeSwitchCount) +
                string.Format("• Head Motion Level: {0} (Total Movement: {1:F2}m)\n", data.headMovementLevel, data.totalHeadDistanceTraveled) +
                string.Format("• Facial Expression Tracking: {0}\n\n", data.facialTrackingStatus) +
                "SUPPORTIVE FEEDBACK & WELLNESS TIPS:\n";

            foreach (string tip in data.supportiveTips)
            {
                tipsCombined += "• " + tip + "\n";
            }

            tipsCombined += "\n<size=12><color=#80A0C0>" +
                "DISCLAIMER 1: Behavioral measurements are based on available VR tracking data during this session. Gaze represents headset/head orientation rather than true eye movement unless eye-tracking hardware is available. Facial-expression measurements are only provided when supported by the hardware.\n" +
                "DISCLAIMER 2: These indicators describe observable session behavior and self-reported experience. They are not a medical or psychological diagnosis." +
                "</color></size>";

            feedbackTipsText.text = tipsCombined;

            resultsDashboardPanel.SetActive(true);
        }

        private GameObject AddHeader(GameObject parent, string title)
        {
            GameObject headObj = new GameObject("Header");
            headObj.transform.SetParent(parent.transform, false);
            RectTransform rect = headObj.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector3(0, (parent.GetComponent<RectTransform>().sizeDelta.y / 2f) - 40f, 0);
            rect.sizeDelta = new Vector2(parent.GetComponent<RectTransform>().sizeDelta.x - 40f, 50f);

            TextMeshProUGUI text = headObj.AddComponent<TextMeshProUGUI>();
            text.text = title;
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.4f, 0.85f, 1.0f);
            text.raycastTarget = false;
            return headObj;
        }

        private GameObject AddText(GameObject parent, string str, float fontSize, Color color, Vector2 size, Vector3 pos = default, bool raycastTarget = false)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.text = str;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = raycastTarget;
            return obj;
        }

        private Slider AddSlider(GameObject parent, Vector3 pos, float min, float max, float startVal)
        {
            GameObject sObj = new GameObject("Slider");
            sObj.transform.SetParent(parent.transform, false);
            RectTransform rect = sObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(400, 30);

            Image bg = sObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.3f);
            bg.raycastTarget = true;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sObj.transform, false);
            RectTransform fAreaRect = fillArea.AddComponent<RectTransform>();
            fAreaRect.anchorMin = Vector2.zero;
            fAreaRect.anchorMax = Vector2.one;
            fAreaRect.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.65f, 0.95f);
            fillImg.raycastTarget = false;

            Slider slider = sObj.AddComponent<Slider>();
            slider.targetGraphic = bg;
            slider.fillRect = fillRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = startVal;

            return slider;
        }

        private Button AddButton(GameObject parent, string labelText, Vector3 pos, Vector2 size)
        {
            GameObject btnObj = new GameObject("Button");
            btnObj.transform.SetParent(parent.transform, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.45f, 0.75f);
            img.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject tObj = AddText(btnObj, labelText, 18, Color.white, size, Vector3.zero, false);
            tObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            return btn;
        }
    }
}
