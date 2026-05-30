using UnityEngine;

// Streams roadside props toward the player and recycles them — an infinite,
// non-repeating, lightweight alternative to looping a heavy static diorama.
// Fills BOTH sides at each step for a dense, lived-in alley; re-varies on recycle.
public class PropStreamer : MonoBehaviour
{
    public GameObject[] propPrefabs;
    [Tooltip("Number of z-slots; each slot places a prop on the left AND the right (total = count*2).")]
    public int count = 60;
    [Tooltip("Z gap between slots.")]
    public float spacing = 2.2f;
    [Tooltip("Random horizontal distance from centre (keep > lane half-width, < building line).")]
    public Vector2 sideX = new Vector2(2.4f, 4.0f);
    public float startZ = 4f;
    public float recycleZ = -14f;
    public float groundY = 0f;
    public Vector2 scaleRange = new Vector2(0.85f, 1.3f);
    public bool randomYaw = true;

    private float loopLength;
    private Transform[] props;

    void Start() { Build(); }

    void Build()
    {
        if (propPrefabs == null || propPrefabs.Length == 0) return;
        loopLength = count * spacing;
        props = new Transform[count * 2];
        int idx = 0;
        for (int i = 0; i < count; i++)
        {
            float z = startZ + i * spacing;
            props[idx++] = Spawn(z, -1f);
            props[idx++] = Spawn(z, 1f);
        }
    }

    Transform Spawn(float z, float side)
    {
        GameObject prefab = propPrefabs[Random.Range(0, propPrefabs.Length)];
        GameObject go = Instantiate(prefab, transform);
        foreach (Collider c in go.GetComponentsInChildren<Collider>()) Destroy(c); // decor only
        Place(go.transform, z, side);
        return go.transform;
    }

    void Place(Transform t, float z, float side)
    {
        float x = side * Random.Range(sideX.x, sideX.y);
        t.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
        t.localRotation = randomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
        t.localPosition = new Vector3(x, 0f, z);
        AlignGround(t);
    }

    void AlignGround(Transform t)
    {
        var rends = t.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        t.position += new Vector3(0f, groundY - b.min.y, 0f);
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;
        if (props == null) return;

        float dz = (GameManager.Instance ? GameManager.Instance.moveSpeed : 12f) * Time.deltaTime;
        for (int i = 0; i < props.Length; i++)
        {
            Transform t = props[i];
            if (!t) continue;
            Vector3 p = t.localPosition;
            p.z -= dz;
            if (p.z < recycleZ) Place(t, p.z + loopLength, Mathf.Sign(p.x)); // recycle ahead, same side, re-vary
            else t.localPosition = p;
        }
    }
}
