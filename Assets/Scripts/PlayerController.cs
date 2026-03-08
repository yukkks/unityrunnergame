using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float laneOffset = 1.2f;
    public float laneLerp = 10f;
    private int lane = 0;
    public CameraFollow cameraFollow;

    [Header("VFX")]
    public ParticleSystem thruster;
    public Color thrusterColor = new Color(1f, 1f, 1f, 1f);
    public float thrusterRate = 30f;
    public Vector3 thrusterLocalOffset = new Vector3(0f, 0.2f, -0.4f);
    public ParticleSystem crashFxPrefab;
    public Material thrusterMaterial;

    private ParticleSystem.EmissionModule thrusterEmission;
    private Material runtimeThrusterMaterial;

    void Update()
    {
        bool running = GameManager.Instance == null || GameManager.Instance.IsRunning;
        if (!running)
        {
            if (thruster) thrusterEmission.rateOverTime = 0f;
            return;
        }

        if (!cameraFollow)
        {
            cameraFollow = FindObjectOfType<CameraFollow>();
        }

        int prevLane = lane;
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            lane = 1 - lane;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            lane = 1 - lane;
        }

        if (lane != prevLane && cameraFollow)
        {
            cameraFollow.OnLaneSwitch(lane - prevLane);
        }
        if (lane != prevLane && AudioController.Instance)
        {
            AudioController.Instance.PlayLaneSwitch();
        }

        float targetX = (lane == 0) ? -laneOffset : laneOffset;
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * laneLerp);
        transform.position = pos;

        if (thruster)
        {
            thrusterEmission.rateOverTime = thrusterRate;
            var main = thruster.main;
            main.startColor = thrusterColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            SpawnCrashFx();
            GameManager.Instance.GameOver();
        }
    }

    void Start()
    {
        EnsureThruster();
    }

    void EnsureThruster()
    {
        if (thruster)
        {
            thrusterEmission = thruster.emission;
            ApplyThrusterMaterial(thruster);
            return;
        }

        GameObject fx = new GameObject("ThrusterFx");
        fx.transform.SetParent(transform, false);
        fx.transform.localPosition = thrusterLocalOffset;
        fx.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        thruster = fx.AddComponent<ParticleSystem>();
        var main = thruster.main;
        main.loop = true;
        main.startLifetime = 0.25f;
        main.startSpeed = 1.8f;
        main.startSize = 0.12f;
        main.startColor = thrusterColor;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = thruster.emission;
        emission.rateOverTime = thrusterRate;
        thrusterEmission = emission;

        var shape = thruster.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.03f;

        var renderer = thruster.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        ApplyThrusterMaterial(thruster);
    }

    void ApplyThrusterMaterial(ParticleSystem ps)
    {
        if (!ps) return;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (!renderer) return;

        if (thrusterMaterial)
        {
            renderer.material = thrusterMaterial;
            return;
        }

        if (!runtimeThrusterMaterial)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Standard");
            runtimeThrusterMaterial = new Material(shader);
            runtimeThrusterMaterial.hideFlags = HideFlags.DontSave;

            if (runtimeThrusterMaterial.HasProperty("_BaseMap"))
            {
                runtimeThrusterMaterial.SetTexture("_BaseMap", null);
            }
            if (runtimeThrusterMaterial.HasProperty("_MainTex"))
            {
                runtimeThrusterMaterial.SetTexture("_MainTex", null);
            }

            Color white = Color.white;
            if (runtimeThrusterMaterial.HasProperty("_BaseColor"))
            {
                runtimeThrusterMaterial.SetColor("_BaseColor", white);
            }
            if (runtimeThrusterMaterial.HasProperty("_Color"))
            {
                runtimeThrusterMaterial.SetColor("_Color", white);
            }
            if (runtimeThrusterMaterial.HasProperty("_EmissionColor"))
            {
                runtimeThrusterMaterial.EnableKeyword("_EMISSION");
                runtimeThrusterMaterial.SetColor("_EmissionColor", white);
            }
        }

        renderer.material = runtimeThrusterMaterial;
    }

    void SpawnCrashFx()
    {
        if (crashFxPrefab)
        {
            ParticleSystem fx = Instantiate(crashFxPrefab, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
            return;
        }

        GameObject fxObj = new GameObject("CrashFx");
        fxObj.transform.position = transform.position;
        ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = 0.5f;
        main.startSpeed = 2.6f;
        main.startSize = 0.28f;
        main.startColor = new Color(1f, 0.55f, 0.1f, 1f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 28) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ps.Play();
        Destroy(fxObj, 2f);
    }
}
