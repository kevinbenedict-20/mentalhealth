using UnityEngine;
using TMPro;

namespace MentalHealthApp.Generators
{
    public class ModernMeetingRoomGenerator : MonoBehaviour
    {
        [Header("Room Dimensions")]
        public float roomWidth = 9f;
        public float roomLength = 8f;
        public float roomHeight = 3.5f;

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

            // Materials matching reference image
            Material floorMat = MaterialHelper.CreateMaterial("WoodFloorMat", new Color(0.68f, 0.65f, 0.62f), 0.05f, 0.4f);
            Material wallMat = MaterialHelper.CreateMaterial("SoftGreyWallMat", new Color(0.86f, 0.88f, 0.9f), 0.05f, 0.8f);
            Material ceilingMat = MaterialHelper.CreateMaterial("CeilingMat", new Color(0.94f, 0.95f, 0.96f), 0.05f, 0.9f);
            Material frameMat = MaterialHelper.CreateMaterial("BoardFrameMat", new Color(0.35f, 0.38f, 0.42f), 0.4f, 0.5f);
            Material shelfWoodMat = MaterialHelper.CreateMaterial("ShelfWoodMat", new Color(0.65f, 0.45f, 0.28f), 0.1f, 0.5f);

            // 1. Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(roomParent.transform);
            floor.transform.position = new Vector3(0, -0.1f, 0);
            floor.transform.localScale = new Vector3(roomWidth, 0.2f, roomLength);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // 2. Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(roomParent.transform);
            ceiling.transform.position = new Vector3(0, roomHeight + 0.1f, 0);
            ceiling.transform.localScale = new Vector3(roomWidth, 0.2f, roomLength);
            ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;

            // Recessed Ceiling Lights
            for (int x = -2; x <= 2; x += 2)
            {
                for (int z = -2; z <= 2; z += 2)
                {
                    GameObject pLight = new GameObject("CeilingSpot");
                    pLight.transform.SetParent(roomParent.transform);
                    pLight.transform.position = new Vector3(x, roomHeight - 0.2f, z);
                    Light l = pLight.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.intensity = 1.6f;
                    l.range = 7.0f;
                    l.color = new Color(0.98f, 0.96f, 0.92f);
                    l.shadows = LightShadows.None;
                }
            }

            // 3. Back Wall (Whiteboard & Posters)
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.SetParent(roomParent.transform);
            backWall.transform.position = new Vector3(0, roomHeight / 2f, roomLength / 2f);
            backWall.transform.localScale = new Vector3(roomWidth, roomHeight, 0.2f);
            backWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            // 4. Whiteboard on Back Wall
            BuildMentalHealthWhiteboard(roomParent.transform, frameMat);

            // 5. Mental Health Posters on Back Wall
            BuildWallPosters(roomParent.transform);

            // 6. Left Bookshelf
            BuildBookshelf(roomParent.transform, new Vector3(-roomWidth / 2f + 0.6f, 0, roomLength / 2f - 1.2f), shelfWoodMat);

            // 7. Left Solid Wall & Right Glass Window Wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(roomParent.transform);
            leftWall.transform.position = new Vector3(-roomWidth / 2f, roomHeight / 2f, 0);
            leftWall.transform.localScale = new Vector3(0.2f, roomHeight, roomLength);
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

            BuildRightWindowWall(roomParent.transform, frameMat);
        }

        private void BuildMentalHealthWhiteboard(Transform parent, Material frameMat)
        {
            presentationScreenObj = new GameObject("MentalHealthWhiteboard");
            presentationScreenObj.transform.SetParent(parent);
            presentationScreenObj.transform.position = new Vector3(-0.4f, 2.35f, roomLength / 2f - 0.14f);

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "WhiteboardFrame";
            frame.transform.SetParent(presentationScreenObj.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(3.8f, 2.0f, 0.05f);
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;

            GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surface.name = "WhiteboardSurface";
            surface.transform.SetParent(presentationScreenObj.transform);
            surface.transform.localPosition = new Vector3(0, 0, -0.03f);
            surface.transform.localRotation = Quaternion.Euler(0, 180, 0);
            surface.transform.localScale = new Vector3(3.68f, 1.88f, 1f);
            surface.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("WhiteboardMat", Color.white, 0.0f, 0.9f);

            GameObject canvasObj = new GameObject("WhiteboardCanvas");
            canvasObj.transform.SetParent(presentationScreenObj.transform);
            canvasObj.transform.localPosition = new Vector3(0, 0, -0.035f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(3.68f, 1.88f);

            GameObject titleObj = new GameObject("WhiteboardTitle");
            titleObj.transform.SetParent(canvasObj.transform, false);
            presentationTitleText = titleObj.AddComponent<TextMeshPro>();
            presentationTitleText.text = "Mental Health";
            presentationTitleText.fontSize = 0.32f;
            presentationTitleText.alignment = TextAlignmentOptions.Left;
            presentationTitleText.color = new Color(0.12f, 0.2f, 0.35f);
            presentationTitleText.fontStyle = FontStyles.Bold;
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchoredPosition = new Vector3(-0.75f, 0.68f, 0);
            tRect.sizeDelta = new Vector2(2.0f, 0.4f);

            GameObject bodyObj = new GameObject("WhiteboardContent");
            bodyObj.transform.SetParent(canvasObj.transform, false);
            presentationBodyText = bodyObj.AddComponent<TextMeshPro>();
            presentationBodyText.text = "What are we discussing?\n• Common mental health issues\n• Causes and triggers\n• How to support each other\n• Healthy coping strategies\n• When to seek professional help";
            presentationBodyText.fontSize = 0.135f;
            presentationBodyText.alignment = TextAlignmentOptions.Left;
            presentationBodyText.color = new Color(0.15f, 0.18f, 0.22f);
            RectTransform bRect = bodyObj.GetComponent<RectTransform>();
            bRect.anchoredPosition = new Vector3(-0.55f, 0.05f, 0);
            bRect.sizeDelta = new Vector2(2.3f, 1.0f);

            GameObject sloganObj = new GameObject("WhiteboardSlogan");
            sloganObj.transform.SetParent(canvasObj.transform, false);
            presentationStatusText = sloganObj.AddComponent<TextMeshPro>();
            presentationStatusText.text = "It's okay\nto not be okay";
            presentationStatusText.fontSize = 0.15f;
            presentationStatusText.alignment = TextAlignmentOptions.Center;
            presentationStatusText.color = new Color(0.18f, 0.25f, 0.4f);
            presentationStatusText.fontStyle = FontStyles.Italic;
            RectTransform sRect = sloganObj.GetComponent<RectTransform>();
            sRect.anchoredPosition = new Vector3(1.1f, 0.25f, 0);
            sRect.sizeDelta = new Vector2(1.2f, 0.7f);
        }

        private void BuildWallPosters(Transform parent)
        {
            BuildPoster(parent, new Vector3(-3.4f, 2.5f, roomLength / 2f - 0.12f), new Vector2(0.6f, 1.0f), new Color(0.92f, 0.92f, 0.94f), "Be\nKind\nTo Your\nMind", new Color(0.2f, 0.25f, 0.35f));
            BuildPoster(parent, new Vector3(2.2f, 2.7f, roomLength / 2f - 0.12f), new Vector2(0.55f, 0.8f), new Color(0.45f, 0.7f, 0.65f), "Talk\nListen\nSupport", Color.white);
            BuildPoster(parent, new Vector3(3.1f, 2.6f, roomLength / 2f - 0.12f), new Vector2(0.55f, 0.85f), new Color(0.9f, 0.75f, 0.35f), "Better\nMental Health\nBrighter\nFuture", new Color(0.15f, 0.18f, 0.25f));
        }

        private void BuildPoster(Transform parent, Vector3 pos, Vector2 size, Color bgCol, string textStr, Color textCol)
        {
            GameObject poster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            poster.name = "WallPoster";
            poster.transform.SetParent(parent);
            poster.transform.position = pos;
            poster.transform.rotation = Quaternion.Euler(0, 180, 0);
            poster.transform.localScale = new Vector3(size.x, size.y, 1f);
            poster.GetComponent<Renderer>().sharedMaterial = MaterialHelper.CreateMaterial("PosterBgMat", bgCol, 0.0f, 0.6f);

            GameObject canvasObj = new GameObject("PosterCanvas");
            canvasObj.transform.SetParent(poster.transform, false);
            canvasObj.transform.localPosition = new Vector3(0, 0, -0.01f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            GameObject textObj = new GameObject("PosterText");
            textObj.transform.SetParent(canvasObj.transform, false);
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = textStr;
            tmp.fontSize = 0.12f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textCol;
            tmp.fontStyle = FontStyles.Bold;
            RectTransform tRect = textObj.GetComponent<RectTransform>();
            tRect.anchoredPosition = Vector3.zero;
            tRect.sizeDelta = size;
        }

        private void BuildBookshelf(Transform parent, Vector3 pos, Material woodMat)
        {
            GameObject shelf = new GameObject("LeftBookshelf");
            shelf.transform.SetParent(parent);
            shelf.transform.position = pos;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.transform.SetParent(shelf.transform);
            frame.transform.localPosition = new Vector3(0, 1.1f, 0);
            frame.transform.localScale = new Vector3(0.5f, 2.2f, 0.8f);
            frame.GetComponent<Renderer>().sharedMaterial = woodMat;

            Material bookMat1 = MaterialHelper.CreateMaterial("BookMat1", new Color(0.2f, 0.35f, 0.6f));
            Material bookMat2 = MaterialHelper.CreateMaterial("BookMat2", new Color(0.7f, 0.25f, 0.2f));

            for (int i = 0; i < 3; i++)
            {
                GameObject book = GameObject.CreatePrimitive(PrimitiveType.Cube);
                book.transform.SetParent(shelf.transform);
                book.transform.localPosition = new Vector3(0.05f, 0.5f + i * 0.55f, (i - 1) * 0.2f);
                book.transform.localScale = new Vector3(0.22f, 0.32f, 0.08f);
                book.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? bookMat1 : bookMat2;
            }
        }

        private void BuildRightWindowWall(Transform parent, Material frameMat)
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
