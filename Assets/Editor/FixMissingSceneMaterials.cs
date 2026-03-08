using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixMissingSceneMaterials
{
    private static readonly string[] RoadMaterialPaths =
    {
        "Assets/PolygonSciFiCity/Materials/Misc/Road.mat",
        "Assets/PolygonSciFiCity/Materials/Misc/PolygonSciFi_Buildings_Background.mat"
    };

    [MenuItem("Tools/SciFiRunner/Select Missing-Shader Renderers")]
    private static void SelectMissingShaderRenderers()
    {
        List<GameObject> hits = new List<GameObject>();
        foreach (Renderer r in Object.FindObjectsOfType<Renderer>(true))
        {
            if (!r) continue;
            foreach (Material m in r.sharedMaterials)
            {
                if (IsBadMaterial(m))
                {
                    hits.Add(r.gameObject);
                    break;
                }
            }
        }

        Selection.objects = hits.ToArray();
        Debug.Log($"Found {hits.Count} renderer(s) with missing/pink shaders.");
    }

    [MenuItem("Tools/SciFiRunner/Replace Missing Shaders In Scene")]
    private static void ReplaceMissingShadersInScene()
    {
        Material fallback = LoadFallbackMaterial();
        if (!fallback)
        {
            Debug.LogError("No fallback material found. Import POLYGON pack materials first.");
            return;
        }

        int replaced = 0;
        foreach (Renderer r in Object.FindObjectsOfType<Renderer>(true))
        {
            if (!r) continue;
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (IsBadMaterial(mats[i]))
                {
                    mats[i] = fallback;
                    replaced++;
                    changed = true;
                }
            }
            if (changed)
            {
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
            }
        }

        if (replaced > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
        }

        Debug.Log($"Replaced {replaced} material slot(s) with fallback.");
    }

    private static bool IsBadMaterial(Material mat)
    {
        if (!mat) return true;
        if (!mat.shader) return true;
        string name = mat.shader.name;
        if (name == "Hidden/InternalErrorShader") return true;
        if (name.StartsWith("SyntyStudios/")) return true;
        return false;
    }

    private static Material LoadFallbackMaterial()
    {
        foreach (string path in RoadMaterialPaths)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
        }
        return null;
    }
}
