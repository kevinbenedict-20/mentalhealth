using UnityEngine;
using UnityEngine.Rendering;

namespace MentalHealthApp.Generators
{
    public static class MaterialHelper
    {
        public static Shader GetActivePipelineShader()
        {
            // 1. Try getting shader from active RenderPipelineAsset in GraphicsSettings
            if (GraphicsSettings.defaultRenderPipeline != null && GraphicsSettings.defaultRenderPipeline.defaultMaterial != null)
            {
                return GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader;
            }

            // 2. Try QualitySettings render pipeline
            if (QualitySettings.renderPipeline != null && QualitySettings.renderPipeline.defaultMaterial != null)
            {
                return QualitySettings.renderPipeline.defaultMaterial.shader;
            }

            // 3. Fallbacks
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("URP/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return shader;
        }

        public static Material CreateMaterial(string name, Color color, float metallic = 0.1f, float smoothness = 0.5f)
        {
            Shader shader = GetActivePipelineShader();
            Material mat = new Material(shader);
            mat.name = name;

            // Set main color for both URP (_BaseColor) and Built-in Standard (_Color)
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            return mat;
        }

        public static Material CreateEmissiveMaterial(string name, Color emissiveColor)
        {
            Material mat = CreateMaterial(name, emissiveColor, 0.0f, 0.5f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissiveColor);
            return mat;
        }

        public static Material CreateGlassMaterial()
        {
            Color glassCol = new Color(0.85f, 0.92f, 0.98f, 0.35f);
            Material mat = CreateMaterial("GlassMaterial", glassCol, 0.9f, 0.95f);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent in URP
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            return mat;
        }
    }
}
