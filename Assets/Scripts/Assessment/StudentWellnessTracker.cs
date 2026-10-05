using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace MentalHealthApp.Assessment
{
    [Serializable]
    public class WellnessSessionData
    {
        public string rawStudentId;
        public string anonymizedStudentId;
        public DateTime timestamp;
        
        // Pre-Session Metrics
        public int preSessionStressRating;      // 1 (Low) to 10 (High)
        public int preSessionConfidenceRating;  // 1 (Low) to 10 (High)

        // In-Session Metrics
        public int responseCount;
        public float totalResponseConfidence;
        public int peerInteractionsObserved;

        // Post-Session Metrics
        public int postSessionMoodRating;       // 1 (Low) to 10 (High)
        public int postSessionConfidenceRating; // 1 (Low) to 10 (High)

        // Computed Non-Clinical Indices
        public float focusScore;
        public float communicationEaseScore;
        public float socialEngagementScore;
        public float overallWellnessScore;      // 0 to 100

        public List<string> supportiveTips = new List<string>();
    }

    public class StudentWellnessTracker : MonoBehaviour
    {
        public WellnessSessionData currentData = new WellnessSessionData();

        public void InitializeSession(string studentId, int preStress, int preConfidence)
        {
            currentData = new WellnessSessionData();
            currentData.rawStudentId = string.IsNullOrEmpty(studentId) ? "STUDENT_ANON_01" : studentId;
            currentData.anonymizedStudentId = AnonymizeId(currentData.rawStudentId);
            currentData.timestamp = DateTime.Now;
            currentData.preSessionStressRating = Mathf.Clamp(preStress, 1, 10);
            currentData.preSessionConfidenceRating = Mathf.Clamp(preConfidence, 1, 10);
        }

        public void RecordPeerTurn()
        {
            currentData.peerInteractionsObserved++;
        }

        public void RecordAssessedStudentResponse(string text, int confidenceRating)
        {
            currentData.responseCount++;
            currentData.totalResponseConfidence += confidenceRating;
        }

        public void FinalizeSession(int postMood, int postConfidence)
        {
            currentData.postSessionMoodRating = Mathf.Clamp(postMood, 1, 10);
            currentData.postSessionConfidenceRating = Mathf.Clamp(postConfidence, 1, 10);

            CalculateWellnessScores();
            GenerateSupportiveFeedback();
        }

        private void CalculateWellnessScores()
        {
            // Focus Score based on response participation and engagement balance
            currentData.focusScore = Mathf.Clamp(75f + (currentData.responseCount * 8f), 60f, 98f);

            // Communication Ease Score
            float avgConfidence = currentData.responseCount > 0 ? (currentData.totalResponseConfidence / currentData.responseCount) : 5f;
            currentData.communicationEaseScore = Mathf.Clamp((avgConfidence / 10f) * 100f, 50f, 100f);

            // Social Engagement Score
            currentData.socialEngagementScore = Mathf.Clamp((currentData.peerInteractionsObserved * 7f) + (currentData.responseCount * 12f), 55f, 95f);

            // Overall Session Wellness Index (Weighted Composite)
            // Reduced stress (10 - stress) + post mood + confidence + engagement
            float stressDelta = (10 - currentData.preSessionStressRating) * 5f;
            float moodBonus = currentData.postSessionMoodRating * 4f;
            float confBonus = currentData.postSessionConfidenceRating * 3f;

            currentData.overallWellnessScore = Mathf.Clamp(
                (currentData.focusScore * 0.25f) + 
                (currentData.communicationEaseScore * 0.25f) + 
                (currentData.socialEngagementScore * 0.25f) + 
                (stressDelta + moodBonus + confBonus) * 0.25f, 
                0f, 100f
            );
        }

        private void GenerateSupportiveFeedback()
        {
            currentData.supportiveTips.Clear();

            currentData.supportiveTips.Add("Great job actively participating in today's group discussion session!");
            
            if (currentData.communicationEaseScore >= 75f)
            {
                currentData.supportiveTips.Add("Your confidence in expressing your thoughts is a valuable strength in team settings.");
            }
            else
            {
                currentData.supportiveTips.Add("Remember that sharing even brief thoughts builds team trust over time.");
            }

            if (currentData.preSessionStressRating > 6)
            {
                currentData.supportiveTips.Add("Tip: Try 4-7-8 rhythmic breathing before group presentations to steady your focus.");
            }

            currentData.supportiveTips.Add("Continuing to engage in peer discussions boosts overall academic clarity and resilience.");
        }

        public string ExportCSV()
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("AnonymizedID,Timestamp,PreStress,PreConfidence,ResponseCount,AvgConfidence,PeerTurns,PostMood,PostConfidence,FocusScore,CommEaseScore,SocialEngageScore,OverallWellnessIndex");
            
            float avgConf = currentData.responseCount > 0 ? (currentData.totalResponseConfidence / currentData.responseCount) : 0f;
            
            csv.AppendLine(string.Format("{0},{1},{2},{3},{4},{5:F1},{6},{7},{8},{9:F1},{10:F1},{11:F1},{12:F1}",
                currentData.anonymizedStudentId,
                currentData.timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                currentData.preSessionStressRating,
                currentData.preSessionConfidenceRating,
                currentData.responseCount,
                avgConf,
                currentData.peerInteractionsObserved,
                currentData.postSessionMoodRating,
                currentData.postSessionConfidenceRating,
                currentData.focusScore,
                currentData.communicationEaseScore,
                currentData.socialEngagementScore,
                currentData.overallWellnessScore
            ));

            string folderPath = Path.Combine(Application.persistentDataPath, "WellnessExports");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = "SessionWellness_" + currentData.anonymizedStudentId.Substring(0, Mathf.Min(8, currentData.anonymizedStudentId.Length)) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
            string fullPath = Path.Combine(folderPath, fileName);

            File.WriteAllText(fullPath, csv.ToString());
            Debug.Log("Exported Session Wellness CSV to: " + fullPath);
            return fullPath;
        }

        private string AnonymizeId(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input + "_SALT_2026"));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(bytes[i].ToString("X2"));
                }
                return "ANON-" + sb.ToString();
            }
        }
    }
}
