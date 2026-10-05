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

        private string[] nonRepeatingPeerQuestions = new string[]
        {
            "How do you usually recognize when study stress is starting to affect your daily routine?",
            "What strategies have you found most helpful for staying focused during heavy exam weeks?",
            "How do you handle situations when team members have conflicting priorities in a group project?",
            "What is one healthy boundary you set to protect your personal time after classes?",
            "How do you encourage quiet team members to share their ideas without feeling put on the spot?",
            "When academic pressure mounts, who or what is your primary source of support?",
            "What small daily habit helps you reset your mind after a long day of lectures?"
        };

        public IEnumerator GeneratePeerStatement(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onStatementReceived)
        {
            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());
            string systemPrompt = string.Format(
                "You are participating in a supportive college group discussion on student mental health. " +
                "Your name is {0} and your persona is: {1}. " +
                "Topic: '{2}'. " +
                "Previous statements in the group:\n{3}\n" +
                "CRITICAL RULE: Share a fresh, empathetic 1-2 sentence thought or strategy on the topic. Do NOT repeat previous points or questions.",
                peerName, peerPersona, topic, historyContext
            );

            yield return SendGeminiRequest(systemPrompt, (res) =>
            {
                string cleaned = CleanResponse(res);
                if (string.IsNullOrEmpty(cleaned)) cleaned = GetUniqueFallbackQuestion();
                onStatementReceived?.Invoke(cleaned);
            });
        }

        public IEnumerator GeneratePeerQuestion(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onQuestionReceived)
        {
            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());

            string systemPrompt = string.Format(
                "You are participating in a college group discussion on student mental health and well-being. " +
                "Your name is {0} and your persona is: {1}. " +
                "The topic is '{2}'. " +
                "Previous statements made in the group:\n{3}\n" +
                "CRITICAL RULE: DO NOT REPEAT ANY PREVIOUS QUESTION OR STATEMENT. Ask a fresh, insightful, open-ended 1-sentence question or make a new point for the group to discuss.",
                peerName, peerPersona, topic, historyContext
            );

            yield return SendGeminiRequest(systemPrompt, (res) =>
            {
                string cleaned = CleanResponse(res);
                if (askedQuestionsHistory.Contains(cleaned) || string.IsNullOrEmpty(cleaned))
                {
                    cleaned = GetUniqueFallbackQuestion();
                }
                askedQuestionsHistory.Add(cleaned);
                onQuestionReceived?.Invoke(cleaned);
            });
        }

        public IEnumerator GeneratePeerResponse(string peerName, string peerPersona, string topic, string studentInput, Action<string> onResponseReceived)
        {
            string systemPrompt = string.Format(
                "You are participating in a supportive college group discussion on student mental health. " +
                "Your name is {0} and your persona is: {1}. " +
                "Topic: '{2}'. The student just said: \"{3}\". " +
                "CRITICAL RULE: Do NOT repeat previous questions. Give a fresh, empathetic 2-sentence response building directly on their thoughts.",
                peerName, peerPersona, topic, studentInput
            );

            yield return SendGeminiRequest(systemPrompt, (res) =>
            {
                string cleaned = CleanResponse(res);
                onResponseReceived?.Invoke(cleaned);
            });
        }

        private IEnumerator SendGeminiRequest(string prompt, Action<string> onResult)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.Log("Gemini API Key empty. Using unique fallback question.");
                onResult?.Invoke(GetUniqueFallbackQuestion());
                yield break;
            }

            string url = string.Format("https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}", modelName, apiKey);
            string jsonPayload = "{\"contents\":[{\"parts\":[{\"text\":\"" + EscapeJsonString(prompt) + "\"}]}]}";

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
                onResult?.Invoke(GetUniqueFallbackQuestion());
            }
        }

        private string GetUniqueFallbackQuestion()
        {
            for (int i = 0; i < nonRepeatingPeerQuestions.Length; i++)
            {
                int idx = (fallbackIndex + i) % nonRepeatingPeerQuestions.Length;
                string q = nonRepeatingPeerQuestions[idx];
                if (!askedQuestionsHistory.Contains(q))
                {
                    fallbackIndex = idx + 1;
                    return q;
                }
            }
            return nonRepeatingPeerQuestions[fallbackIndex++ % nonRepeatingPeerQuestions.Length];
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
    }
}
