using UnityEngine;

// Procedural set-dressing built at runtime: a torii gate at the vanishing point
// (fills the dark void and gives a "running toward something" read) plus subtle
// lane guide lines on the road. Kept out of prefabs/scene geometry so it's
// low-risk and easy to tweak.
public class SceneDressing : MonoBehaviour
{
    public float roadY = 0.45f;
    public float toriiZ = 95f;

    void Start()
    {
        BuildLaneLines();
        BuildTorii();
    }

    Material Mat(Color c, bool emissive)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (!sh) sh = Shader.Find("Standard");
        var m = new Material(sh) { hideFlags = HideFlags.DontSave };
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (emissive && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.5f);
        }
        return m;
    }

    GameObject Box(string n, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n;
        var col = go.GetComponent<Collider>();
        if (col) Destroy(col);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    void BuildLaneLines()
    {
        var mat = TransparentMat(new Color(0.95f, 0.92f, 0.82f, 0.22f)); // ~22% opacity
        var root = new GameObject("LaneLines").transform;
        // Dashed guides at the two lane boundaries, raised clear of the road.
        foreach (float x in new float[] { -0.8f, 0.8f })
            for (int i = 0; i < 40; i++)
                Box("Dash", new Vector3(x, roadY + 0.06f, i * 6f - 30f), new Vector3(0.12f, 0.02f, 2.6f), mat, root);
    }

    Material TransparentMat(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (!sh) sh = Shader.Find("Standard");
        var m = new Material(sh) { hideFlags = HideFlags.DontSave };
        m.SetFloat("_Surface", 1f);   // 0 opaque, 1 transparent
        m.SetFloat("_Blend", 0f);     // alpha blend
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", (Color)c * 0.2f);
        }
        return m;
    }

    void BuildTorii()
    {
        var mat = Mat(new Color(0.5f, 0.11f, 0.09f), false); // deep torii red
        var root = new GameObject("Torii").transform;
        root.position = new Vector3(0f, roadY, toriiZ);
        float h = 7.5f, w = 5.2f;
        Box("PillarL", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.55f, h, 0.55f), mat, root);
        Box("PillarR", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.55f, h, 0.55f), mat, root);
        Box("TopBeam", new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 1.8f, 0.65f, 0.8f), mat, root);
        Box("Nuki", new Vector3(0f, h * 0.78f, 0f), new Vector3(w + 0.4f, 0.45f, 0.65f), mat, root);
    }
}
