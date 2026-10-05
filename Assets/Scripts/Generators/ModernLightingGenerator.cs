using UnityEngine;

namespace MentalHealthApp.Generators
{
    public class ModernLightingGenerator : MonoBehaviour
    {
        public void SetupLighting()
        {
            // 1. Sun / Main Directional Light
            Light sunLight = null;
            Light[] existingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in existingLights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLight = l;
                    break;
                }
            }

            if (sunLight == null)
            {
                GameObject sunObj = new GameObject("SunLight");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }

            sunLight.transform.rotation = Quaternion.Euler(35f, -120f, 0f); // Soft angle entering through glass windows
            sunLight.color = new Color(0.98f, 0.96f, 0.92f); // Warm natural daylight
            sunLight.intensity = 1.1f;
            sunLight.shadows = LightShadows.Soft;

            // 2. Window Bounce / Fill Light
            GameObject windowFill = GameObject.Find("WindowFillLight");
            if (windowFill == null)
            {
                windowFill = new GameObject("WindowFillLight");
                Light fillLight = windowFill.AddComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.transform.rotation = Quaternion.Euler(15f, 60f, 0f); // Gentle fill opposite windows
                fillLight.color = new Color(0.85f, 0.9f, 1.0f); // Cool ambient sky fill
                fillLight.intensity = 0.45f;
                fillLight.shadows = LightShadows.None;
            }

            // 3. Ambient Lighting Environment Settings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.82f, 0.90f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.65f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.3f, 0.32f);
            RenderSettings.ambientIntensity = 1.2f;
        }
    }
}
