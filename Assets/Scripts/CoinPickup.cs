using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    public float rotateSpeed = 180f;
    public float destroyZ = -10f;
    public float points = 10f;
    [Tooltip("Kilograms gained per treat eaten.")]
    public float weightGain = 2f;

    [Header("Pickup Animation")]
    public float pickupDuration = 0.35f;
    public float pickupFloat = 0.5f;
    public float splitDistance = 0.35f;
    public float splitRotate = 260f;

    private bool picked;
    private Renderer cachedRenderer;
    private MeshFilter cachedMeshFilter;
    private static CameraFollow cachedCamera;
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
            ObjectPool.Release(gameObject);
        }
    }

    void OnEnable()
    {
        // Reset per-life state so a pooled coin behaves like a fresh one.
        picked = false;
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = true;
        if (cachedRenderer)
        {
            cachedRenderer.enabled = true;
            cachedRenderer.SetPropertyBlock(null);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<PlayerController>()) return;
        if (picked) return;
        picked = true;

        DogWeightVisual dogWeight = other.GetComponent<DogWeightVisual>();

        if (dogWeight)
        {
            dogWeight.GainWeight(weightGain);
        }

        if (GameManager.Instance)
        {
            GameManager.Instance.AddScore(points);
        }

        if (AudioController.Instance)
        {
            AudioController.Instance.PlayCoin();
            AudioController.Instance.PlayBark();
        }

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // Eat juice: a quick camera kick so the bite has impact. (The dog also
        // grows via GainWeight and the weight bar pulses in GameManager.)
        if (cachedCamera == null) cachedCamera = FindObjectOfType<CameraFollow>();
        if (cachedCamera) cachedCamera.Shake();

        StartCoroutine(PlayPickupPop());
    }

    // A snappy "chomp pop": the treat punches up in scale, then squashes to
    // nothing while lifting and fading. Reads well for any mesh (the old
    // coin-split was built for a flat coin and looked wrong on the steak).
    System.Collections.IEnumerator PlayPickupPop()
    {
        Vector3 startScale = transform.localScale;
        Vector3 startPos = transform.position;
        float dur = Mathf.Max(0.12f, pickupDuration);
        float t = 0f;
        while (t < dur)
        {
            float p = t / dur;
            // Scale: quick overshoot to ~1.35x in the first 25%, then down to 0.
            float s = p < 0.25f
                ? Mathf.Lerp(1f, 1.35f, p / 0.25f)
                : Mathf.Lerp(1.35f, 0f, (p - 0.25f) / 0.75f);
            transform.localScale = startScale * s;

            float ease = 1f - Mathf.Pow(1f - p, 3f);
            transform.position = startPos + Vector3.up * (pickupFloat * ease);
            transform.Rotate(Vector3.up, 540f * Time.deltaTime, Space.World);
            ApplyFade(cachedRenderer, p);

            t += Time.deltaTime;
            yield return null;
        }

        transform.localScale = startScale; // restore for the pool
        ObjectPool.Release(gameObject);
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
