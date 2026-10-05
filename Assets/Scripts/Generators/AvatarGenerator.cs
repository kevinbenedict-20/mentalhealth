using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using MentalHealthApp.Discussion;

namespace MentalHealthApp.Generators
{
    public class AvatarGenerator : MonoBehaviour
    {
        [HideInInspector]
        public List<StudentAvatarData> generatedAvatars = new List<StudentAvatarData>();

        private string[] studentNames = new string[]
        {
            "Maya (Discussion Leader)",   // Seat 0 - Purple Hoodie
            "Alex (Assessed Student)",     // Seat 1 - Cream Sweater
            "Karan (Peer)",                // Seat 2 - Green Hoodie
            "Arjun (Peer)",                // Seat 3 - Dark Blue Hoodie + Glasses
            "Priya (Peer)"                 // Seat 4 - Burgundy Sweater
        };

        private Color[] shirtColors = new Color[]
        {
            new Color(0.48f, 0.28f, 0.68f), // Maya - Purple
            new Color(0.86f, 0.80f, 0.68f), // Alex - Cream
            new Color(0.22f, 0.45f, 0.32f), // Karan - Green
            new Color(0.16f, 0.28f, 0.52f), // Arjun - Dark Blue
            new Color(0.62f, 0.16f, 0.20f)  // Priya - Burgundy
        };

        private Color[] skinTones = new Color[]
        {
            new Color(0.88f, 0.72f, 0.58f), // Maya - Warm Beige
            new Color(0.92f, 0.78f, 0.66f), // Alex - Light Warm
            new Color(0.72f, 0.52f, 0.38f), // Karan - Medium Tan
            new Color(0.58f, 0.40f, 0.28f), // Arjun - Deep Warm
            new Color(0.84f, 0.66f, 0.52f)  // Priya - Olive Warm
        };

        private Color[] hairColors = new Color[]
        {
            new Color(0.08f, 0.08f, 0.08f), // Maya - Black
            new Color(0.14f, 0.12f, 0.10f), // Alex - Dark Brown
            new Color(0.18f, 0.14f, 0.10f), // Karan - Chestnut
            new Color(0.09f, 0.09f, 0.09f), // Arjun - Black
            new Color(0.32f, 0.16f, 0.10f)  // Priya - Auburn
        };

        public void GenerateAvatars(List<Transform> seatAnchors)
        {
            foreach (var a in generatedAvatars)
            {
                if (a.avatarRoot != null) DestroyImmediate(a.avatarRoot);
            }
            generatedAvatars.Clear();

            GameObject avatarGroup = new GameObject("StudentAvatars");
            avatarGroup.transform.SetParent(transform);

            for (int i = 0; i < seatAnchors.Count && i < studentNames.Length; i++)
            {
                Transform seat = seatAnchors[i];
                if (seat == null) continue;

                bool isAssessed = (i == 1);

                StudentAvatarData avatarData = BuildNormalHumanAvatar(
                    studentNames[i],
                    "STU_" + (1000 + i),
                    isAssessed,
                    seat.position,
                    seat.rotation,
                    shirtColors[i % shirtColors.Length],
                    skinTones[i % skinTones.Length],
                    hairColors[i % hairColors.Length],
                    i
                );

                avatarData.avatarRoot.transform.SetParent(avatarGroup.transform);
                generatedAvatars.Add(avatarData);
            }
        }

        private StudentAvatarData BuildNormalHumanAvatar(string name, string id, bool isAssessed, Vector3 pos, Quaternion rot, Color shirtCol, Color skinCol, Color hairCol, int index)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = rot;

            Material skinMat = MaterialHelper.CreateMaterial("HumanSkinMat_" + index, skinCol, 0.0f, 0.55f);
            Material shirtMat = MaterialHelper.CreateMaterial("ShirtMat_" + index, shirtCol, 0.1f, 0.45f);
            Material hairMat = MaterialHelper.CreateMaterial("HairMat_" + index, hairCol, 0.08f, 0.35f);
            Material pantsMat = MaterialHelper.CreateMaterial("PantsMat_" + index, new Color(0.14f, 0.18f, 0.25f), 0.1f, 0.4f);
            Material eyeScleraMat = MaterialHelper.CreateMaterial("EyeScleraMat", new Color(0.96f, 0.96f, 0.98f), 0.2f, 0.9f);
            Material irisMat = MaterialHelper.CreateMaterial("IrisMat_" + index, new Color(0.15f, 0.25f, 0.38f), 0.5f, 0.85f);
            Material pupilMat = MaterialHelper.CreateMaterial("PupilMat", new Color(0.04f, 0.04f, 0.04f), 0.8f, 0.9f);
            Material eyebrowMat = MaterialHelper.CreateMaterial("EyebrowMat_" + index, hairCol * 0.75f, 0.05f, 0.3f);
            Material lipMat = MaterialHelper.CreateMaterial("LipMat_" + index, skinCol * 0.8f + new Color(0.15f, 0.03f, 0.05f), 0.1f, 0.55f);

            // 1. Torso & Upper Body Structure
            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chest.name = "ChestTorso";
            chest.transform.SetParent(root.transform);
            chest.transform.localPosition = new Vector3(0, 0.44f, 0);
            chest.transform.localScale = new Vector3(0.36f, 0.32f, 0.24f);
            chest.GetComponent<Renderer>().sharedMaterial = shirtMat;

            // Hoodie or Sweater Collar Details
            if (index == 0 || index == 2 || index == 3) // Hoodies
            {
                GameObject hoodBack = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hoodBack.name = "HoodieBack";
                hoodBack.transform.SetParent(chest.transform);
                hoodBack.transform.localPosition = new Vector3(0, 0.62f, -0.36f);
                hoodBack.transform.localScale = new Vector3(0.82f, 0.45f, 0.58f);
                hoodBack.GetComponent<Renderer>().sharedMaterial = shirtMat;

                // Drawstrings
                for (int s = -1; s <= 1; s += 2)
                {
                    GameObject stringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stringObj.name = "Drawstring_" + s;
                    stringObj.transform.SetParent(chest.transform);
                    stringObj.transform.localPosition = new Vector3(s * 0.16f, 0.52f, 0.42f);
                    stringObj.transform.localScale = new Vector3(0.035f, 0.18f, 0.035f);
                    stringObj.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("StringMat", Color.white);
                }
            }
            else // Ribbed Sweater Collar
            {
                GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                collar.name = "SweaterCollar";
                collar.transform.SetParent(chest.transform);
                collar.transform.localPosition = new Vector3(0, 0.82f, 0);
                collar.transform.localScale = new Vector3(0.46f, 0.08f, 0.46f);
                collar.GetComponent<Renderer>().sharedMaterial = shirtMat;
            }

            // 2. Anatomical Neck & Collarbone
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root.transform);
            neck.transform.localPosition = new Vector3(0, 0.74f, 0);
            neck.transform.localScale = new Vector3(0.11f, 0.09f, 0.11f);
            neck.GetComponent<Renderer>().sharedMaterial = skinMat;

            // 3. Human Head Geometry (Cranium + Jaw/Chin + Ears)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0, 0.96f, 0);
            head.transform.localScale = new Vector3(0.22f, 0.26f, 0.23f);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Chin & Jaw Contour
            GameObject chin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            chin.name = "ChinContour";
            chin.transform.SetParent(head.transform);
            chin.transform.localPosition = new Vector3(0, -0.32f, 0.16f);
            chin.transform.localScale = new Vector3(0.44f, 0.32f, 0.42f);
            chin.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Human Ears (Left & Right)
            for (int eSide = -1; eSide <= 1; eSide += 2)
            {
                GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ear.name = "Ear_" + eSide;
                ear.transform.SetParent(head.transform);
                ear.transform.localPosition = new Vector3(eSide * 0.48f, 0.02f, -0.05f);
                ear.transform.localRotation = Quaternion.Euler(15f, 0, eSide * 20f);
                ear.transform.localScale = new Vector3(0.08f, 0.16f, 0.12f);
                ear.GetComponent<Renderer>().sharedMaterial = skinMat;
            }

            // Human Nose Bridge & Tip
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "NoseBridge";
            nose.transform.SetParent(head.transform);
            nose.transform.localPosition = new Vector3(0, 0.02f, 0.46f);
            nose.transform.localRotation = Quaternion.Euler(-18f, 0, 0);
            nose.transform.localScale = new Vector3(0.11f, 0.2f, 0.12f);
            nose.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Human Mouth & Lips
            GameObject mouthObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouthObj.name = "Mouth";
            mouthObj.transform.SetParent(head.transform);
            mouthObj.transform.localPosition = new Vector3(0, -0.24f, 0.45f);
            mouthObj.transform.localScale = new Vector3(0.32f, 0.07f, 0.07f);
            mouthObj.GetComponent<Renderer>().sharedMaterial = lipMat;

            // Anatomical Eyes (White Sclera, Colored Iris, Black Pupil)
            Transform leftEyelidTr = null;
            Transform rightEyelidTr = null;

            for (int eyeSide = -1; eyeSide <= 1; eyeSide += 2)
            {
                GameObject eyeSocket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eyeSocket.name = "EyeSocket_" + eyeSide;
                eyeSocket.transform.SetParent(head.transform);
                eyeSocket.transform.localPosition = new Vector3(eyeSide * 0.23f, 0.11f, 0.42f);
                eyeSocket.transform.localScale = new Vector3(0.14f, 0.12f, 0.11f);
                eyeSocket.GetComponent<Renderer>().sharedMaterial = eyeScleraMat;

                GameObject iris = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                iris.name = "Iris_" + eyeSide;
                iris.transform.SetParent(eyeSocket.transform);
                iris.transform.localPosition = new Vector3(0, 0, 0.42f);
                iris.transform.localScale = new Vector3(0.58f, 0.58f, 0.25f);
                iris.GetComponent<Renderer>().sharedMaterial = irisMat;

                GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pupil.name = "Pupil_" + eyeSide;
                pupil.transform.SetParent(iris.transform);
                pupil.transform.localPosition = new Vector3(0, 0, 0.45f);
                pupil.transform.localScale = new Vector3(0.5f, 0.5f, 0.3f);
                pupil.GetComponent<Renderer>().sharedMaterial = pupilMat;

                // Eyelids for blinking
                GameObject eyelid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eyelid.name = "Eyelid_" + eyeSide;
                eyelid.transform.SetParent(eyeSocket.transform);
                eyelid.transform.localPosition = new Vector3(0, 0.08f, 0.48f);
                eyelid.transform.localScale = new Vector3(1.1f, 1.1f, 0.2f);
                eyelid.GetComponent<Renderer>().sharedMaterial = skinMat;
                eyelid.SetActive(false);

                if (eyeSide == -1) leftEyelidTr = eyelid.transform;
                else rightEyelidTr = eyelid.transform;
            }

            // Human 3D Eyebrows
            GameObject leftEyebrowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftEyebrowObj.name = "LeftEyebrow";
            leftEyebrowObj.transform.SetParent(head.transform);
            leftEyebrowObj.transform.localPosition = new Vector3(-0.23f, 0.26f, 0.43f);
            leftEyebrowObj.transform.localRotation = Quaternion.Euler(0, 0, -5f);
            leftEyebrowObj.transform.localScale = new Vector3(0.22f, 0.04f, 0.05f);
            leftEyebrowObj.GetComponent<Renderer>().sharedMaterial = eyebrowMat;

            GameObject rightEyebrowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightEyebrowObj.name = "RightEyebrow";
            rightEyebrowObj.transform.SetParent(head.transform);
            rightEyebrowObj.transform.localPosition = new Vector3(0.23f, 0.26f, 0.43f);
            rightEyebrowObj.transform.localRotation = Quaternion.Euler(0, 0, 5f);
            rightEyebrowObj.transform.localScale = new Vector3(0.22f, 0.04f, 0.05f);
            rightEyebrowObj.GetComponent<Renderer>().sharedMaterial = eyebrowMat;

            // Glasses for Seat 3 (Arjun)
            if (index == 3)
            {
                GameObject glasses = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glasses.name = "GlassesFrame";
                glasses.transform.SetParent(head.transform);
                glasses.transform.localPosition = new Vector3(0, 0.11f, 0.47f);
                glasses.transform.localScale = new Vector3(0.82f, 0.22f, 0.05f);
                glasses.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("GlassesMat", new Color(0.1f, 0.1f, 0.12f), 0.8f, 0.8f);
            }

            // Hairstyles
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(head.transform);

            if (index == 0 || index == 4) // Maya & Priya - Long layered hair
            {
                hair.transform.localPosition = new Vector3(0, 0.22f, -0.05f);
                hair.transform.localScale = new Vector3(1.12f, 0.85f, 1.22f);

                GameObject longHairBack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                longHairBack.name = "LongHairBack";
                longHairBack.transform.SetParent(head.transform);
                longHairBack.transform.localPosition = new Vector3(0, -0.3f, -0.38f);
                longHairBack.transform.localScale = new Vector3(1.05f, 0.65f, 0.52f);
                longHairBack.GetComponent<Renderer>().sharedMaterial = hairMat;
            }
            else if (index == 1) // Alex - Top Bun
            {
                hair.transform.localPosition = new Vector3(0, 0.22f, -0.05f);
                hair.transform.localScale = new Vector3(1.08f, 0.6f, 1.08f);

                GameObject bun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bun.name = "HairBun";
                bun.transform.SetParent(head.transform);
                bun.transform.localPosition = new Vector3(0, 0.56f, -0.32f);
                bun.transform.localScale = new Vector3(0.46f, 0.46f, 0.46f);
                bun.GetComponent<Renderer>().sharedMaterial = hairMat;
            }
            else // Karan & Arjun - Short layered male hair
            {
                hair.transform.localPosition = new Vector3(0, 0.24f, -0.05f);
                hair.transform.localScale = new Vector3(1.06f, 0.6f, 1.06f);
            }
            hair.GetComponent<Renderer>().sharedMaterial = hairMat;

            // 4. Arms & Hands with 5 Individual Fingers
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject upperArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                upperArm.name = "UpperArm_" + side;
                upperArm.transform.SetParent(root.transform);
                upperArm.transform.localPosition = new Vector3(side * 0.22f, 0.45f, 0.05f);
                upperArm.transform.localRotation = Quaternion.Euler(25f, 0, side * 15f);
                upperArm.transform.localScale = new Vector3(0.08f, 0.18f, 0.08f);
                upperArm.GetComponent<Renderer>().sharedMaterial = shirtMat;

                GameObject forearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                forearm.name = "Forearm_" + side;
                forearm.transform.SetParent(root.transform);
                forearm.transform.localPosition = new Vector3(side * 0.19f, 0.28f, 0.28f);
                forearm.transform.localRotation = Quaternion.Euler(75f, side * -10f, 0);
                forearm.transform.localScale = new Vector3(0.075f, 0.16f, 0.075f);
                forearm.GetComponent<Renderer>().sharedMaterial = skinMat;

                // Palm
                GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                palm.name = "Palm_" + side;
                palm.transform.SetParent(root.transform);
                palm.transform.localPosition = new Vector3(side * 0.18f, 0.26f, 0.42f);
                palm.transform.localScale = new Vector3(0.075f, 0.025f, 0.09f);
                palm.GetComponent<Renderer>().sharedMaterial = skinMat;

                // 5 Individual Fingers
                for (int f = -2; f <= 2; f++)
                {
                    GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    finger.name = "Finger_" + side + "_" + f;
                    finger.transform.SetParent(palm.transform);
                    finger.transform.localPosition = new Vector3(f * 0.22f, 0, 0.55f);
                    finger.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
                    finger.GetComponent<Renderer>().sharedMaterial = skinMat;
                }
            }

            // 5. Speech Bubble UI (Hidden overhead by default)
            GameObject speechBubbleObj = new GameObject("SpeechBubble");
            speechBubbleObj.transform.SetParent(root.transform);
            speechBubbleObj.transform.localPosition = new Vector3(0, 1.45f, 0);

            Canvas bubbleCanvas = speechBubbleObj.AddComponent<Canvas>();
            bubbleCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform bRect = speechBubbleObj.GetComponent<RectTransform>();
            bRect.sizeDelta = new Vector2(2.4f, 0.85f);

            GameObject bubbleBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bubbleBg.name = "BubbleBg";
            bubbleBg.transform.SetParent(speechBubbleObj.transform, false);
            bubbleBg.transform.localScale = new Vector3(2.4f, 0.85f, 1f);
            bubbleBg.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("BubbleBgMat", new Color(0.08f, 0.12f, 0.18f, 0.92f), 0f, 0.9f);

            GameObject tObj = new GameObject("BubbleText");
            tObj.transform.SetParent(speechBubbleObj.transform, false);
            tObj.transform.localPosition = new Vector3(0, 0, -0.02f);
            TextMeshPro textComp = tObj.AddComponent<TextMeshPro>();
            textComp.fontSize = 0.17f;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.color = Color.white;
            textComp.fontStyle = FontStyles.Bold;
            RectTransform tRect = tObj.GetComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(2.2f, 0.75f);

            speechBubbleObj.SetActive(false);

            // 6. Attach Expression Controller Component
            AvatarExpressionController exprCtrl = root.AddComponent<AvatarExpressionController>();
            exprCtrl.headTransform = head.transform;
            exprCtrl.leftEyebrow = leftEyebrowObj.transform;
            exprCtrl.rightEyebrow = rightEyebrowObj.transform;
            exprCtrl.mouthTransform = mouthObj.transform;
            exprCtrl.leftEyelid = leftEyelidTr;
            exprCtrl.rightEyelid = rightEyelidTr;

            return new StudentAvatarData
            {
                studentName = name,
                studentId = id,
                isAssessedStudent = isAssessed,
                avatarRoot = root,
                headTransform = head.transform,
                speechBubbleObj = speechBubbleObj,
                speechBubbleText = textComp
            };
        }
    }
}
