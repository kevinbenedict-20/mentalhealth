using UnityEngine;
using UnityEditor;
using System.IO;

namespace MentalHealthApp.Editor
{
    [InitializeOnLoad]
    public class TMPAutoImporter
    {
        static TMPAutoImporter()
        {
            EditorApplication.delayCall += () =>
            {
                ImportTMPEssentialResourcesIfNeeded();
            };
        }

        [MenuItem("Tools/Import TMP Essential Resources", false, 2)]
        public static void ImportTMPEssentialResourcesIfNeeded()
        {
            // Check if TMP Settings or TextMesh Pro folder exists in Assets
            string tmpPath = Path.Combine(Application.dataPath, "TextMesh Pro");
            if (!Directory.Exists(tmpPath))
            {
                Debug.Log("TMP Essential Resources not found. Attempting automatic package import...");

                // Search for Unity's built-in TMP Essential Resources package path
                string editorPath = EditorApplication.applicationContentsPath;
                string[] possiblePackagePaths = new string[]
                {
                    Path.Combine(editorPath, "Resources", "PackageManager", "BuiltInPackages", "com.unity.ugui", "Package Resources", "TMP Essential Resources.unitypackage"),
                    Path.Combine(editorPath, "BuiltInPackages", "com.unity.ugui", "Package Resources", "TMP Essential Resources.unitypackage"),
                    Path.Combine(editorPath, "Package Manager", "BuiltInPackages", "com.unity.ugui", "Package Resources", "TMP Essential Resources.unitypackage")
                };

                foreach (string pkgPath in possiblePackagePaths)
                {
                    if (File.Exists(pkgPath))
                    {
                        Debug.Log("Importing TMP Essential Package from: " + pkgPath);
                        AssetDatabase.ImportPackage(pkgPath, false);
                        return;
                    }
                }

                Debug.LogWarning("TMP Essential Resources package file not located automatically. Please use Unity Menu: Window -> TextMeshPro -> Import TMP Essential Resources.");
            }
        }
    }
}
