using UnityEngine;
using TMPro;

namespace MentalHealthApp.Generators
{
    public class ModernMeetingRoomGenerator : MonoBehaviour
    {
        [Header("Room Dimensions")]
        public float roomWidth = 9.5f;
        public float roomLength = 8.5f;
        public float roomHeight = 3.6f;

        [Header("Generated References")]
        public GameObject roomParent;
        public GameObject presentationScreenObj;
        public TextMeshPro presentationTitleText;
        public TextMeshPro presentationBodyText;
        public TextMeshPro presentationStatusText;

        public void GenerateRoom()
        {
            if (roomParent != null)
            {
                DestroyImmediate(roomParent);
            }

            roomParent = new GameObject("ModernMeetingRoom");
            roomParent.transform.SetParent(transform);

            // High-End Materials
            Material floorMat = MaterialHelper.CreateMaterial("LightOakFloorMat", new Color(0.72f, 0.68f, 0.64f), 0.08f, 0.35f);
            Material wallMat = MaterialHelper.CreateMaterial("ModernNeutralWallMat", new Color(0.88f, 0.9f, 0.92f), 0.05f, 0.85f);
            Material woodSlatMat = MaterialHelper.CreateMaterial("OakWoodSlatMat", new Color(0.62f, 0.42f, 0.25f), 0.15f, 0.4f);
            Material ceilingMat = MaterialHelper.CreateMaterial("AcousticCeilingMat", new Color(0.95f, 0.96f, 0.97f), 0.05f, 0.9f);
            Material bezelMat = MaterialHelper.CreateMaterial("ScreenBezelMat", new Color(0.12f, 0.14f, 0.18f), 0.6f, 0.6f);
            Material ledGlowMat = MaterialHelper.CreateMaterial("LedStripMat", new Color(0.4f, 0.85f, 1.0f), 0.9f, 0.2f);
            Material plantLeafMat = MaterialHelper.CreateMaterial("PlantLeafMat", new Color(0.18f, 0.45f, 0.22f), 0.1f, 0.5f);
            Material potMat = MaterialHelper.CreateMaterial("CeramicPotMat", new Color(0.92f, 0.92f, 0.95f), 0.3f, 0.7f);

            // 1. Polished Wood Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(roomParent.transform);
            floor.transform.position = new Vector3(0, -0.1f, 0);
            floor.transform.localScale = new Vector3(roomWidth, 0.2f, roomLength);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Baseboard Trim
            GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseboard.name = "BaseboardTrim";
            baseboard.transform.SetParent(roomParent.transform);
            baseboard.transform.position = new Vector3(0, 0.08f, roomLength / 2f - 0.05f);
            baseboard.transform.localScale = new Vector3(roomWidth, 0.16f, 0.04f);
            baseboard.GetComponent<Renderer>().sharedMaterial = bezelMat;

            // 2. Acoustic Ceiling & Recessed LED Lights
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(roomParent.transform);
            ceiling.transform.position = new Vector3(0, roomHeight + 0.1f, 0);
            ceiling.transform.localScale = new Vector3(roomWidth, 0.2f, roomLength);
            ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;

            for (int x = -2; x <= 2; x += 2)
            {
                for (int z = -2; z <= 2; z += 2)
                {
                    GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    fixture.name = "RecessedLightFixture";
                    fixture.transform.SetParent(roomParent.transform);
                    fixture.transform.position = new Vector3(x * 1.5f, roomHeight - 0.05f, z * 1.5f);
                    fixture.transform.localScale = new Vector3(0.4f, 0.04f, 0.4f);
                    fixture.GetComponent<Renderer>().sharedMaterial = ledGlowMat;

                    GameObject pLight = new GameObject("CeilingSpot");
                    pLight.transform.SetParent(fixture.transform, false);
                    pLight.transform.localPosition = Vector3.zero;
                    Light l = pLight.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.intensity = 1.75f;
                    l.range = 7.5f;
                    l.color = new Color(0.98f, 0.96f, 0.92f);
                }
            }

            // 3. Back Wall with Architectural Wood Slat Accent Feature
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.SetParent(roomParent.transform);
            backWall.transform.position = new Vector3(0, roomHeight / 2f, roomLength / 2f);
            backWall.transform.localScale = new Vector3(roomWidth, roomHeight, 0.2f);
            backWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Acoustic Wood Slats behind Board
            GameObject slatGroup = new GameObject("WoodSlatFeatureWall");
            slatGroup.transform.SetParent(roomParent.transform);
            float startX = -2.6f;
            for (int i = 0; i < 28; i++)
            {
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = "WoodSlat_" + i;
                slat.transform.SetParent(slatGroup.transform);
                slat.transform.position = new Vector3(startX + i * 0.18f, roomHeight / 2f, roomLength / 2f - 0.12f);
                slat.transform.localScale = new Vector3(0.08f, roomHeight - 0.4f, 0.05f);
                slat.GetComponent<Renderer>().sharedMaterial = woodSlatMat;
            }

            // Glowing LED Accent Strip behind Wood Feature Wall
            GameObject ledStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledStrip.name = "AccentLedStrip";
            ledStrip.transform.SetParent(roomParent.transform);
            ledStrip.transform.position = new Vector3(0, roomHeight - 0.25f, roomLength / 2f - 0.13f);
            ledStrip.transform.localScale = new Vector3(5.2f, 0.05f, 0.03f);
            ledStrip.GetComponent<Renderer>().sharedMaterial = ledGlowMat;

            // 4. Whiteboard on Back Wall
            BuildMentalHealthWhiteboard(roomParent.transform, bezelMat);

            // 5. Left Bookshelf & Indoor Greenery Planters
            BuildBookshelf(roomParent.transform, new Vector3(-roomWidth / 2f + 0.6f, 0, roomLength / 2f - 1.2f), woodSlatMat);
            BuildPottedPlant(roomParent.transform, new Vector3(-roomWidth / 2f + 0.8f, 0, roomLength / 2f - 2.8f), potMat, plantLeafMat);
            BuildPottedPlant(roomParent.transform, new Vector3(roomWidth / 2f - 0.8f, 0, roomLength / 2f - 0.8f), potMat, plantLeafMat);

            // 6. Solid Left Wall & Floor-to-Ceiling Glass Window Wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(roomParent.transform);
            leftWall.transform.position = new Vector3(-roomWidth / 2f, roomHeight / 2f, 0);
            leftWall.transform.localScale = new Vector3(0.2f, roomHeight, roomLength);
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            BuildRightGlassWindowWall(roomParent.transform, bezelMat);
        }

        private void BuildMentalHealthWhiteboard(Transform parent, Material bezelMat)
        {
            presentationScreenObj = new GameObject("MentalHealthWhiteboard");
            presentationScreenObj.transform.SetParent(parent);
            presentationScreenObj.transform.position = new Vector3(-0.1f, 2.25f, roomLength / 2f - 0.16f);

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "WhiteboardFrame";
            frame.transform.SetParent(presentationScreenObj.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(4.0f, 2.1f, 0.05f);
            frame.GetComponent<Renderer>().sharedMaterial = bezelMat;

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "WhiteboardSurface";
            surface.transform.SetParent(presentationScreenObj.transform);
            surface.transform.localPosition = new Vector3(0, 0, -0.03f);
            surface.transform.localRotation = Quaternion.Euler(0, 180, 0);
            surface.transform.localScale = new Vector3(3.88f, 1.98f, 1f);
            surface.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("WhiteboardMat", Color.white, 0.0f, 0.9f);

            GameObject canvasObj = new GameObject("WhiteboardCanvas");
            canvasObj.transform.SetParent(presentationScreenObj.transform);
            canvasObj.transform.localPosition = new Vector3(0, 0, -0.035f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(3.88f, 1.98f);

            GameObject titleObj = new GameObject("WhiteboardTitle");
            titleObj.transform.SetParent(canvasObj.transform, false);
            presentationTitleText = titleObj.AddComponent<TextMeshPro>();
            presentationTitleText.text = "Student Wellness Discussion";
            presentationTitleText.fontSize = 0.3f;
            presentationTitleText.alignment = TextAlignmentOptions.Left;
            presentationTitleText.color = new Color(0.12f, 0.22f, 0.42f);
            presentationTitleText.fontStyle = FontStyles.Bold;
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchoredPosition = new Vector3(-0.75f, 0.72f, 0);
            tRect.sizeDelta = new Vector2(2.2f, 0.4f);

            GameObject bodyObj = new GameObject("WhiteboardContent");
            bodyObj.transform.SetParent(canvasObj.transform, false);
            presentationBodyText = bodyObj.AddComponent<TextMeshPro>();
            presentationBodyText.text = "Session Focus Topics:\n• Managing Academic Workload & Exam Stress\n• Team Collaboration & Communication Ease\n• Personal Well-being, Boundaries & Rest";
            presentationBodyText.fontSize = 0.14f;
            presentationBodyText.alignment = TextAlignmentOptions.Left;
            presentationBodyText.color = new Color(0.15f, 0.18f, 0.22f);
            RectTransform bRect = bodyObj.GetComponent<RectTransform>();
            bRect.anchoredPosition = new Vector3(-0.55f, 0.08f, 0);
            bRect.sizeDelta = new Vector2(2.4f, 1.0f);

            GameObject sloganObj = new GameObject("WhiteboardSlogan");
            sloganObj.transform.SetParent(canvasObj.transform, false);
            presentationStatusText = sloganObj.AddComponent<TextMeshPro>();
            presentationStatusText.text = "It's okay to\nnot be okay";
            presentationStatusText.fontSize = 0.16f;
            presentationStatusText.alignment = TextAlignmentOptions.Center;
            presentationStatusText.color = new Color(0.2f, 0.4f, 0.65f);
            presentationStatusText.fontStyle = FontStyles.Italic;
            RectTransform sRect = sloganObj.GetComponent<RectTransform>();
            sRect.anchoredPosition = new Vector3(1.18f, 0.28f, 0);
            sRect.sizeDelta = new Vector2(1.2f, 0.7f);
        }

        private void BuildPottedPlant(Transform parent, Vector3 pos, Material potMat, Material leafMat)
        {
            GameObject plantGroup = new GameObject("PottedPlant");
            plantGroup.transform.SetParent(parent);
            plantGroup.transform.position = pos;

            GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "CeramicPot";
            pot.transform.SetParent(plantGroup.transform);
            pot.transform.localPosition = new Vector3(0, 0.35f, 0);
            pot.transform.localScale = new Vector3(0.42f, 0.35f, 0.42f);
            pot.GetComponent<Renderer>().sharedMaterial = potMat;

            // Foliage Leaves
            for (int i = 0; i < 7; i++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaf.name = "Leaf_" + i;
                leaf.transform.SetParent(plantGroup.transform);
                float angle = i * (360f / 7f);
                Vector3 leafOffset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.22f, 0.8f + (i % 3) * 0.12f, Mathf.Cos(angle * Mathf.Deg2Rad) * 0.22f);
                leaf.transform.localPosition = leafOffset;
                leaf.transform.localScale = new Vector3(0.32f, 0.12f, 0.42f);
                leaf.transform.localRotation = Quaternion.Euler(20f, angle, 15f);
                leaf.GetComponent<Renderer>().sharedMaterial = leafMat;
            }
        }

        private void BuildBookshelf(Transform parent, Vector3 pos, Material woodMat)
        {
            GameObject shelf = new GameObject("LeftBookshelf");
            shelf.transform.SetParent(parent);
            shelf.transform.position = pos;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.transform.SetParent(shelf.transform);
            frame.transform.localPosition = new Vector3(0, 1.1f, 0);
            frame.transform.localScale = new Vector3(0.45f, 2.2f, 0.85f);
            frame.GetComponent<Renderer>().sharedMaterial = woodMat;

            Material bookMat1 = MaterialHelper.CreateMaterial("BookMat1", new Color(0.2f, 0.35f, 0.6f));
            Material bookMat2 = MaterialHelper.CreateMaterial("BookMat2", new Color(0.7f, 0.25f, 0.2f));

            for (int i = 0; i < 4; i++)
            {
                GameObject book = GameObject.CreatePrimitive(PrimitiveType.Cube);
                book.transform.SetParent(shelf.transform);
                book.transform.localPosition = new Vector3(0.05f, 0.45f + i * 0.45f, (i - 1.5f) * 0.18f);
                book.transform.localScale = new Vector3(0.22f, 0.32f, 0.08f);
                book.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? bookMat1 : bookMat2;
            }
        }

        private void BuildRightGlassWindowWall(Transform parent, Material frameMat)
        {
            GameObject windowGroup = new GameObject("RightWindowWall");
            windowGroup.transform.SetParent(parent);

            float windowX = roomWidth / 2f;

            GameObject glassWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassWall.name = "GlassPane";
            glassWall.transform.SetParent(windowGroup.transform);
            glassWall.transform.position = new Vector3(windowX, roomHeight / 2f, 0);
            glassWall.transform.localScale = new Vector3(0.04f, roomHeight - 0.4f, roomLength - 0.4f);
            glassWall.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateGlassMaterial();

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.transform.SetParent(windowGroup.transform);
            pillar.transform.position = new Vector3(windowX - 0.05f, roomHeight / 2f, 0);
            pillar.transform.localScale = new Vector3(0.12f, roomHeight, 0.12f);
            pillar.GetComponent<Renderer>().sharedMaterial = frameMat;
        }
    }
}
