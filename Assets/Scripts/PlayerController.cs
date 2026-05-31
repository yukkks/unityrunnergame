using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float laneOffset = 1.2f;
    public float laneLerp = 10f;
    [Range(2, 5)]
    public int laneCount = 3;
    public int startLane = 1;
    private int lane = 1;
    public CameraFollow cameraFollow;

    [Header("Swipe (mobile)")]
    [Tooltip("Minimum horizontal swipe distance as a fraction of screen width.")]
    public float swipeThresholdFraction = 0.05f;
    [Tooltip("Absolute minimum swipe distance in pixels (small-screen floor).")]
    public float minSwipePixels = 40f;

    private Vector2 swipeStart;
    private bool swiping;

    [Header("VFX")]
    public ParticleSystem thruster;
    public Color thrusterColor = new Color(1f, 1f, 1f, 1f);
    public float thrusterRate = 30f;
    public Vector3 thrusterLocalOffset = new Vector3(0f, 0.2f, -0.4f);
    public ParticleSystem crashFxPrefab;
    public Material thrusterMaterial;

    [Header("Weight")]
    [Tooltip("Kilograms lost when hitting an obstacle.")]
    public float weightLoss = 8f;

    [Header("Lane Switch Feel")]
    [Tooltip("Dog model to lean/hop on lane change. Auto-found from DogWeightVisual if empty.")]
    public Transform dogModel;
    [Tooltip("Bank angle (deg) the dog leans into a lane switch.")]
    public float leanAngle = 16f;
    public float leanReturn = 9f;
    [Tooltip("Little hop height on a lane switch.")]
    public float hopHeight = 0.2f;
    public float hopReturn = 4.5f;

    private float leanZ;
    private float hop;
    private Quaternion dogBaseRot;
    private Vector3 dogBasePos;

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
        int laneDelta = 0;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            laneDelta = -1;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            laneDelta = 1;
        }
        else
        {
            laneDelta = ReadSwipe();
        }

        if (laneDelta != 0)
        {
            int maxLane = Mathf.Max(0, laneCount - 1);
            lane = Mathf.Clamp(lane + laneDelta, 0, maxLane);
        }

        if (lane != prevLane)
        {
            int dir = lane - prevLane;
            if (cameraFollow) cameraFollow.OnLaneSwitch(dir);
            if (AudioController.Instance) AudioController.Instance.PlayLaneSwitch();
            leanZ = -dir * leanAngle;   // bank into the turn
            hop = 1f;                   // little hop
        }

        float centerIndex = (laneCount - 1) * 0.5f;
        float targetX = (lane - centerIndex) * laneOffset;
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * laneLerp);
        transform.position = pos;

        // Lane-switch lean + hop on the dog model (decays back to rest).
        if (dogModel)
        {
            leanZ = Mathf.Lerp(leanZ, 0f, leanReturn * Time.deltaTime);
            hop = Mathf.MoveTowards(hop, 0f, hopReturn * Time.deltaTime);
            dogModel.localRotation = dogBaseRot * Quaternion.Euler(0f, 0f, leanZ);
            Vector3 lp = dogBasePos;
            lp.y += Mathf.Sin(hop * Mathf.PI) * hopHeight;
            dogModel.localPosition = lp;
        }

        if (thruster)
        {
            thrusterEmission.rateOverTime = thrusterRate;
            var main = thruster.main;
            main.startColor = thrusterColor;
        }
    }
    // Returns -1 (left), 1 (right), or 0 — from a touch swipe or a mouse drag.
    int ReadSwipe()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                swipeStart = t.position;
                swiping = true;
            }
            else if (swiping && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
            {
                swiping = false;
                return SwipeDir(t.position - swipeStart);
            }
            return 0;
        }

        if (Input.GetMouseButtonDown(0))
        {
            swipeStart = (Vector2)Input.mousePosition;
            swiping = true;
        }
        else if (swiping && Input.GetMouseButtonUp(0))
        {
            swiping = false;
            return SwipeDir((Vector2)Input.mousePosition - swipeStart);
        }
        return 0;
    }

    int SwipeDir(Vector2 delta)
    {
        float threshold = Mathf.Max(minSwipePixels, Screen.width * swipeThresholdFraction);
        if (Mathf.Abs(delta.x) < threshold) return 0;          // too short
        if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return 0; // mostly vertical
        return delta.x > 0f ? 1 : -1;
    }

    // Called by ObstacleHit (which lives on the obstacle and detects the player
    // via GetComponent<PlayerController>(), mirroring how CoinPickup drives
    // pickups). The obstacle handles its own despawn.
    public void HitByObstacle()
    {
        // Default splat color if the obstacle didn't pass one.
        HitByObstacle(new Color(0.55f, 0.75f, 0.35f));
    }

    public void HitByObstacle(Color splatColor)
    {
        DogWeightVisual dogWeight = GetComponent<DogWeightVisual>();

        if (dogWeight)
        {
            dogWeight.LoseWeight(weightLoss);
        }

        SpawnCrashFx(splatColor);

        if (AudioController.Instance) AudioController.Instance.PlayWhimper();
        if (!cameraFollow) cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow) cameraFollow.Shake();
    }
    void Start()
    {
        int maxLane = Mathf.Max(0, laneCount - 1);
        lane = Mathf.Clamp(startLane, 0, maxLane);
        EnsureThruster();

        if (!dogModel)
        {
            DogWeightVisual dw = GetComponent<DogWeightVisual>();
            if (dw && dw.dogVisual) dogModel = dw.dogVisual;
        }
        if (dogModel)
        {
            dogBaseRot = dogModel.localRotation;
            dogBasePos = dogModel.localPosition;
        }
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

    private static Material crashFxMaterial;

    void SpawnCrashFx(Color splatColor)
    {
        if (crashFxPrefab)
        {
            ParticleSystem fx = Instantiate(crashFxPrefab, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
            return;
        }

        // Material loaded from Resources (a committed .mat using URP/Lit) so the
        // shader survives the WebGL build — runtime Shader.Find for particle
        // shaders returns null in builds and renders magenta/pink.
        if (crashFxMaterial == null) crashFxMaterial = Resources.Load<Material>("CrashFx");

        GameObject fxObj = new GameObject("CrashFx");
        fxObj.transform.position = transform.position + Vector3.up * 0.5f;

        // 1) Chunky veggie bits — burst outward and fall under gravity.
        ParticleSystem chunks = fxObj.AddComponent<ParticleSystem>();
        var cMain = chunks.main;
        cMain.loop = false;
        cMain.duration = 0.5f;
        cMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
        cMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        cMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
        cMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        cMain.gravityModifier = 2.2f;
        cMain.startColor = SplatGradient(splatColor);
        cMain.playOnAwake = false;
        var cEmit = chunks.emission;
        cEmit.rateOverTime = 0f;
        cEmit.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        var cShape = chunks.shape;
        cShape.enabled = true;
        cShape.shapeType = ParticleSystemShapeType.Sphere;
        cShape.radius = 0.15f;
        SetFxMaterial(chunks, ParticleSystemRenderMode.Mesh);

        // 2) Soft pale puff — a quick cartoon "poof".
        GameObject puffObj = new GameObject("CrashPuff");
        puffObj.transform.SetParent(fxObj.transform, false);
        ParticleSystem puff = puffObj.AddComponent<ParticleSystem>();
        var pMain = puff.main;
        pMain.loop = false;
        pMain.duration = 0.4f;
        pMain.startLifetime = 0.35f;
        pMain.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        pMain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        pMain.startColor = new Color(1f, 0.97f, 0.9f, 0.85f);
        pMain.playOnAwake = false;
        var pEmit = puff.emission;
        pEmit.rateOverTime = 0f;
        pEmit.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
        var pShape = puff.shape;
        pShape.enabled = true;
        pShape.shapeType = ParticleSystemShapeType.Sphere;
        pShape.radius = 0.2f;
        var pSol = puff.sizeOverLifetime;
        pSol.enabled = true;
        pSol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
        SetFxMaterial(puff, ParticleSystemRenderMode.Billboard);

        chunks.Play();
        puff.Play();
        Destroy(fxObj, 2f);
    }

    ParticleSystem.MinMaxGradient SplatGradient(Color baseColor)
    {
        Color light = Color.Lerp(baseColor, Color.white, 0.35f);
        Color dark = Color.Lerp(baseColor, Color.black, 0.25f);
        return new ParticleSystem.MinMaxGradient(dark, light);
    }

    void SetFxMaterial(ParticleSystem ps, ParticleSystemRenderMode mode)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (!r) return;
        r.renderMode = mode;
        if (mode == ParticleSystemRenderMode.Mesh)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.mesh = cube.GetComponent<MeshFilter>().sharedMesh;
            Destroy(cube);
        }
        if (crashFxMaterial) r.material = crashFxMaterial;
    }
}
