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

        [Header("Question Memory Reference")]
        public GDQuestionHistory questionHistory;

        public IEnumerator GeneratePeerStatement(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onStatementReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());
            string uniqueSeed = Guid.NewGuid().ToString().Substring(0, 6);

            string systemPrompt = string.Format(
                "You are participating in an interactive, highly dynamic college student mental health group discussion. " +
                "Random Seed: {0}. Your name is {1} and your distinct personality is: {2}. " +
                "Current Discussion Topic: '{3}'. " +
                "Recent dialogue history:\n{4}\n" +
                "CRITICAL INSTRUCTION: Generate a completely unique, highly creative, empathetic 1-2 sentence peer statement or personal insight on the topic. " +
                "Express a distinct perspective matching your persona. DO NOT repeat any previous ideas, phrases, or questions.",
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
                onStatementReceived?.Invoke(cleaned);
            });
        }

        public IEnumerator GeneratePeerQuestion(string peerName, string peerPersona, string topic, List<string> recentDialogueHistory, Action<string> onQuestionReceived)
        {
            if (questionHistory == null) questionHistory = GetComponent<GDQuestionHistory>();
            string historyContext = string.Join("\n", recentDialogueHistory.ToArray());
            string uniqueSeed = Guid.NewGuid().ToString().Substring(0, 6);

            string systemPrompt = string.Format(
                "You are participating in an intuitive college student mental health group discussion. " +
                "Random Seed: {0}. Your name is {1} and your persona is: {2}. " +
                "Current Topic: '{3}'. " +
                "Recent dialogue context:\n{4}\n" +
                "CRITICAL INSTRUCTION: Ask a fresh, intuitive, highly open-ended 1-sentence question for the group and Kevin to reflect on. " +
                "Make it thoughtful, authentic, and unique. DO NOT repeat any previous questions, concepts, or wording.",
                uniqueSeed, peerName, peerPersona, topic, historyContext
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
            string uniqueSeed = Guid.NewGuid().ToString().Substring(0, 6);

            string systemPrompt = string.Format(
                "You are participating in a supportive college group discussion on student mental health. " +
                "Random Seed: {0}. Your name is {1} and your persona is: {2}. " +
                "Topic: '{3}'. The assessed student (Kevin) just shared: \"{4}\". " +
                "CRITICAL INSTRUCTION: Respond directly to Kevin's statement with an empathetic, thoughtful 2-sentence feedback or follow-up reflection. " +
                "Acknowledge Kevin's specific point and build upon it naturally. Do NOT repeat previous phrases.",
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
                onResponseReceived?.Invoke(cleaned);
            });
        }

        private IEnumerator SendGeminiRequest(string prompt, Action<string> onResult)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                LoadSecureAPIKey();
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                onResult?.Invoke(GetUniqueFallbackStatement());
                yield break;
            }

            // Route to OpenAI API if key starts with sk-
            if (apiKey.Trim().StartsWith("sk-"))
            {
                yield return SendOpenAIRequest(prompt, onResult);
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

        private IEnumerator SendOpenAIRequest(string prompt, Action<string> onResult)
        {
            string url = "https://api.openai.com/v1/chat/completions";
            string systemInstruction = "You are a creative, highly versatile college student participating in an interactive group discussion on student mental health. Never repeat previous questions, phrases, or ideas. Keep responses to 1-2 empathetic sentences.";
            
            string jsonPayload = "{\"model\":\"gpt-4o-mini\",\"messages\":[{\"role\":\"system\",\"content\":\"" + EscapeJsonString(systemInstruction) + "\"},{\"role\":\"user\",\"content\":\"" + EscapeJsonString(prompt) + "\"}],\"temperature\":0.95,\"top_p\":0.95}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

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
                        Debug.LogError("Error parsing OpenAI API JSON response: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("OpenAI API WebRequest failed (" + request.responseCode + "): " + request.error + " | Response: " + request.downloadHandler.text);
                }

                // If gpt-4o-mini fails, try gpt-3.5-turbo fallback before static pool
                yield return SendOpenAIFallbackModelRequest(prompt, onResult);
            }
        }

        private IEnumerator SendOpenAIFallbackModelRequest(string prompt, Action<string> onResult)
        {
            string url = "https://api.openai.com/v1/chat/completions";
            string systemInstruction = "You are a creative college student in a group discussion on mental health. Keep responses concise and fresh.";
            string jsonPayload = "{\"model\":\"gpt-3.5-turbo\",\"messages\":[{\"role\":\"system\",\"content\":\"" + EscapeJsonString(systemInstruction) + "\"},{\"role\":\"user\",\"content\":\"" + EscapeJsonString(prompt) + "\"}],\"temperature\":0.9}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

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
            // Remove emoji surrogate pairs that cause font fallback warnings
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
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
            // 1. Check Project Root Git-Ignored Config File
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

            // 2. Check System Environment Variable
            string envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(envKey)) envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(envKey)) envKey = Environment.GetEnvironmentVariable("UNITY_GEMINI_API_KEY");
            if (!string.IsNullOrEmpty(envKey))
            {
                apiKey = envKey.Trim();
                return apiKey;
            }

            // 3. Check Local Persistent Device Storage
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

            // 4. Check PlayerPrefs Fallback
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
                // Save locally outside git repo to persistentDataPath and root config
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
