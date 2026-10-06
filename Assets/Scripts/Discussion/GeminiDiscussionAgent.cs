using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MentalHealthApp.Discussion
{
    [Serializable]
    public class GeminiResponseData
    {
        public Candidate[] candidates;
    }

    [Serializable]
    public class Candidate
    {
        public Content content;
    }

    [Serializable]
    public class Content
    {
        public Part[] parts;
    }

    [Serializable]
    public class Part
    {
        public string text;
    }

    public class GeminiDiscussionAgent : MonoBehaviour
    {
        [Header("API Settings")]
        public string apiKey = string.Empty;
        public string modelName = "gemini-1.5-flash";

        private HashSet<string> askedQuestionsHistory = new HashSet<string>();
        private int fallbackIndex = 0;
        private string sessionGuid = Guid.NewGuid().ToString().Substring(0, 8);

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
            "What is a personal stress indicator that tells you it's time to take a mental break?"
        };

        [Header("Question Memory Reference")]
        public GDQuestionHistory questionHistory;

        public IEnumerator GeneratePeerStatement(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onStatementReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());
            string systemPrompt = string.Format(
                "You are participating in an authentic college student mental health group discussion. " +
                "Session ID: {0}. Your name is {1} and your persona is: {2}. " +
                "Topic: '{3}'. " +
                "Previous statements in the group:\n{4}\n" +
                "CRITICAL RULE: Give a completely unique, highly creative, empathetic 1-2 sentence statement or tip on the topic. DO NOT repeat any previous ideas, phrases, or questions.",
                sessionGuid, peerName, peerPersona, topic, historyContext
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
                onStatementReceived?.Invoke(cleaned);
            });
        }

        public IEnumerator GeneratePeerQuestion(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onQuestionReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());

            string systemPrompt = string.Format(
                "You are participating in an interactive college student mental health group discussion. " +
                "Session ID: {0}. Your name is {1} and your persona is: {2}. " +
                "Topic: '{3}'. " +
                "Previous statements in the group:\n{4}\n" +
                "CRITICAL RULE: DO NOT REPEAT ANY PREVIOUS QUESTION OR PHRASE. Ask a fresh, insightful, open-ended 1-sentence question for the group to explore.",
                sessionGuid, peerName, peerPersona, topic, historyContext
            );

            yield return SendGeminiRequest(systemPrompt, (res) =>
            {
                string cleaned = CleanResponse(res);
                if (askedQuestionsHistory.Contains(cleaned) || string.IsNullOrEmpty(cleaned) || (questionHistory != null && questionHistory.IsDuplicateOrSimilar(cleaned)))
                {
                    cleaned = GetUniqueFallbackStatement();
                }
                askedQuestionsHistory.Add(cleaned);
                if (questionHistory != null) questionHistory.AddQuestion(cleaned, 4, topic, QuestionIntent.Clarification, 2);
                onQuestionReceived?.Invoke(cleaned);
            });
        }

        public IEnumerator GeneratePeerResponse(string peerName, string peerPersona, string topic, string studentInput, Action<string> onResponseReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            string systemPrompt = string.Format(
                "You are participating in a supportive college group discussion on student mental health. " +
                "Session ID: {0}. Your name is {1} and your persona is: {2}. " +
                "Topic: '{3}'. The student (Kevin) just said: \"{4}\". " +
                "CRITICAL RULE: Give a fresh, dynamic, empathetic 2-sentence response building directly on Kevin's input. Do NOT repeat previous phrases.",
                sessionGuid, peerName, peerPersona, topic, studentInput
            );

            yield return SendGeminiRequest(systemPrompt, (res) =>
            {
                string cleaned = CleanResponse(res);
                if (string.IsNullOrEmpty(cleaned) || (questionHistory != null && questionHistory.IsDuplicateOrSimilar(cleaned)))
                {
                    cleaned = GetUniqueFallbackStatement();
                }
                if (questionHistory != null) questionHistory.AddStatement(cleaned);
                onResponseReceived?.Invoke(cleaned);
            });
        }

        private IEnumerator SendGeminiRequest(string prompt, Action<string> onResult)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                onResult?.Invoke(GetUniqueFallbackStatement());
                yield break;
            }

            string url = string.Format("https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}", modelName, apiKey);
            string jsonPayload = "{\"contents\":[{\"parts\":[{\"text\":\"" + EscapeJsonString(prompt) + "\"}]}],\"generationConfig\":{\"temperature\":0.95,\"topP\":0.95}}";

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

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
                            onResult?.Invoke(aiText);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error parsing Gemini API JSON: " + ex.Message);
                    }
                }

                // Fallback on error
                onResult?.Invoke(GetUniqueFallbackStatement());
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
            return raw.Replace("\"", "").Trim();
        }

        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }

        public string LoadSecureAPIKey()
        {
            // 1. Check System Environment Variable first
            string envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(envKey)) envKey = Environment.GetEnvironmentVariable("UNITY_GEMINI_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                apiKey = envKey.Trim();
                return apiKey;
            }

            // 2. Check Local Persistent Device Storage
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

            // 3. Check PlayerPrefs Fallback
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
                // Save locally outside git repo to persistentDataPath
                string configPath = System.IO.Path.Combine(Application.persistentDataPath, "gemini_key.json");
                System.IO.File.WriteAllText(configPath, trimmed);
                PlayerPrefs.SetString("GEMINI_SECURE_KEY", trimmed);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not persist Gemini key locally: " + ex.Message);
            }
        }
    }
}
