using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MentalHealthApp.Discussion
{
    // Production Security Note: Client-side API keys in compiled builds are not fully secure.
    // TODO: For production deployment, route all requests through a secure backend proxy server.

    [Serializable]
    public class GeminiModelInfo
    {
        public string name;
        public string displayName;
        public string description;
        public string[] supportedGenerationMethods;
    }

    [Serializable]
    public class GeminiModelListResponse
    {
        public GeminiModelInfo[] models;
    }

    [Serializable]
    public class GeminiResponseData
    {
        public Candidate[] candidates;
    }

    [Serializable]
    public class Candidate
    {
        public Content content;
        public string finishReason;
    }

    [Serializable]
    public class Content
    {
        public Part[] parts;
        public string role;
    }

    [Serializable]
    public class Part
    {
        public string text;
    }

    [Serializable]
    public class OpenAIResponseData
    {
        public OpenAIChoice[] choices;
    }

    [Serializable]
    public class OpenAIChoice
    {
        public OpenAIMessage message;
    }

    [Serializable]
    public class OpenAIMessage
    {
        public string content;
    }

    [Serializable]
    public class StructuredGDResponse
    {
        public string response;
        public string intent;
        public bool isQuestion;
        public string question;
        public bool shouldInterrupt;
        public bool requiresUserResponse;
    }

    public class GeminiDiscussionAgent : MonoBehaviour
    {
        [Header("API Settings")]
        public string apiKey = string.Empty;
        public string modelName = "gemini-3.5-flash";

        [Header("Gemini Configuration")]
        public float apiTimeout = 25f;
        public int maxRetries = 2;
        public bool enableModelDiscovery = true;
        public bool enableConversationHistory = true;
        public bool enableDuplicateQuestionDetection = true;

        [Header("Question Memory Reference")]
        public GDQuestionHistory questionHistory;

        private HashSet<string> askedQuestionsHistory = new HashSet<string>();
        private List<string> fullConversationHistory = new List<string>();
        private int fallbackIndex = 0;
        private string discoveredModelName = null;
        private bool isDiscoveringModel = false;

        private string[] nonRepeatingPeerPool = new string[]
        {
            "How do you usually recognize when study stress is starting to affect your daily routine?",
            "What strategies have you found most helpful for staying focused during heavy exam weeks?",
            "How do you handle situations when team members have conflicting priorities in a group project?",
            "What is one healthy boundary you set to protect your personal time after classes?",
            "How do you encourage quiet team members to share their ideas without feeling put on the spot?",
            "When academic pressure mounts, who or what is your primary source of support?",
            "What small daily habit helps you reset your mind after a long day of lectures?",
            "I've realized that taking 10-minute walk breaks between study blocks keeps my mind surprisingly clear.",
            "Setting up clear communication channels early in team projects prevents so much last-minute anxiety.",
            "I find that speaking up about workload stress with peers helps me realize I'm not alone in feeling overwhelmed.",
            "Prioritizing consistent sleep hygiene completely changed how I handle exam week pressure.",
            "Learning to say no to extra non-essential commitments gives me space to recharge mentally.",
            "Active listening and validating each other's ideas makes group discussions feel like a safe haven.",
            "What is a personal stress indicator that tells you it's time to take a mental break?",
            "How do you stay motivated when an assignment feels completely intimidating or tedious?",
            "Have you ever tried time-blocking your day, and did it help reduce your evening study stress?",
            "What's your go-to strategy when you feel like you're falling behind on group project deliverables?",
            "I've started turning off my notifications during study sessions, and my focus level doubled almost immediately.",
            "When deadlines overlap, I rank tasks by urgency rather than trying to finish everything at once.",
            "Building a supportive peer study circle makes preparing for tough exams feel less isolating.",
            "What advice would you give to a freshman struggling to balance campus social life and coursework?",
            "How do you check in on a classmate who seems quiet or overwhelmed during group meetings?",
            "I find that breaking long assignments into 25-minute sprints helps me overcome initial procrastination.",
            "Taking time out for hobbies without feeling guilty is essential for preventing long-term burnout.",
            "What is one communication habit that has helped your project teams run smoothly without friction?",
            "How do you restore your energy when you feel mentally exhausted after a long lab or lecture series?",
            "Normalizing honest conversations about academic pressure helps everyone feel more supported.",
            "What personal boundary has had the biggest positive impact on your mental well-being this semester?",
            "How do you handle constructive criticism from peers without taking it personally?",
            "Creating a calm study environment free of clutter makes a surprisingly big difference in my focus.",
            "What is a positive habit you built this year that you wish you had started earlier in college?",
            "How do you reset your mindset after experiencing an unexpected setback on a test or project?"
        };

        private void Awake()
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            LoadSecureAPIKey();
        }

        private void Start()
        {
            if (enableModelDiscovery && !string.IsNullOrEmpty(apiKey) && !apiKey.Trim().StartsWith("sk-"))
            {
                StartCoroutine(DiscoverAvailableGeminiModel((bestModel) =>
                {
                    Debug.Log("[Gemini] Model discovery complete. Active model: " + bestModel);
                }));
            }
        }

        public IEnumerator TestGeminiConnection(Action<bool, string> onTestComplete)
        {
            Debug.Log("[Gemini] Initializing connection test...");
            if (string.IsNullOrEmpty(apiKey)) LoadSecureAPIKey();

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("[Gemini] Connection test failed: API key missing.");
                onTestComplete?.Invoke(false, "API key missing.");
                yield break;
            }

            if (enableModelDiscovery && string.IsNullOrEmpty(discoveredModelName) && !apiKey.Trim().StartsWith("sk-"))
            {
                yield return DiscoverAvailableGeminiModel(null);
            }

            string testPrompt = "Respond with exactly: GEMINI_CONNECTION_OK";
            string testResult = string.Empty;

            yield return SendGeminiRequest(testPrompt, (res) =>
            {
                testResult = res;
            });

            if (!string.IsNullOrEmpty(testResult) && testResult.Contains("GEMINI_CONNECTION_OK"))
            {
                Debug.Log("[Gemini] Connection test successful! Verified active model: " + GetActiveModelName());
                onTestComplete?.Invoke(true, "Connection successful with model: " + GetActiveModelName());
            }
            else if (!string.IsNullOrEmpty(testResult))
            {
                Debug.Log("[Gemini] Connection response received: " + testResult);
                onTestComplete?.Invoke(true, "API connected. Response: " + testResult);
            }
            else
            {
                Debug.LogWarning("[Gemini] Connection test failed to receive response.");
                onTestComplete?.Invoke(false, "Connection test failed.");
            }
        }

        public IEnumerator DiscoverAvailableGeminiModel(Action<string> onDiscovered)
        {
            if (!string.IsNullOrEmpty(discoveredModelName))
            {
                onDiscovered?.Invoke(discoveredModelName);
                yield break;
            }

            if (isDiscoveringModel)
            {
                while (isDiscoveringModel) yield return null;
                onDiscovered?.Invoke(GetActiveModelName());
                yield break;
            }

            isDiscoveringModel = true;
            string cleanKey = apiKey.Trim();
            if (string.IsNullOrEmpty(cleanKey) || cleanKey.StartsWith("sk-"))
            {
                isDiscoveringModel = false;
                onDiscovered?.Invoke(modelName);
                yield break;
            }

            Debug.Log("[Gemini] Discovering available models via Gemini Models API...");
            string listModelsUrl = string.Format("https://generativelanguage.googleapis.com/v1beta/models?key={0}", cleanKey);

            using (UnityWebRequest request = UnityWebRequest.Get(listModelsUrl))
            {
                request.timeout = Mathf.RoundToInt(apiTimeout);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        GeminiModelListResponse listResponse = JsonUtility.FromJson<GeminiModelListResponse>(json);
                        if (listResponse != null && listResponse.models != null && listResponse.models.Length > 0)
                        {
                            string selected = FilterBestGenerateContentModel(listResponse.models);
                            if (!string.IsNullOrEmpty(selected))
                            {
                                discoveredModelName = SanitizeModelName(selected);
                                Debug.Log("[Gemini] Discovered optimal model: " + discoveredModelName);
                                isDiscoveringModel = false;
                                onDiscovered?.Invoke(discoveredModelName);
                                yield break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[Gemini] Failed to parse Models API JSON: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("[Gemini] Models API lookup failed (" + request.responseCode + "): " + request.error);
                }
            }

            isDiscoveringModel = false;
            if (string.IsNullOrEmpty(discoveredModelName)) discoveredModelName = modelName;
            onDiscovered?.Invoke(discoveredModelName);
        }

        private string FilterBestGenerateContentModel(GeminiModelInfo[] models)
        {
            List<string> flashCandidates = new List<string>();
            List<string> generateCandidates = new List<string>();

            foreach (var m in models)
            {
                if (m == null || string.IsNullOrEmpty(m.name)) continue;
                bool supportsGenerate = false;
                if (m.supportedGenerationMethods != null)
                {
                    foreach (var method in m.supportedGenerationMethods)
                    {
                        if (method == "generateContent")
                        {
                            supportsGenerate = true;
                            break;
                        }
                    }
                }

                if (supportsGenerate)
                {
                    string cleanName = SanitizeModelName(m.name);
                    generateCandidates.Add(cleanName);

                    // Prefer fast, low-latency Flash models for conversational GD
                    if (cleanName.Contains("3.5-flash") || cleanName.Contains("3.8-flash") || cleanName.Contains("3.1-flash-lite") || cleanName.Contains("2.5-flash-lite") || cleanName.Contains("flash-latest") || cleanName.Contains("flash"))
                    {
                        flashCandidates.Add(cleanName);
                    }
                }
            }

            // Prioritize specific high-quality Flash models
            string[] preferredOrder = new string[]
            {
                "gemini-3.5-flash",
                "gemini-3.8-flash",
                "gemini-3.1-flash-lite",
                "gemini-2.5-flash-lite",
                "gemini-flash-latest",
                "gemini-3.5-flash-lite",
                "gemini-3.6-flash"
            };

            foreach (string p in preferredOrder)
            {
                if (generateCandidates.Contains(p)) return p;
            }

            if (flashCandidates.Count > 0) return flashCandidates[0];
            if (generateCandidates.Count > 0) return generateCandidates[0];

            return modelName;
        }

        private string SanitizeModelName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "gemini-3.5-flash";
            string s = raw.Trim();
            if (s.StartsWith("models/")) s = s.Substring(7);
            return s;
        }

        public string GetActiveModelName()
        {
            if (!string.IsNullOrEmpty(discoveredModelName)) return discoveredModelName;
            return SanitizeModelName(modelName);
        }

        public IEnumerator GeneratePeerStatement(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onStatementReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            UpdateDialogueHistory(recentDialogueHistory);

            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());
            string uniqueSeed = Guid.NewGuid().ToString().Substring(0, 6);

            string systemPrompt = string.Format(
                "System Context:\n" +
                "You are participating in an interactive, dynamic college student mental health group discussion.\n" +
                "Random Seed: {0}. Your name is '{1}' and your persona is: '{2}'.\n" +
                "Current Discussion Topic: '{3}'.\n" +
                "Recent Dialogue History:\n{4}\n\n" +
                "CRITICAL INSTRUCTION: Generate a completely unique, empathetic 1-2 sentence peer statement or personal insight on the topic.\n" +
                "Express a distinct perspective matching your persona. DO NOT repeat any previous ideas or phrasing.",
                uniqueSeed, peerName, peerPersona, topic, historyContext
            );

            yield return SendGeminiRequest(systemPrompt, (res) =>
            {
                string cleaned = CleanResponse(res);
                if (string.IsNullOrEmpty(cleaned) || askedQuestionsHistory.Contains(cleaned) || (questionHistory != null && questionHistory.IsDuplicateOrSimilar(cleaned)))
                {
                    cleaned = GetUniqueFallbackStatement();
                }
                askedQuestionsHistory.Add(cleaned);
                if (questionHistory != null) questionHistory.AddStatement(cleaned);
                fullConversationHistory.Add(peerName + ": " + cleaned);
                onStatementReceived?.Invoke(cleaned);
            });
        }

        public IEnumerator GeneratePeerQuestion(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onQuestionReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            UpdateDialogueHistory(recentDialogueHistory);

            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());
            int attempt = 0;
            string generatedQuestion = string.Empty;
            bool questionAccepted = false;

            while (attempt < 2 && !questionAccepted)
            {
                attempt++;
                string uniqueSeed = Guid.NewGuid().ToString().Substring(0, 6);

                string attemptInstruction = (attempt == 1)
                    ? "Ask a fresh, open-ended 1-sentence question for the group and Kevin to reflect on. Make it thoughtful, authentic, and unique."
                    : "IMPORTANT: The previous question attempt was too similar to an existing question. Ask a COMPLETELY DIFFERENT question focusing on practical consequences or alternative solutions.";

                string systemPrompt = string.Format(
                    "System Context:\n" +
                    "You are participating in a college student mental health group discussion.\n" +
                    "Random Seed: {0}. Your name is '{1}' and your persona is: '{2}'.\n" +
                    "Current Topic: '{3}'.\n" +
                    "Recent Dialogue Context:\n{4}\n\n" +
                    "CRITICAL INSTRUCTION: {5} DO NOT repeat any previously asked questions, concepts, or wording.",
                    uniqueSeed, peerName, peerPersona, topic, historyContext, attemptInstruction
                );

                yield return SendGeminiRequest(systemPrompt, (res) =>
                {
                    string cleaned = CleanResponse(res);
                    if (!string.IsNullOrEmpty(cleaned) && !IsDuplicateOrSimilarQuestion(cleaned))
                    {
                        generatedQuestion = cleaned;
                        questionAccepted = true;
                    }
                });
            }

            if (!questionAccepted || string.IsNullOrEmpty(generatedQuestion))
            {
                Debug.Log("[Gemini] Duplicate question detected or API empty. Using unique non-repeating question.");
                generatedQuestion = GetUniqueFallbackStatement();
            }

            askedQuestionsHistory.Add(generatedQuestion);
            if (questionHistory != null) questionHistory.AddQuestion(generatedQuestion, 4, topic, QuestionIntent.Clarification, 2);
            fullConversationHistory.Add(peerName + ": " + generatedQuestion);
            onQuestionReceived?.Invoke(generatedQuestion);
        }

        public IEnumerator GeneratePeerResponse(string peerName, string peerPersona, string topic, string studentInput, Action<string> onResponseReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            string uniqueSeed = Guid.NewGuid().ToString().Substring(0, 6);

            string systemPrompt = string.Format(
                "System Context:\n" +
                "You are participating in a supportive college group discussion on student mental health.\n" +
                "Random Seed: {0}. Your name is '{1}' and your persona is: '{2}'.\n" +
                "Topic: '{3}'. The assessed student (Kevin) just shared: \"{4}\".\n\n" +
                "CRITICAL INSTRUCTION: Respond directly to Kevin's statement with an empathetic, thoughtful 2-sentence feedback or follow-up reflection.\n" +
                "Acknowledge Kevin's specific point and build upon it naturally. You may agree, respectfully challenge, or ask a contextual follow-up.",
                uniqueSeed, peerName, peerPersona, topic, studentInput
            );

            yield return SendGeminiRequest(systemPrompt, (res) =>
            {
                string cleaned = CleanResponse(res);
                if (string.IsNullOrEmpty(cleaned) || (questionHistory != null && questionHistory.IsDuplicateOrSimilar(cleaned)))
                {
                    cleaned = GetUniqueFallbackStatement();
                }
                if (questionHistory != null) questionHistory.AddStatement(cleaned);
                fullConversationHistory.Add(peerName + ": " + cleaned);
                onResponseReceived?.Invoke(cleaned);
            });
        }

        private IEnumerator SendGeminiRequest(string prompt, Action<string> onResult)
        {
            if (string.IsNullOrEmpty(apiKey)) LoadSecureAPIKey();

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogWarning("[Gemini] API Key missing. Falling back to local peer pool.");
                onResult?.Invoke(GetUniqueFallbackStatement());
                yield break;
            }

            // Route to OpenAI API if key starts with sk-
            if (apiKey.Trim().StartsWith("sk-"))
            {
                yield return SendOpenAIRequest(prompt, onResult);
                yield break;
            }

            if (enableModelDiscovery && string.IsNullOrEmpty(discoveredModelName))
            {
                yield return DiscoverAvailableGeminiModel(null);
            }

            string activeModel = GetActiveModelName();
            string cleanKey = apiKey.Trim();
            int retryCount = 0;
            bool success = false;
            string finalResponse = string.Empty;

            while (retryCount <= maxRetries && !success)
            {
                retryCount++;
                string url = string.Format("https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}", activeModel, cleanKey);

                string jsonPayload = "{\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"" + EscapeJsonString(prompt) + "\"}]}],\"generationConfig\":{\"temperature\":0.85,\"topP\":0.95}}";
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = Mathf.RoundToInt(apiTimeout);

                    Debug.Log(string.Format("[Gemini] Sending generateContent request (Attempt {0}/{1}) to model: {2}", retryCount, maxRetries + 1, activeModel));
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            string responseJson = request.downloadHandler.text;
                            GeminiResponseData parsed = JsonUtility.FromJson<GeminiResponseData>(responseJson);
                            if (parsed != null && parsed.candidates != null && parsed.candidates.Length > 0 && parsed.candidates[0].content.parts.Length > 0)
                            {
                                string aiText = parsed.candidates[0].content.parts[0].text.Trim();
                                if (!string.IsNullOrEmpty(aiText))
                                {
                                    finalResponse = aiText;
                                    success = true;
                                    Debug.Log("[Gemini] Live AI response generated successfully: " + aiText);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("[Gemini] Error parsing response JSON: " + ex.Message);
                        }
                    }
                    else
                    {
                        long code = request.responseCode;
                        string errText = request.downloadHandler != null ? request.downloadHandler.text : "";
                        Debug.LogWarning(string.Format("[Gemini] Request failed (HTTP {0}): {1} | Response: {2}", code, request.error, errText));

                        // 404 Recovery: Selected model is unavailable -> Rediscover available models & retry
                        if (code == 404 || errText.Contains("404") || errText.Contains("NOT_FOUND") || errText.Contains("no longer available"))
                        {
                            Debug.LogWarning("[Gemini] HTTP 404 - Selected model '" + activeModel + "' unavailable. Rediscovering available models...");
                            discoveredModelName = null;
                            yield return DiscoverAvailableGeminiModel(null);
                            activeModel = GetActiveModelName();
                        }
                        // 429 Rate Limit: Exponential Backoff
                        else if (code == 429 || errText.Contains("429") || errText.Contains("RESOURCE_EXHAUSTED"))
                        {
                            float backoffSeconds = Mathf.Pow(2, retryCount);
                            Debug.LogWarning(string.Format("[Gemini] HTTP 429 Rate limit encountered. Backing off for {0:F1}s before retry...", backoffSeconds));
                            yield return new WaitForSeconds(backoffSeconds);
                        }
                        // 503 High Demand Spikes: Short Wait
                        else if (code == 503 || errText.Contains("UNAVAILABLE") || errText.Contains("high demand"))
                        {
                            Debug.LogWarning("[Gemini] HTTP 503 High demand spike. Retrying after brief delay...");
                            yield return new WaitForSeconds(1.5f);
                        }
                        else
                        {
                            yield return new WaitForSeconds(1.0f);
                        }
                    }
                }
            }

            if (success && !string.IsNullOrEmpty(finalResponse))
            {
                onResult?.Invoke(finalResponse);
            }
            else
            {
                Debug.LogWarning("[Gemini] All Gemini retries exhausted or network unavailable. Using graceful local fallback.");
                onResult?.Invoke(GetUniqueFallbackStatement());
            }
        }

        private IEnumerator SendOpenAIRequest(string prompt, Action<string> onResult)
        {
            string url = "https://api.openai.com/v1/chat/completions";
            string systemInstruction = "You are a creative, highly versatile college student participating in an interactive group discussion on student mental health. Never repeat previous questions, phrases, or ideas. Keep responses to 1-2 empathetic sentences.";
            
            string jsonPayload = "{\"model\":\"gpt-4o-mini\",\"messages\":[{\"role\":\"system\",\"content\":\"" + EscapeJsonString(systemInstruction) + "\"},{\"role\":\"user\",\"content\":\"" + EscapeJsonString(prompt) + "\"}],\"temperature\":0.85,\"top_p\":0.95}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());
                request.timeout = Mathf.RoundToInt(apiTimeout);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseJson = request.downloadHandler.text;
                        OpenAIResponseData parsed = JsonUtility.FromJson<OpenAIResponseData>(responseJson);
                        if (parsed != null && parsed.choices != null && parsed.choices.Length > 0 && parsed.choices[0].message != null)
                        {
                            string aiText = parsed.choices[0].message.content.Trim();
                            if (!string.IsNullOrEmpty(aiText))
                            {
                                Debug.Log("[OpenAI API Success] Generated live AI response: " + aiText);
                                onResult?.Invoke(aiText);
                                yield break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[OpenAI] Error parsing JSON: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("OpenAI API WebRequest failed (" + request.responseCode + "): " + request.error);
                }

                yield return SendOpenAIFallbackModelRequest(prompt, onResult);
            }
        }

        private IEnumerator SendOpenAIFallbackModelRequest(string prompt, Action<string> onResult)
        {
            string url = "https://api.openai.com/v1/chat/completions";
            string systemInstruction = "You are a creative college student in a group discussion on mental health. Keep responses concise and fresh.";
            string jsonPayload = "{\"model\":\"gpt-3.5-turbo\",\"messages\":[{\"role\":\"system\",\"content\":\"" + EscapeJsonString(systemInstruction) + "\"},{\"role\":\"user\",\"content\":\"" + EscapeJsonString(prompt) + "\"}],\"temperature\":0.85}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());
                request.timeout = Mathf.RoundToInt(apiTimeout);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string responseJson = request.downloadHandler.text;
                        OpenAIResponseData parsed = JsonUtility.FromJson<OpenAIResponseData>(responseJson);
                        if (parsed != null && parsed.choices != null && parsed.choices.Length > 0 && parsed.choices[0].message != null)
                        {
                            string aiText = parsed.choices[0].message.content.Trim();
                            if (!string.IsNullOrEmpty(aiText))
                            {
                                Debug.Log("[OpenAI Fallback Model Success]: " + aiText);
                                onResult?.Invoke(aiText);
                                yield break;
                            }
                        }
                    }
                    catch { }
                }

                onResult?.Invoke(GetUniqueFallbackStatement());
            }
        }

        public bool IsDuplicateOrSimilarQuestion(string candidateQuestion)
        {
            if (string.IsNullOrEmpty(candidateQuestion)) return true;
            if (!enableDuplicateQuestionDetection) return false;

            string normCandidate = NormalizeText(candidateQuestion);
            if (askedQuestionsHistory.Contains(candidateQuestion) || askedQuestionsHistory.Contains(normCandidate)) return true;

            if (questionHistory != null && questionHistory.IsDuplicateOrSimilar(candidateQuestion)) return true;

            HashSet<string> candidateWords = GetSignificantWords(normCandidate);
            if (candidateWords.Count == 0) return false;

            foreach (string q in askedQuestionsHistory)
            {
                HashSet<string> existingWords = GetSignificantWords(NormalizeText(q));
                if (existingWords.Count == 0) continue;

                int intersect = 0;
                foreach (string w in candidateWords)
                {
                    if (existingWords.Contains(w)) intersect++;
                }

                float jaccard = (float)intersect / (candidateWords.Count + existingWords.Count - intersect);
                if (jaccard > 0.55f)
                {
                    Debug.Log(string.Format("[Gemini] Rewording detected! Jaccard similarity: {0:F2} between '{1}' and '{2}'", jaccard, candidateQuestion, q));
                    return true;
                }
            }

            return false;
        }

        private HashSet<string> GetSignificantWords(string text)
        {
            HashSet<string> words = new HashSet<string>();
            string[] raw = text.Split(new char[] { ' ', '\t', '\n', '\r', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> stopWords = new HashSet<string> { "what", "how", "why", "do", "you", "think", "is", "are", "the", "a", "an", "in", "on", "to", "for", "of", "and", "or", "your", "that", "this", "it", "with" };

            foreach (string w in raw)
            {
                string lower = w.ToLower().Trim();
                if (lower.Length > 2 && !stopWords.Contains(lower))
                {
                    words.Add(lower);
                }
            }
            return words;
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return Regex.Replace(text.ToLower(), @"[^\w\s]", "").Trim();
        }

        private void UpdateDialogueHistory(List<string> recentHistory)
        {
            if (recentHistory == null) return;
            foreach (var h in recentHistory)
            {
                if (!fullConversationHistory.Contains(h))
                {
                    fullConversationHistory.Add(h);
                }
            }
        }

        private string GetUniqueFallbackStatement()
        {
            for (int i = 0; i < nonRepeatingPeerPool.Length; i++)
            {
                int idx = (fallbackIndex + i) % nonRepeatingPeerPool.Length;
                string q = nonRepeatingPeerPool[idx];
                if (!askedQuestionsHistory.Contains(q))
                {
                    fallbackIndex = idx + 1;
                    return q;
                }
            }
            return nonRepeatingPeerPool[UnityEngine.Random.Range(0, nonRepeatingPeerPool.Length)];
        }

        private string CleanResponse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string cleaned = raw.Replace("\"", "").Trim();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < cleaned.Length; i++)
            {
                char c = cleaned[i];
                if (char.IsSurrogate(c)) continue;
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }

        public string LoadSecureAPIKey()
        {
            try
            {
                string rootConfigPath = System.IO.Path.Combine(Application.dataPath, "../gemini_config.json");
                if (System.IO.File.Exists(rootConfigPath))
                {
                    string json = System.IO.File.ReadAllText(rootConfigPath);
                    if (json.Contains("apiKey"))
                    {
                        int idx = json.IndexOf("\"apiKey\":");
                        if (idx != -1)
                        {
                            int start = json.IndexOf("\"", idx + 9) + 1;
                            int end = json.IndexOf("\"", start);
                            if (start > 0 && end > start)
                            {
                                string k = json.Substring(start, end - start).Trim();
                                if (!string.IsNullOrEmpty(k))
                                {
                                    apiKey = k;
                                    return apiKey;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            string envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(envKey)) envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(envKey)) envKey = Environment.GetEnvironmentVariable("UNITY_GEMINI_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                apiKey = envKey.Trim();
                return apiKey;
            }

            string configPath = System.IO.Path.Combine(Application.persistentDataPath, "gemini_key.json");
            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        apiKey = json.Trim();
                        return apiKey;
                    }
                }
                catch { }
            }

            string prefKey = PlayerPrefs.GetString("GEMINI_SECURE_KEY", "");
            if (!string.IsNullOrEmpty(prefKey))
            {
                apiKey = prefKey.Trim();
                return apiKey;
            }

            return apiKey;
        }

        public void SaveSecureAPIKey(string newKey)
        {
            if (string.IsNullOrEmpty(newKey)) return;
            string trimmed = newKey.Trim();
            apiKey = trimmed;

            try
            {
                string configPath = System.IO.Path.Combine(Application.persistentDataPath, "gemini_key.json");
                System.IO.File.WriteAllText(configPath, trimmed);

                string rootConfigPath = System.IO.Path.Combine(Application.dataPath, "../gemini_config.json");
                System.IO.File.WriteAllText(rootConfigPath, "{\n  \"apiKey\": \"" + trimmed + "\"\n}");

                PlayerPrefs.SetString("GEMINI_SECURE_KEY", trimmed);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not persist API key locally: " + ex.Message);
            }
        }
    }
}
