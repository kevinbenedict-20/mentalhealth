using UnityEngine;
using System.Collections.Generic;
using TMPro;

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

                bool isAssessed = (i == 1); // Seat 1 is Assessed Student Alex

                StudentAvatarData avatarData = BuildPrimitiveAvatar(
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

        private StudentAvatarData BuildPrimitiveAvatar(string name, string id, bool isAssessed, Vector3 pos, Quaternion rot, Color shirtCol, Color skinCol, Color hairCol, int index)
        {
            GameObject root = new GameObject(name);
            root.transform.position = pos;
            root.transform.rotation = rot;

            Material skinMat = MaterialHelper.CreateMaterial("SkinMat_" + index, skinCol, 0.0f, 0.6f);
            Material shirtMat = MaterialHelper.CreateMaterial("ShirtMat_" + index, shirtCol, 0.1f, 0.4f);
            Material hairMat = MaterialHelper.CreateMaterial("HairMat_" + index, hairCol, 0.05f, 0.3f);
            Material pantsMat = MaterialHelper.CreateMaterial("PantsMat_" + index, new Color(0.15f, 0.2f, 0.28f), 0.1f, 0.4f);
            Material eyeMat = MaterialHelper.CreateMaterial("EyeMat", new Color(0.08f, 0.08f, 0.1f), 0.5f, 0.8f);

            // 1. Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform);
            torso.transform.localPosition = new Vector3(0, 0.42f, 0);
            torso.transform.localScale = new Vector3(0.38f, 0.32f, 0.26f);
            torso.GetComponent<Renderer>().sharedMaterial = shirtMat;

            // Hoodie detail for Seat 0, 2, 3
            if (index == 0 || index == 2 || index == 3)
            {
                GameObject hood = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hood.name = "HoodieCollar";
                hood.transform.SetParent(torso.transform);
                hood.transform.localPosition = new Vector3(0, 0.65f, -0.4f);
                hood.transform.localScale = new Vector3(0.85f, 0.5f, 0.6f);
                hood.GetComponent<Renderer>().sharedMaterial = shirtMat;
            }

            // 2. Neck
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Neck";
            neck.transform.SetParent(root.transform);
            neck.transform.localPosition = new Vector3(0, 0.72f, 0);
            neck.transform.localScale = new Vector3(0.11f, 0.08f, 0.11f);
            neck.GetComponent<Renderer>().sharedMaterial = skinMat;

            // 3. Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0, 0.94f, 0);
            head.transform.localScale = new Vector3(0.24f, 0.26f, 0.24f);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Eyes
            for (int eyeSide = -1; eyeSide <= 1; eyeSide += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye_" + eyeSide;
                eye.transform.SetParent(head.transform);
                eye.transform.localPosition = new Vector3(eyeSide * 0.25f, 0.1f, 0.42f);
                eye.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                eye.GetComponent<Renderer>().sharedMaterial = eyeMat;
            }

            // Glasses for Seat 3 (Arjun)
            if (index == 3)
            {
                GameObject glasses = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glasses.name = "GlassesFrame";
                glasses.transform.SetParent(head.transform);
                glasses.transform.localPosition = new Vector3(0, 0.1f, 0.45f);
                glasses.transform.localScale = new Vector3(0.85f, 0.25f, 0.08f);
                glasses.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("GlassesMat", new Color(0.1f, 0.1f, 0.12f), 0.8f, 0.8f);
            }

            // Hair Styles
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(head.transform);

            if (index == 0 || index == 4) // Long hair for female avatars (Maya & Priya)
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

            // 4. Arms with Gestures matching reference image
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

            // Special Open Gesture for Discussion Leader Maya (Seat 0)
            if (index == 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Transform fa = root.transform.Find("Forearm_" + side);
                    if (fa != null) fa.localRotation = Quaternion.Euler(45f, side * 25f, 0);
                    Transform h = root.transform.Find("Hand_" + side);
                    if (h != null) h.localPosition = new Vector3(side * 0.35f, 0.45f, 0.48f); // Open palm gesture
                }
            }
            // Hand on Chin gesture for Green Hoodie Karan (Seat 2)
            else if (index == 2)
            {
                Transform fa = root.transform.Find("Forearm_1");
                if (fa != null) fa.localRotation = Quaternion.Euler(115f, -30f, 0);
                Transform h = root.transform.Find("Hand_1");
                if (h != null) h.localPosition = new Vector3(0.05f, 0.72f, 0.2f);
            }

            // 5. Legs
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = "Leg_" + side;
                leg.transform.SetParent(root.transform);
                leg.transform.localPosition = new Vector3(side * 0.12f, 0.12f, 0.15f);
                leg.transform.localRotation = Quaternion.Euler(80f, 0, 0);
                leg.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
                leg.GetComponent<Renderer>().sharedMaterial = pantsMat;
            }

            // 6. Speech Bubble UI
            GameObject speechBubbleObj = new GameObject("SpeechBubble");
            speechBubbleObj.transform.SetParent(root.transform);
            speechBubbleObj.transform.localPosition = new Vector3(0, 1.45f, 0);
            speechBubbleObj.transform.localRotation = Quaternion.identity;

            Canvas canvas = speechBubbleObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform sRect = speechBubbleObj.GetComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(2.2f, 0.8f);
            sRect.localScale = Vector3.one * 0.8f;

            GameObject bubbleBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bubbleBg.name = "BubbleBg";
            bubbleBg.transform.SetParent(speechBubbleObj.transform, false);
            bubbleBg.transform.localPosition = Vector3.zero;
            bubbleBg.transform.localScale = new Vector3(2.2f, 0.8f, 1f);
            bubbleBg.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("SpeechBgMat", new Color(0.12f, 0.15f, 0.22f, 0.9f), 0.1f, 0.5f);

            GameObject textObj = new GameObject("BubbleText");
            textObj.transform.SetParent(speechBubbleObj.transform, false);
            TextMeshPro bubbleText = textObj.AddComponent<TextMeshPro>();
            bubbleText.text = "...";
            bubbleText.fontSize = 0.16f;
            bubbleText.alignment = TextAlignmentOptions.Center;
            bubbleText.color = Color.white;
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchoredPosition = Vector3.zero;
            tRect.sizeDelta = new Vector2(2.1f, 0.75f);

            speechBubbleObj.SetActive(false);

            StudentAvatarData data = new StudentAvatarData();
            data.studentName = name;
            data.studentId = id;
            data.isAssessedStudent = isAssessed;
            data.avatarRoot = root;
            data.headTransform = head.transform;
            data.speechBubbleObj = speechBubbleObj;
            data.speechBubbleText = bubbleText;
            data.shirtColor = shirtCol;

            return data;
        }
    }
}
