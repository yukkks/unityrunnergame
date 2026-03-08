using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-time upgrade of POLYGON Sci‑Fi City materials to URP to fix pink shaders.
/// Runs once on editor load, then disables itself via EditorPrefs.
/// </summary>
public static class AutoUpgradePolygonMaterialsToURP
{
    private const string PrefKey = "SciFiRunner_PolygonMaterialsUpgraded";

    static AutoUpgradePolygonMaterialsToURP()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (EditorPrefs.GetBool(PrefKey, false))
        {
            return;
        }

        RunUpgrade(true);
    }

    [MenuItem("Tools/SciFiRunner/Upgrade POLYGON Materials to URP")]
    private static void MenuUpgrade()
    {
        RunUpgrade(false);
    }

    private static void RunUpgrade(bool setPref)
    {
        try
        {
            List<MaterialUpgrader> upgraders =
                MaterialUpgrader.FetchAllUpgradersForPipeline(typeof(UniversalRenderPipelineAsset));

            if (upgraders == null || upgraders.Count == 0)
            {
                Debug.LogWarning("URP material upgrader not found. Ensure URP is installed.");
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/PolygonSciFiCity" });
            int upgraded = 0;
            int forced = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) continue;

                bool didUpgrade = false;
                if (upgraders != null && upgraders.Count > 0)
                {
                    string message = string.Empty;
                    didUpgrade = MaterialUpgrader.Upgrade(
                        mat,
                        upgraders,
                        MaterialUpgrader.UpgradeFlags.LogMessageWhenNoUpgraderFound,
                        ref message
                    );
                }

                if (didUpgrade)
                {
                    upgraded += 1;
                    continue;
                }

                // Fallback: if shader is missing, Standard, or Synty custom shader, force URP/Lit.
                if (mat.shader == null ||
                    mat.shader.name == "Hidden/InternalErrorShader" ||
                    mat.shader.name == "Standard" ||
                    mat.shader.name.StartsWith("SyntyStudios/", StringComparison.OrdinalIgnoreCase))
                {
                    ForceUrpLit(mat);
                    forced += 1;
                }
            }

            AssetDatabase.SaveAssets();
            if (setPref)
            {
                EditorPrefs.SetBool(PrefKey, true);
            }
            Debug.Log($"POLYGON materials: upgraded={upgraded}, forced={forced}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to upgrade POLYGON materials to URP: " + ex.Message);
        }
    }

    private static void ForceUrpLit(Material mat)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (!urpLit) return;

        Texture baseMap = GetTexture(mat, "_BaseMap", "_MainTex", "_Texture");
        Color baseColor = GetColor(mat, "_BaseColor", "_Color");
        Texture normalMap = GetTexture(mat, "_BumpMap");
        Texture emissionMap = GetTexture(mat, "_EmissionMap", "_Emission");
        Color emissionColor = GetColor(mat, "_EmissionColor");
        Texture metallicMap = GetTexture(mat, "_MetallicGlossMap");

        mat.shader = urpLit;
        if (baseMap) mat.SetTexture("_BaseMap", baseMap);
        mat.SetColor("_BaseColor", baseColor);
        if (normalMap) mat.SetTexture("_BumpMap", normalMap);
        if (metallicMap) mat.SetTexture("_MetallicGlossMap", metallicMap);

        if (emissionMap)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", emissionMap);
            mat.SetColor("_EmissionColor", emissionColor == Color.black ? Color.white : emissionColor);
        }
    }

    private static Texture GetTexture(Material mat, params string[] names)
    {
        foreach (string n in names)
        {
            if (mat.HasProperty(n))
            {
                Texture t = mat.GetTexture(n);
                if (t) return t;
            }
        }
        return null;
    }

    private static Color GetColor(Material mat, params string[] names)
    {
        foreach (string n in names)
        {
            if (mat.HasProperty(n))
            {
                return mat.GetColor(n);
            }
        }
        return Color.white;
    }
}
