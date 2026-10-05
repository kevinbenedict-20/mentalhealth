using UnityEngine;
using System.Collections.Generic;
using MentalHealthApp.Generators;
using MentalHealthApp.Discussion;
using MentalHealthApp.Assessment;
using MentalHealthApp.UI;

namespace MentalHealthApp.Core
{
    [ExecuteAlways]
    public class SceneBuilder : MonoBehaviour
    {
        [Header("Generator References")]
        public ModernMeetingRoomGenerator roomGenerator;
        public ModernLightingGenerator lightingGenerator;
        public ModernTableGenerator tableGenerator;
        public ModernChairGenerator chairGenerator;
        public TablePropsGenerator propsGenerator;
        public AvatarGenerator avatarGenerator;

        [Header("Logic & UI References")]
        public GroupDiscussionManager discussionManager;
        public StudentWellnessTracker wellnessTracker;
        public WellnessUIController uiController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnPlay()
        {
            SceneBuilder builder = Object.FindFirstObjectByType<SceneBuilder>();
            if (builder == null)
            {
                Debug.Log("Auto-Initializing Mental Health Group Discussion Environment on Play...");
                GameObject managerObj = new GameObject("ModernMeetingRoomManager");
                builder = managerObj.AddComponent<SceneBuilder>();
            }

            builder.ClearExistingEnvironment();
            builder.BuildCompleteEnvironment();
        }

        private void Awake()
        {
            if (transform.Find("ModernMeetingRoom") == null)
            {
                BuildCompleteEnvironment();
            }
        }

        private void Start()
        {
            if (transform.Find("ModernMeetingRoom") == null)
            {
                BuildCompleteEnvironment();
            }
        }

        public void ClearExistingEnvironment()
        {
            string[] oldNames = new string[] { "ModernMeetingRoom", "ModernChairs", "ModernMeetingTable", "ModernRoundTable", "StudentAvatars", "TableProps", "CircularRingLightFixture" };
            foreach (string n in oldNames)
            {
                GameObject oldObj = GameObject.Find(n);
                while (oldObj != null)
                {
                    DestroyImmediate(oldObj);
                    oldObj = GameObject.Find(n);
                }
            }

            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            foreach (GameObject child in children)
            {
                DestroyImmediate(child);
            }
        }

        [ContextMenu("Build Complete Environment")]
        public void BuildCompleteEnvironment()
        {
            Debug.Log("Starting Procedural Construction of Mental Health Group Discussion Environment (Clean Build)...");

            ClearExistingEnvironment();

            // 1. Ensure Components
            if (roomGenerator == null) roomGenerator = GetOrAddComponent<ModernMeetingRoomGenerator>();
            if (lightingGenerator == null) lightingGenerator = GetOrAddComponent<ModernLightingGenerator>();
            if (tableGenerator == null) tableGenerator = GetOrAddComponent<ModernTableGenerator>();
            if (chairGenerator == null) chairGenerator = GetOrAddComponent<ModernChairGenerator>();
            if (propsGenerator == null) propsGenerator = GetOrAddComponent<TablePropsGenerator>();
            if (avatarGenerator == null) avatarGenerator = GetOrAddComponent<AvatarGenerator>();

            if (discussionManager == null) discussionManager = GetOrAddComponent<GroupDiscussionManager>();
            if (wellnessTracker == null) wellnessTracker = GetOrAddComponent<StudentWellnessTracker>();

            GameObject uiObj = GameObject.Find("WellnessUICanvas");
            if (uiObj == null)
            {
                uiObj = new GameObject("WellnessUICanvas");
            }
            uiController = uiObj.GetComponent<WellnessUIController>();
            if (uiController == null) uiController = uiObj.AddComponent<WellnessUIController>();

            // 2. Generate Architecture & Lighting
            roomGenerator.GenerateRoom();
            lightingGenerator.SetupLighting();

            // 3. Generate Round Wooden Table & Chairs
            tableGenerator.GenerateTable();
            chairGenerator.GenerateChairs(tableGenerator.tableDiameter, tableGenerator.tableHeight);
            propsGenerator.GenerateProps(chairGenerator.seatAnchors, tableGenerator.tableHeight);

            // 4. Generate 5 Student Avatars
            avatarGenerator.GenerateAvatars(chairGenerator.seatAnchors);

            // 5. Connect Managers & UI
            GeminiDiscussionAgent geminiAgent = GetOrAddComponent<GeminiDiscussionAgent>();
            discussionManager.geminiAgent = geminiAgent;

            discussionManager.roomGenerator = roomGenerator;
            discussionManager.avatarGenerator = avatarGenerator;
            discussionManager.wellnessTracker = wellnessTracker;
            discussionManager.uiController = uiController;

            uiController.wellnessTracker = wellnessTracker;
            uiController.discussionManager = discussionManager;
            uiController.BuildUICanvas();

            // 6. Setup Exact Camera Perspective Matching Reference Image
            SetupMentalHealthCamera();

            Debug.Log("Mental Health Group Discussion Room Construction Completed Successfully!");
        }

        private void SetupMentalHealthCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            // Direct front perspective looking across round table towards center facilitator and whiteboard
            mainCam.transform.position = new Vector3(0, 1.48f, -2.85f);
            mainCam.transform.rotation = Quaternion.Euler(11f, 0, 0);
            mainCam.fieldOfView = 54f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.85f, 0.88f, 0.92f);
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            T comp = GetComponent<T>();
            if (comp == null) comp = gameObject.AddComponent<T>();
            return comp;
        }
    }
}
