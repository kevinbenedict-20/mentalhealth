using UnityEngine;
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
            "Maya (Discussion Leader)",   // Center Back (Purple Hoodie - Facilitator)
            "Alex (Assessed Student)",     // Foreground Left (Cream/Striped Sweater)
            "Karan (Peer)",                // Mid Left (Green Hoodie)
            "Arjun (Peer)",                // Mid Right (Dark Blue Hoodie + Glasses)
            "Priya (Peer)"                 // Foreground Right (Burgundy Sweater)
        };

        private Color[] shirtColors = new Color[]
        {
            new Color(0.52f, 0.32f, 0.72f), // Purple Hoodie (Maya - Center Back)
            new Color(0.88f, 0.82f, 0.68f), // Cream Striped Sweater (Alex - Foreground Left)
            new Color(0.25f, 0.48f, 0.35f), // Green Hoodie (Karan - Mid Left)
            new Color(0.18f, 0.32f, 0.58f), // Dark Blue Hoodie (Arjun - Mid Right)
            new Color(0.65f, 0.18f, 0.22f)  // Burgundy Sweater (Priya - Foreground Right)
        };

        private Color[] skinTones = new Color[]
        {
            new Color(0.88f, 0.72f, 0.58f),
            new Color(0.92f, 0.76f, 0.65f),
            new Color(0.75f, 0.55f, 0.4f),
            new Color(0.6f, 0.42f, 0.3f),
            new Color(0.85f, 0.68f, 0.55f)
        };

        private Color[] hairColors = new Color[]
        {
            new Color(0.08f, 0.08f, 0.08f), // Long black hair
            new Color(0.12f, 0.12f, 0.12f), // Dark hair bun
            new Color(0.15f, 0.12f, 0.1f),  // Curly dark hair
            new Color(0.1f, 0.1f, 0.1f),    // Dark hair + glasses
            new Color(0.35f, 0.18f, 0.12f)  // Long reddish-brown hair
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

                StudentAvatarData avatarData = BuildHumanoidAvatar(
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

        private StudentAvatarData BuildHumanoidAvatar(string name, string id, bool isAssessed, Vector3 pos, Quaternion rot, Color shirtCol, Color skinCol, Color hairCol, int index)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = rot;

            Material skinMat = MaterialHelper.CreateMaterial("SkinMat_" + index, skinCol, 0.0f, 0.6f);
            Material shirtMat = MaterialHelper.CreateMaterial("ShirtMat_" + index, shirtCol, 0.1f, 0.4f);
            Material hairMat = MaterialHelper.CreateMaterial("HairMat_" + index, hairCol, 0.05f, 0.3f);
            Material pantsMat = MaterialHelper.CreateMaterial("PantsMat_" + index, new Color(0.15f, 0.2f, 0.28f), 0.1f, 0.4f);
            Material eyeWhiteMat = MaterialHelper.CreateMaterial("EyeWhiteMat", Color.white, 0.1f, 0.9f);
            Material irisMat = MaterialHelper.CreateMaterial("IrisMat_" + index, new Color(0.12f, 0.22f, 0.35f), 0.4f, 0.8f);
            Material eyebrowMat = MaterialHelper.CreateMaterial("EyebrowMat_" + index, hairCol * 0.8f, 0.05f, 0.3f);
            Material lipMat = MaterialHelper.CreateMaterial("LipMat_" + index, skinCol * 0.82f + new Color(0.12f, 0.02f, 0.04f), 0.1f, 0.5f);

            // 1. Torso & Upper Body
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform);
            torso.transform.localPosition = new Vector3(0, 0.42f, 0);
            torso.transform.localScale = new Vector3(0.38f, 0.34f, 0.26f);
            torso.GetComponent<Renderer>().sharedMaterial = shirtMat;

            // Hoodie Collar & Drawstrings
            if (index == 0 || index == 2 || index == 3)
            {
                GameObject hood = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hood.name = "HoodieCollar";
                hood.transform.SetParent(torso.transform);
                hood.transform.localPosition = new Vector3(0, 0.65f, -0.38f);
                hood.transform.localScale = new Vector3(0.85f, 0.48f, 0.6f);
                hood.GetComponent<Renderer>().sharedMaterial = shirtMat;

                // Drawstrings
                for (int s = -1; s <= 1; s += 2)
                {
                    GameObject stringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stringObj.name = "Drawstring_" + s;
                    stringObj.transform.SetParent(torso.transform);
                    stringObj.transform.localPosition = new Vector3(s * 0.15f, 0.55f, 0.42f);
                    stringObj.transform.localScale = new Vector3(0.04f, 0.2f, 0.04f);
                    stringObj.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("StringMat", Color.white);
                }
            }
            else // Sweater Ribbed Collar
            {
                GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                collar.name = "SweaterCollar";
                collar.transform.SetParent(torso.transform);
                collar.transform.localPosition = new Vector3(0, 0.82f, 0);
                collar.transform.localScale = new Vector3(0.48f, 0.08f, 0.48f);
                collar.GetComponent<Renderer>().sharedMaterial = shirtMat;
            }

            // 2. Neck
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root.transform);
            neck.transform.localPosition = new Vector3(0, 0.73f, 0);
            neck.transform.localScale = new Vector3(0.12f, 0.09f, 0.12f);
            neck.GetComponent<Renderer>().sharedMaterial = skinMat;

            // 3. Head & Facial Geometry
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0, 0.96f, 0);
            head.transform.localScale = new Vector3(0.24f, 0.27f, 0.25f);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Jaw & Chin Contour
            GameObject chin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chin.name = "ChinJawContour";
            chin.transform.SetParent(head.transform);
            chin.transform.localPosition = new Vector3(0, -0.32f, 0.18f);
            chin.transform.localRotation = Quaternion.Euler(15f, 0, 0);
            chin.transform.localScale = new Vector3(0.48f, 0.32f, 0.42f);
            chin.GetComponent<Renderer>().sharedMaterial = skinMat;

            // 3D Nose Bridge & Tip
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "NoseBridge";
            nose.transform.SetParent(head.transform);
            nose.transform.localPosition = new Vector3(0, 0.02f, 0.48f);
            nose.transform.localRotation = Quaternion.Euler(-20f, 0, 0);
            nose.transform.localScale = new Vector3(0.12f, 0.22f, 0.14f);
            nose.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Mouth & Lips
            GameObject mouthObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouthObj.name = "Mouth";
            mouthObj.transform.SetParent(head.transform);
            mouthObj.transform.localPosition = new Vector3(0, -0.25f, 0.46f);
            mouthObj.transform.localScale = new Vector3(0.35f, 0.08f, 0.08f);
            mouthObj.GetComponent<Renderer>().sharedMaterial = lipMat;

            // Detailed Eyes (White Sockets, Irises, Pupils)
            Transform leftEyelidTr = null;
            Transform rightEyelidTr = null;

            for (int eyeSide = -1; eyeSide <= 1; eyeSide += 2)
            {
                GameObject eyeSocket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eyeSocket.name = "EyeSocket_" + eyeSide;
                eyeSocket.transform.SetParent(head.transform);
                eyeSocket.transform.localPosition = new Vector3(eyeSide * 0.24f, 0.12f, 0.43f);
                eyeSocket.transform.localScale = new Vector3(0.15f, 0.13f, 0.12f);
                eyeSocket.GetComponent<Renderer>().sharedMaterial = eyeWhiteMat;

                GameObject iris = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                iris.name = "Iris_" + eyeSide;
                iris.transform.SetParent(eyeSocket.transform);
                iris.transform.localPosition = new Vector3(0, 0, 0.42f);
                iris.transform.localScale = new Vector3(0.6f, 0.6f, 0.3f);
                iris.GetComponent<Renderer>().sharedMaterial = irisMat;

                // Eyelids for blinking
                GameObject eyelid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eyelid.name = "Eyelid_" + eyeSide;
                eyelid.transform.SetParent(eyeSocket.transform);
                eyelid.transform.localPosition = new Vector3(0, 0.1f, 0.45f);
                eyelid.transform.localScale = new Vector3(1.1f, 1.1f, 0.2f);
                eyelid.GetComponent<Renderer>().sharedMaterial = skinMat;
                eyelid.SetActive(false);

                if (eyeSide == -1) leftEyelidTr = eyelid.transform;
                else rightEyelidTr = eyelid.transform;
            }

            // 3D Eyebrows (Left & Right)
            GameObject leftEyebrowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftEyebrowObj.name = "LeftEyebrow";
            leftEyebrowObj.transform.SetParent(head.transform);
            leftEyebrowObj.transform.localPosition = new Vector3(-0.24f, 0.28f, 0.44f);
            leftEyebrowObj.transform.localRotation = Quaternion.Euler(0, 0, -5f);
            leftEyebrowObj.transform.localScale = new Vector3(0.24f, 0.045f, 0.06f);
            leftEyebrowObj.GetComponent<Renderer>().sharedMaterial = eyebrowMat;

            GameObject rightEyebrowObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightEyebrowObj.name = "RightEyebrow";
            rightEyebrowObj.transform.SetParent(head.transform);
            rightEyebrowObj.transform.localPosition = new Vector3(0.24f, 0.28f, 0.44f);
            rightEyebrowObj.transform.localRotation = Quaternion.Euler(0, 0, 5f);
            rightEyebrowObj.transform.localScale = new Vector3(0.24f, 0.045f, 0.06f);
            rightEyebrowObj.GetComponent<Renderer>().sharedMaterial = eyebrowMat;

            // Glasses for Seat 3 (Arjun)
            if (index == 3)
            {
                GameObject glasses = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glasses.name = "GlassesFrame";
                glasses.transform.SetParent(head.transform);
                glasses.transform.localPosition = new Vector3(0, 0.12f, 0.48f);
                glasses.transform.localScale = new Vector3(0.85f, 0.24f, 0.06f);
                glasses.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("GlassesMat", new Color(0.1f, 0.1f, 0.12f), 0.8f, 0.8f);
            }

            // Hair Styles
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(head.transform);

            if (index == 0 || index == 4) // Long layered hair (Maya & Priya)
            {
                hair.transform.localPosition = new Vector3(0, 0.22f, -0.05f);
                hair.transform.localScale = new Vector3(1.12f, 0.85f, 1.25f);

                GameObject longHairBack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                longHairBack.name = "LongHairBack";
                longHairBack.transform.SetParent(head.transform);
                longHairBack.transform.localPosition = new Vector3(0, -0.3f, -0.4f);
                longHairBack.transform.localScale = new Vector3(1.05f, 0.65f, 0.55f);
                longHairBack.GetComponent<Renderer>().sharedMaterial = hairMat;
            }
            else if (index == 1) // Hair bun (Alex)
            {
                hair.transform.localPosition = new Vector3(0, 0.22f, -0.05f);
                hair.transform.localScale = new Vector3(1.08f, 0.6f, 1.08f);

                GameObject bun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bun.name = "HairBun";
                bun.transform.SetParent(head.transform);
                bun.transform.localPosition = new Vector3(0, 0.58f, -0.35f);
                bun.transform.localScale = new Vector3(0.48f, 0.48f, 0.48f);
                bun.GetComponent<Renderer>().sharedMaterial = hairMat;
            }
            else // Short male hair
            {
                hair.transform.localPosition = new Vector3(0, 0.24f, -0.05f);
                hair.transform.localScale = new Vector3(1.08f, 0.62f, 1.08f);
            }
            hair.GetComponent<Renderer>().sharedMaterial = hairMat;

            // 4. Arms & Hands resting naturally on table
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject upperArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                upperArm.name = "UpperArm_" + side;
                upperArm.transform.SetParent(root.transform);
                upperArm.transform.localPosition = new Vector3(side * 0.23f, 0.45f, 0.05f);
                upperArm.transform.localRotation = Quaternion.Euler(25f, 0, side * 15f);
                upperArm.transform.localScale = new Vector3(0.085f, 0.18f, 0.085f);
                upperArm.GetComponent<Renderer>().sharedMaterial = shirtMat;

                GameObject forearm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                forearm.name = "Forearm_" + side;
                forearm.transform.SetParent(root.transform);
                forearm.transform.localPosition = new Vector3(side * 0.2f, 0.28f, 0.28f);
                forearm.transform.localRotation = Quaternion.Euler(75f, side * -10f, 0);
                forearm.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
                forearm.GetComponent<Renderer>().sharedMaterial = skinMat;

                GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hand.name = "Hand_" + side;
                hand.transform.SetParent(root.transform);
                hand.transform.localPosition = new Vector3(side * 0.18f, 0.26f, 0.42f);
                hand.transform.localScale = new Vector3(0.08f, 0.05f, 0.1f);
                hand.GetComponent<Renderer>().sharedMaterial = skinMat;
            }

            // 5. Speech Bubble UI
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
