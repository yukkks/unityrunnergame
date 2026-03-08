using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public float rotateSpeed = 180f;
    public float destroyZ = -10f;
    public float points = 10f;

    [Header("Pickup Animation")]
    public float pickupDuration = 0.35f;
    public float pickupFloat = 0.5f;
    public float splitDistance = 0.35f;
    public float splitRotate = 260f;

    private bool picked;
    private Renderer cachedRenderer;
    private MeshFilter cachedMeshFilter;
    private MaterialPropertyBlock block;
    private string colorProp;
    private Color baseColor = Color.white;

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        cachedMeshFilter = GetComponent<MeshFilter>();
        if (cachedRenderer)
        {
            if (cachedRenderer.sharedMaterial && cachedRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                colorProp = "_BaseColor";
                baseColor = cachedRenderer.sharedMaterial.GetColor(colorProp);
            }
            else if (cachedRenderer.sharedMaterial && cachedRenderer.sharedMaterial.HasProperty("_Color"))
            {
                colorProp = "_Color";
                baseColor = cachedRenderer.sharedMaterial.GetColor(colorProp);
            }
        }
    }

    void Update()
    {
        if (picked) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        float s = GameManager.Instance ? GameManager.Instance.moveSpeed : 12f;
        transform.Translate(Vector3.back * s * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime, Space.Self);

        if (transform.position.z < destroyZ)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<PlayerController>()) return;
        if (picked) return;
        picked = true;

        if (GameManager.Instance)
        {
            GameManager.Instance.AddScore(points);
        }
        if (AudioController.Instance)
        {
            AudioController.Instance.PlayCoin();
        }

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        StartCoroutine(PlayPickupSplit());
    }

    System.Collections.IEnumerator PlayPickupSplit()
    {
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        if (!cachedMeshFilter || !cachedRenderer || cachedMeshFilter.sharedMesh == null)
        {
            // Fallback: simple float + fade
            float t0 = 0f;
            while (t0 < pickupDuration)
            {
                float p0 = t0 / pickupDuration;
                float ease0 = 1f - Mathf.Pow(1f - p0, 3f);
                transform.position = startPos + Vector3.up * (pickupFloat * ease0);
                ApplyFade(cachedRenderer, p0);
                t0 += Time.deltaTime;
                yield return null;
            }
            Destroy(gameObject);
            yield break;
        }

        cachedRenderer.enabled = false;

        Mesh mesh = cachedMeshFilter.sharedMesh;
        Material mat = cachedRenderer.sharedMaterial;
        Vector3 halfScale = new Vector3(startScale.x * 0.5f, startScale.y, startScale.z);
        Vector3 rightDir = transform.right;

        Renderer left = CreateHalf("CoinHalf_L", mesh, mat, startPos, startRot, halfScale);
        Renderer right = CreateHalf("CoinHalf_R", mesh, mat, startPos, startRot, halfScale);

        float t = 0f;
        while (t < pickupDuration)
        {
            float p = t / pickupDuration;
            float ease = 1f - Mathf.Pow(1f - p, 3f);

            Vector3 lift = Vector3.up * (pickupFloat * ease);
            Vector3 offset = rightDir * (splitDistance * ease);

            if (left)
            {
                left.transform.position = startPos - offset + lift;
                left.transform.rotation = startRot * Quaternion.Euler(0f, 0f, -splitRotate * p);
                ApplyFade(left, p);
            }
            if (right)
            {
                right.transform.position = startPos + offset + lift;
                right.transform.rotation = startRot * Quaternion.Euler(0f, 0f, splitRotate * p);
                ApplyFade(right, p);
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (left) Destroy(left.gameObject);
        if (right) Destroy(right.gameObject);
        Destroy(gameObject);
    }

    Renderer CreateHalf(string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.transform.localScale = scale;

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        return mr;
    }

    void ApplyFade(Renderer renderer, float t)
    {
        if (!renderer || string.IsNullOrEmpty(colorProp)) return;
        if (block == null) block = new MaterialPropertyBlock();

        Color c = baseColor;
        c.a = Mathf.Lerp(1f, 0f, t);
        renderer.GetPropertyBlock(block);
        block.SetColor(colorProp, c);
        renderer.SetPropertyBlock(block);
    }
}
