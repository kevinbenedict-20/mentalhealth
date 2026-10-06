using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MentalHealthApp.Discussion
{
    public enum QuestionIntent
    {
        Clarification,
        Challenge,
        ExampleRequest,
        CounterArgument,
        EvidenceRequest,
        PracticalApplication,
        Consequence,
        Comparison,
        PersonalOpinion,
        AlternativeSolution,
        EthicalQuestion,
        FollowUp,
        Summary,
        ScenarioQuestion
    }

    [Serializable]
    public class QuestionRecord
    {
        public string questionID;
        public string questionText;
        public int speakerID;
        public float timestamp;
        public string topic;
        public string targetUser;
        public bool answered;
        public string answerSummary;
        public QuestionIntent intent;
        public int difficultyLevel; // 1 to 5
    }

    public class GDQuestionHistory : MonoBehaviour
    {
        public List<QuestionRecord> questionHistory = new List<QuestionRecord>();
        public List<string> statementHistory = new List<string>();

        public void ClearHistory()
        {
            questionHistory.Clear();
            statementHistory.Clear();
        }

        public void AddQuestion(string text, int speakerId, string topic, QuestionIntent intent, int difficulty)
        {
            QuestionRecord record = new QuestionRecord
            {
                questionID = Guid.NewGuid().ToString().Substring(0, 8),
                questionText = text,
                speakerID = speakerId,
                timestamp = Time.time,
                topic = topic,
                targetUser = "Kevin",
                answered = false,
                answerSummary = "",
                intent = intent,
                difficultyLevel = Mathf.Clamp(difficulty, 1, 5)
            };
            questionHistory.Add(record);
        }

        public void AddStatement(string text)
        {
            if (!string.IsNullOrEmpty(text) && !statementHistory.Contains(text))
            {
                statementHistory.Add(text);
            }
        }

        public bool IsDuplicateOrSimilar(string candidateText, float similarityThreshold = 0.60f)
        {
            if (string.IsNullOrEmpty(candidateText)) return true;

            string cleanCandidate = CleanText(candidateText);

            // Check against questions
            foreach (var q in questionHistory)
            {
                string cleanQ = CleanText(q.questionText);
                float similarity = CalculateSimilarity(cleanCandidate, cleanQ);
                if (similarity >= similarityThreshold)
                {
                    return true;
                }
            }

            // Check against statements
            foreach (var s in statementHistory)
            {
                string cleanS = CleanText(s);
                float similarity = CalculateSimilarity(cleanCandidate, cleanS);
                if (similarity >= similarityThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            char[] arr = input.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray();
            return new string(arr).Trim();
        }

        private float CalculateSimilarity(string s1, string s2)
        {
            if (s1 == s2) return 1.0f;
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0f;

            string[] words1 = s1.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] words2 = s2.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words1.Length == 0 || words2.Length == 0) return 0.0f;

            HashSet<string> set1 = new HashSet<string>(words1);
            HashSet<string> set2 = new HashSet<string>(words2);

            int intersection = set1.Intersect(set2).Count();
            int union = set1.Union(set2).Count();

            float jaccard = union > 0 ? (float)intersection / union : 0f;

            // Also check Levenshtein distance ratio for short phrases
            int levDistance = ComputeLevenshteinDistance(s1, s2);
            int maxLen = Mathf.Max(s1.Length, s2.Length);
            float levRatio = maxLen > 0 ? 1.0f - ((float)levDistance / maxLen) : 0f;

            return Mathf.Max(jaccard, levRatio);
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Mathf.Min(
                        Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}
