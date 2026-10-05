using UnityEngine;
using System.Collections;

namespace MentalHealthApp.Discussion
{
    public enum AvatarExpression
    {
        Neutral,
        Speaking,
        Empathetic,
        Thinking,
        Surprised
    }

    public class AvatarExpressionController : MonoBehaviour
    {
        [Header("Expression State")]
        public AvatarExpression currentExpression = AvatarExpression.Neutral;

        [Header("Facial Component References")]
        public Transform headTransform;
        public Transform leftEyebrow;
        public Transform rightEyebrow;
        public Transform mouthTransform;
        public Transform upperLip;
        public Transform lowerLip;
        public Transform leftEyelid;
        public Transform rightEyelid;

        private Vector3 initialLeftEyebrowPos;
        private Vector3 initialRightEyebrowPos;
        private Vector3 initialMouthScale;

        private float blinkTimer = 0f;
        private float nextBlinkTime = 3f;
        private bool isBlinking = false;

        private float speechTimer = 0f;

        private void Start()
        {
            if (leftEyebrow != null) initialLeftEyebrowPos = leftEyebrow.localPosition;
            if (rightEyebrow != null) initialRightEyebrowPos = rightEyebrow.localPosition;
            if (mouthTransform != null) initialMouthScale = mouthTransform.localScale;
            nextBlinkTime = Random.Range(2.5f, 4.5f);
        }

        private void Update()
        {
            HandleProceduralBlink();
            HandleIdleBreathingAndMicroGestures();

            if (currentExpression == AvatarExpression.Speaking)
            {
                HandleSpeechLipSync();
            }
        }

        public void SetExpression(AvatarExpression expression)
        {
            currentExpression = expression;
            ApplyExpressionTargets();
        }

        private void ApplyExpressionTargets()
        {
            switch (currentExpression)
            {
                case AvatarExpression.Neutral:
                    ResetEyebrows();
                    ResetMouth();
                    break;

                case AvatarExpression.Speaking:
                    if (leftEyebrow != null) leftEyebrow.localPosition = initialLeftEyebrowPos + new Vector3(0, 0.02f, 0);
                    if (rightEyebrow != null) rightEyebrow.localPosition = initialRightEyebrowPos + new Vector3(0, 0.02f, 0);
                    break;

                case AvatarExpression.Empathetic:
                    // Warm smile, slightly raised inward eyebrows
                    if (leftEyebrow != null) leftEyebrow.localPosition = initialLeftEyebrowPos + new Vector3(0, 0.015f, 0);
                    if (rightEyebrow != null) rightEyebrow.localPosition = initialRightEyebrowPos + new Vector3(0, 0.015f, 0);
                    if (mouthTransform != null) mouthTransform.localScale = new Vector3(initialMouthScale.x * 1.25f, initialMouthScale.y * 0.7f, initialMouthScale.z);
                    break;

                case AvatarExpression.Thinking:
                    // Furrowed brow, slight head tilt
                    if (leftEyebrow != null) leftEyebrow.localPosition = initialLeftEyebrowPos + new Vector3(0, -0.01f, 0);
                    if (rightEyebrow != null) rightEyebrow.localPosition = initialRightEyebrowPos + new Vector3(0, 0.015f, 0);
                    if (mouthTransform != null) mouthTransform.localScale = new Vector3(initialMouthScale.x * 0.85f, initialMouthScale.y * 0.8f, initialMouthScale.z);
                    break;

                case AvatarExpression.Surprised:
                    // High eyebrows, open mouth
                    if (leftEyebrow != null) leftEyebrow.localPosition = initialLeftEyebrowPos + new Vector3(0, 0.035f, 0);
                    if (rightEyebrow != null) rightEyebrow.localPosition = initialRightEyebrowPos + new Vector3(0, 0.035f, 0);
                    if (mouthTransform != null) mouthTransform.localScale = new Vector3(initialMouthScale.x * 0.9f, initialMouthScale.y * 1.6f, initialMouthScale.z);
                    break;
            }
        }

        private void ResetEyebrows()
        {
            if (leftEyebrow != null) leftEyebrow.localPosition = initialLeftEyebrowPos;
            if (rightEyebrow != null) rightEyebrow.localPosition = initialRightEyebrowPos;
        }

        private void ResetMouth()
        {
            if (mouthTransform != null) mouthTransform.localScale = initialMouthScale;
        }

        private void HandleSpeechLipSync()
        {
            if (mouthTransform == null) return;

            speechTimer += Time.deltaTime * 12f;
            float mouthOpenFactor = 0.5f + Mathf.Sin(speechTimer) * 0.45f + Mathf.Sin(speechTimer * 2.3f) * 0.2f;
            mouthOpenFactor = Mathf.Clamp(mouthOpenFactor, 0.2f, 1.4f);

            mouthTransform.localScale = new Vector3(initialMouthScale.x * 1.1f, initialMouthScale.y * mouthOpenFactor, initialMouthScale.z);

            // Subtle head nodding gesture while speaking
            if (headTransform != null)
            {
                float nodAngle = Mathf.Sin(speechTimer * 0.4f) * 2.5f;
                headTransform.localRotation = Quaternion.Euler(nodAngle, headTransform.localRotation.eulerAngles.y, headTransform.localRotation.eulerAngles.z);
            }
        }

        private void HandleProceduralBlink()
        {
            if (leftEyelid == null && rightEyelid == null) return;

            blinkTimer += Time.deltaTime;
            if (blinkTimer >= nextBlinkTime)
            {
                StartCoroutine(BlinkRoutine());
                blinkTimer = 0f;
                nextBlinkTime = Random.Range(2.5f, 5.0f);
            }
        }

        private IEnumerator BlinkRoutine()
        {
            isBlinking = true;
            if (leftEyelid != null) leftEyelid.gameObject.SetActive(true);
            if (rightEyelid != null) rightEyelid.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.12f);

            if (leftEyelid != null) leftEyelid.gameObject.SetActive(false);
            if (rightEyelid != null) rightEyelid.gameObject.SetActive(false);
            isBlinking = false;
        }

        private void HandleIdleBreathingAndMicroGestures()
        {
            if (headTransform == null || currentExpression == AvatarExpression.Speaking) return;

            // Subtle micro breathing tilt
            float breatheSway = Mathf.Sin(Time.time * 1.5f) * 0.8f;
            headTransform.localRotation = Quaternion.Euler(breatheSway, headTransform.localRotation.eulerAngles.y, headTransform.localRotation.eulerAngles.z);
        }
    }
}
