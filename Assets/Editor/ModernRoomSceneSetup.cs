using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MentalHealthApp.Core;

namespace MentalHealthApp.Editor
{
    [InitializeOnLoad]
    public class ModernRoomSceneSetup
    {
        static ModernRoomSceneSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EnsureRoomExists();
                }
            };
        }

        [MenuItem("Tools/Build Modern Meeting Room Environment", false, 1)]
        public static void GenerateModernMeetingRoomScene()
        {
            EnsureRoomExists();
        }

        private static void EnsureRoomExists()
        {
            var scene = EditorSceneManager.GetActiveScene();
            
            SceneBuilder builder = Object.FindFirstObjectByType<SceneBuilder>();
            if (builder == null)
            {
                GameObject managerObj = new GameObject("ModernMeetingRoomManager");
                builder = managerObj.AddComponent<SceneBuilder>();
            }

            builder.ClearExistingEnvironment();
            builder.BuildCompleteEnvironment();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = builder.gameObject;
            Debug.Log("Modern Meeting Room Environment generated cleanly and saved!");
        }
    }
}
