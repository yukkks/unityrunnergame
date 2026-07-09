using UnityEngine;

// A cheap soft "contact shadow" under an object so it reads as grounded rather
// than floating. Spawns a flat, unlit dark sprite (shared radial texture) as a
// separate follower object and pins it to the object's base each frame. Works
// regardless of the scene's light direction, and costs almost nothing.
public class BlobShadow : MonoBehaviour
{
    public float footprintScale = 1.3f; // blob diameter vs the object's footprint
    public float alpha = 0.32f;
    public float groundY = 0.45f;       // road-surface height all shadows rest on

    private Transform shadowT;
    private static Sprite sharedSprite;

    void Awake()
    {
        EnsureSprite();
        var go = new GameObject(name + "_Shadow");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sharedSprite;
        sr.color = new Color(0f, 0f, 0f, alpha);
        sr.sortingOrder = -10;
        shadowT = go.transform;
        shadowT.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void OnEnable()  { if (shadowT) shadowT.gameObject.SetActive(true); }
    void OnDisable() { if (shadowT) shadowT.gameObject.SetActive(false); }
    void OnDestroy() { if (shadowT) Destroy(shadowT.gameObject); }

    void LateUpdate()
    {
        if (!shadowT) return;

        float rad = 0.4f;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (r.transform.IsChildOf(shadowT)) continue;
            rad = Mathf.Max(rad, Mathf.Max(r.bounds.size.x, r.bounds.size.z) * 0.5f);
        }

        Vector3 p = transform.position;
        shadowT.position = new Vector3(p.x, groundY, p.z);
        shadowT.rotation = Quaternion.Euler(90f, 0f, 0f);
        float d = rad * 2f * footprintScale;
        shadowT.localScale = new Vector3(d, d, 1f);
    }

    static void EnsureSprite()
    {
        if (sharedSprite != null) return;
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f, dy = (y + 0.5f) / S - 0.5f;
                float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                float a = 1f - dist; a *= a; // soft radial falloff
                px[y * S + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        sharedSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
    }
}
