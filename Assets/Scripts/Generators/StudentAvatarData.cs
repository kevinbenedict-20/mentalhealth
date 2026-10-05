using UnityEngine;
using TMPro;

namespace MentalHealthApp.Generators
{
    [System.Serializable]
    public class StudentAvatarData
    {
        public string studentName;
        public string studentId;
        public bool isAssessedStudent;
        public GameObject avatarRoot;
        public Transform headTransform;
        public TextMeshPro speechBubbleText;
        public GameObject speechBubbleObj;
        public Color shirtColor;
    }
}
