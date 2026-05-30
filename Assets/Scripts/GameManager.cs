using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsRunning { get; private set; }

    public enum GameState
    {
        Waiting,
        Running,
        GameOver
    }

    public GameState State { get; private set; } = GameState.Waiting;

    [Header("Speed")]
    public float startSpeed = 10f;
    public float maxSpeed = 22f;
    public float speedRamp = 0.25f; // speed increase per second

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text bestScoreText;
    public TMP_Text distanceText;
    public TMP_Text startPromptText;
    public GameObject gameOverPanel;
    public TMP_Text gameOverScoreText;
    public TMP_Text gameOverBestText;
    public TMP_Text gameOverHintText;
    public TMP_FontAsset uiFont;

    [Header("Farewell Goal")]
    [Tooltip("Seconds until mom's flight boards.")]
    public float timeLimit = 60f;
    [Tooltip("Reach this weight (kg) to keep the dog grounded — over the airline limit. Real airlines cap hold dogs around 32–45kg (American/Air Canada = 100lb/45kg); ~75kg is the upper ceiling.")]
    public float winWeightKg = 75f;
    [Tooltip("Weight the progress bar treats as empty (visual baseline below the start weight).")]
    public float barFloorKg = 30f;
    [Tooltip("Left empty = auto-found on the Player at start.")]
    public DogWeightVisual dogWeight;
    public TMP_Text timerText;
    public TMP_Text weightText;
    [Tooltip("Hide the arcade SCORE/DIST/BEST/TOP HUD — off-theme for the farewell goal.")]
    public bool showScoreHud = false;

    [Header("UI Palette (warm & handmade)")]
    [Tooltip("Main text color — warm cream.")]
    public Color uiTextColor = new Color(0.96f, 0.93f, 0.85f, 1f);
    [Tooltip("Accent (timer, weight, highlights) — amber.")]
    public Color uiAccentColor = new Color(0.98f, 0.70f, 0.27f, 1f);
    [Tooltip("Urgency color when the clock is low — warm red.")]
    public Color uiUrgentColor = new Color(0.95f, 0.36f, 0.30f, 1f);
    [Tooltip("Soft drop-shadow color behind text — warm brown.")]
    public Color uiShadowColor = new Color(0.16f, 0.09f, 0.05f, 0.75f);
    [Tooltip("Weight bar track (empty) color — warm dark.")]
    public Color uiBarTrackColor = new Color(0.20f, 0.14f, 0.10f, 0.85f);
    [Tooltip("Weight bar fill at low fill — soft green.")]
    public Color uiBarLowColor = new Color(0.55f, 0.78f, 0.42f, 1f);
    [Tooltip("Weight bar fill at full — warm gold.")]
    public Color uiBarFullColor = new Color(1f, 0.83f, 0.32f, 1f);

    [Header("Farewell Text")]
    [TextArea] public string startPrompt = "TAP TO START\nDon't let mom catch her flight!";
    [TextArea] public string winTitle = "SHE'S STAYING";
    [TextArea] public string winMessage = "Too chubby to fly.\nMom isn't going anywhere.";
    [TextArea] public string loseTitle = "THE FLIGHT LEFT";
    [TextArea] public string loseMessage = "Not heavy enough in time…\nOne more walk?";

    [Header("Game Over UI")]
    public int gameOverFontSize = 44;
    [Range(8, 64)]
    public int overlayCornerRadius = 24;

    [Header("Supabase (Global Top Score)")]
    public bool enableGlobalTopScore = true;
    [Tooltip("https://<project>.supabase.co")]
    public string supabaseUrl;
    public string supabaseAnonKey;
    public string supabaseTable = "scores";
    public string gameId = "scifirunner";
    public TMP_Text globalBestText;

    [Header("Post Processing")]
    public bool enablePostProcessing = true;
    [Range(0f, 5f)]
    public float bloomIntensity = 1.1f;
    [Range(0f, 1f)]
    public float bloomThreshold = 0.9f;
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.25f;
    [Range(-50f, 50f)]
    public float exposure = 0f;
    [Range(-100f, 100f)]
    public float contrast = 12f;
    [Range(-100f, 100f)]
    public float saturation = 8f;
    [Range(0f, 1f)]
    public float filmGrainIntensity = 0.18f;

    [Header("Atmosphere")]
    public bool enableFog = true;
    public FogMode fogMode = FogMode.ExponentialSquared;
    public Color fogColor = new Color(0.16f, 0.18f, 0.22f, 1f);
    [Range(0f, 0.05f)]
    public float fogDensity = 0.008f;

    [Header("Lighting")]
    public Light keyLight;
    [Range(0f, 5f)]
    public float keyLightIntensity = 1.35f;
    public Color keyLightColor = new Color(0.95f, 0.97f, 1f, 1f);
    public bool autoCreateRimLight = true;
    public Color rimLightColor = new Color(0.35f, 0.7f, 1f, 1f);
    [Range(0f, 2f)]
    public float rimLightIntensity = 0.6f;
    public Vector3 rimLightEuler = new Vector3(18f, 140f, 0f);

    [Header("Shader Fixes")]
    public bool fixMissingShaders = true;
    public Color missingShaderFallbackColor = new Color(0.07f, 0.08f, 0.1f, 1f);

    [Header("Audio")]
    public AudioController audioController;

    [Header("Environment Motion")]
    public bool enableEnvironmentMotion = true;
    public string[] scrollObjectNames = new string[] { "Ground" };
    public float scrollBaseSpeed = 0.35f;
    public float scrollSpeedMultiplier = 0.02f;
    public Vector2 scrollDirection = new Vector2(0f, -1f);
    public Vector2 scrollTiling = new Vector2(1f, 10f);
    public bool randomizeScrollOffset = true;
    public Vector2 scrollOffsetRange = new Vector2(0f, 1f);

    private float score;
    private float distance;
    public float ElapsedTime { get; private set; }

    public float moveSpeed { get; private set; }

    private int bestScore;
    private int globalBestScore;
    private bool globalBestLoaded;
    private float timeRemaining;
    private bool won;
    private TMP_Text gameOverTitleText;
    private const string BestScoreKey = "BestScore";

    private Material[] scrollMaterials;
    private string[] scrollProps;
    private Vector2[] scrollOffsets;
    private Vector2 scrollDirNormalized;
    private Canvas hudCanvas;
    private RectTransform hudSafeArea;
    private Sprite roundedCardSprite;
    private Image weightBarFill;
    private Image weightBarBg;
    private RectTransform weightBarRoot;
    private Sprite barPillSprite;
    private Image timerPill;
    private RectTransform timerPillRoot;
    private Image gameOverAccent;
    private CanvasGroup gameOverGroup;
    private RectTransform gameOverCardRect;
    private float barFillDisplay;
    private float weightPunch;
    private float lastPunchWeight;
    private bool punchInit;
    private Material fallbackMaterial;
    private float shaderFixUntil;
    private float nextShaderFixTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        moveSpeed = startSpeed;
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        EnsureUi();
        EnsureAudio();
        SetupEnvironmentMotion();
        EnsureLighting();
        EnsurePostProcessing();
        ApplyAtmosphere();
        // Run shader fix for a short window after startup to catch spawned objects.
        shaderFixUntil = Time.time + 6f;
        nextShaderFixTime = Time.time;
        FixMissingShaders();
    }

    void Start()
    {
        SetState(GameState.Waiting);
        UpdateUi();
        if (HasSupabaseConfig())
        {
            StartCoroutine(FetchGlobalBest());
        }
    }

    void Update()
    {
        if (fixMissingShaders && Time.time <= shaderFixUntil && Time.time >= nextShaderFixTime)
        {
            FixMissingShaders();
            nextShaderFixTime = Time.time + 0.75f;
        }

        if (State == GameState.Running)
        {
            ElapsedTime += Time.deltaTime;

            moveSpeed = Mathf.Min(maxSpeed, startSpeed + speedRamp * ElapsedTime);

            score += Time.deltaTime;
            distance += moveSpeed * Time.deltaTime;
            UpdateUi();

            UpdateEnvironmentMotion();

            timeRemaining -= Time.deltaTime;
            UpdateGoalUi();

            float weight = dogWeight ? dogWeight.currentWeight : 0f;
            if (weight >= winWeightKg)
            {
                EndGame(true);
            }
            else if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                EndGame(false);
            }
        }
        else if (State == GameState.Waiting)
        {
            if (IsStartInput())
            {
                StartRun();
            }
        }
        else if (State == GameState.GameOver)
        {
            if (IsRestartInput())
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    public void AddScore(float amount)
    {
        score += amount;
    }

    void SetupEnvironmentMotion()
    {
        if (!enableEnvironmentMotion || scrollObjectNames == null || scrollObjectNames.Length == 0)
        {
            return;
        }

        scrollDirNormalized = scrollDirection.sqrMagnitude > 0.0001f
            ? scrollDirection.normalized
            : new Vector2(0f, -1f);

        var mats = new System.Collections.Generic.List<Material>();
        var props = new System.Collections.Generic.List<string>();

        foreach (string name in scrollObjectNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            GameObject obj = GameObject.Find(name);
            if (!obj) continue;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (!renderer) continue;

            Material mat = renderer.material;
            string prop = mat.HasProperty("_BaseMap") ? "_BaseMap" :
                          (mat.HasProperty("_MainTex") ? "_MainTex" : null);

            if (string.IsNullOrEmpty(prop)) continue;

            EnsureScrollingTexture(mat, prop);
            mat.SetTextureScale(prop, scrollTiling);

            mats.Add(mat);
            props.Add(prop);
        }

        scrollMaterials = mats.ToArray();
        scrollProps = props.ToArray();
        scrollOffsets = new Vector2[scrollMaterials.Length];
        if (randomizeScrollOffset)
        {
            for (int i = 0; i < scrollOffsets.Length; i++)
            {
                scrollOffsets[i] = new Vector2(
                    Random.Range(scrollOffsetRange.x, scrollOffsetRange.y),
                    Random.Range(scrollOffsetRange.x, scrollOffsetRange.y)
                );
                scrollMaterials[i].SetTextureOffset(scrollProps[i], scrollOffsets[i]);
            }
        }
    }

    void UpdateEnvironmentMotion()
    {
        if (scrollMaterials == null || scrollMaterials.Length == 0) return;

        float speed = scrollBaseSpeed + moveSpeed * scrollSpeedMultiplier;
        Vector2 delta = scrollDirNormalized * speed * Time.deltaTime;

        for (int i = 0; i < scrollMaterials.Length; i++)
        {
            scrollOffsets[i] += delta;
            scrollMaterials[i].SetTextureOffset(scrollProps[i], scrollOffsets[i]);
        }
    }

    void EnsureScrollingTexture(Material mat, string prop)
    {
        if (mat.GetTexture(prop) != null) return;

        Texture2D tex = new Texture2D(8, 64, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.DontSave;

        Color baseColor = new Color(0.06f, 0.07f, 0.09f, 1f);
        Color stripeColor = new Color(0.12f, 0.7f, 0.95f, 1f);

        for (int y = 0; y < tex.height; y++)
        {
            bool stripe = (y % 16) < 2;
            Color c = stripe ? stripeColor : baseColor;
            for (int x = 0; x < tex.width; x++)
            {
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        mat.SetTexture(prop, tex);
    }

    // Kept for compatibility — a plain "game over" is a loss.
    public void GameOver()
    {
        EndGame(false);
    }

    public void EndGame(bool victory)
    {
        if (State != GameState.Running) return;
        won = victory;
        CommitBestScore();
        TrySubmitGlobalBest();
        UpdateUi();
        UpdateGameOverUi();
        SetState(GameState.GameOver);
        if (gameOverPanel && gameOverPanel.activeInHierarchy)
        {
            StartCoroutine(AnimateGameOverIn());
            if (victory) StartCoroutine(PlayConfetti());
        }
        if (audioController)
        {
            if (victory) audioController.PlayWin();
            else audioController.PlayLose();
        }
    }

    void CommitBestScore()
    {
        int current = Mathf.FloorToInt(score);
        if (current > bestScore)
        {
            bestScore = current;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }
    }

    void UpdateUi()
    {
        int scoreInt = Mathf.FloorToInt(score);
        int bestDisplay = Mathf.Max(bestScore, scoreInt);
        int distanceInt = Mathf.FloorToInt(distance);

        if (scoreText) scoreText.text = "SCORE " + scoreInt.ToString();
        if (bestScoreText) bestScoreText.text = "BEST " + bestDisplay.ToString();
        if (distanceText) distanceText.text = "DIST " + distanceInt.ToString() + "m";
        if (globalBestText)
        {
            globalBestText.text = globalBestLoaded ? "TOP " + globalBestScore.ToString() : "TOP --";
        }
    }

    void UpdateGoalUi()
    {
        bool urgent = timeRemaining <= 10f && timeRemaining > 0f;
        if (timerText)
        {
            int secs = Mathf.CeilToInt(Mathf.Max(0f, timeRemaining));
            timerText.text = secs + "s";
            timerText.color = (timeRemaining <= 10f) ? uiUrgentColor : uiTextColor;
        }
        float pulse = urgent ? (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 12f)) : 1f;
        if (timerText) timerText.rectTransform.localScale = Vector3.one * pulse;
        if (timerPillRoot) timerPillRoot.localScale = Vector3.one * pulse;
        if (timerPill)
        {
            timerPill.color = urgent
                ? Color.Lerp(uiBarTrackColor, new Color(0.42f, 0.12f, 0.10f, 0.92f), 0.7f)
                : uiBarTrackColor;
        }
        float cur = dogWeight ? dogWeight.currentWeight : barFloorKg;

        // Punch the weight number whenever a treat is eaten (weight ticks up).
        if (!punchInit) { lastPunchWeight = cur; punchInit = true; }
        if (cur > lastPunchWeight + 0.01f) weightPunch = 1f;
        lastPunchWeight = cur;
        weightPunch = Mathf.MoveTowards(weightPunch, 0f, Time.deltaTime * 3.5f);

        if (weightText)
        {
            weightText.text = Mathf.RoundToInt(cur) + " / " + Mathf.RoundToInt(winWeightKg) + " kg";
            weightText.color = uiAccentColor;
            weightText.rectTransform.localScale = Vector3.one * (1f + 0.22f * weightPunch);
        }
        if (weightBarFill)
        {
            float targetT = Mathf.Clamp01(Mathf.InverseLerp(barFloorKg, winWeightKg, cur));
            barFillDisplay = Mathf.Lerp(barFillDisplay, targetT, Time.deltaTime * 6f);
            weightBarFill.fillAmount = barFillDisplay;
            weightBarFill.color = Color.Lerp(uiBarLowColor, uiBarFullColor, barFillDisplay);
        }
    }

    void EnsureUi()
    {
        hudCanvas = EnsureHudCanvas();
        if (!hudCanvas) return;

        RectTransform hudParent = EnsureSafeArea();
        if (!hudParent) return;

        EnsureScoreHud(hudParent);

        if (!bestScoreText)
        {
            bestScoreText = CreateUiText(hudParent, "BestText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -110f), new Vector2(260f, 60f), 28, TextAlignmentOptions.TopRight);
        }
        else if (bestScoreText.transform.parent != hudParent)
        {
            bestScoreText.transform.SetParent(hudParent, false);
        }
        else
        {
            RectTransform rect = bestScoreText.rectTransform;
            rect.anchoredPosition = new Vector2(-32f, -110f);
        }
        StyleHudText(bestScoreText, TextAlignmentOptions.TopRight);

        if (!distanceText)
        {
            distanceText = CreateUiText(hudParent, "DistanceText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -110f), new Vector2(260f, 60f), 36, TextAlignmentOptions.TopLeft);
        }
        else if (distanceText.transform.parent != hudParent)
        {
            distanceText.transform.SetParent(hudParent, false);
        }
        else
        {
            RectTransform rect = distanceText.rectTransform;
            rect.anchoredPosition = new Vector2(32f, -110f);
            distanceText.fontSize = 36;
        }
        StyleHudText(distanceText, TextAlignmentOptions.TopLeft);

        if (!globalBestText)
        {
            globalBestText = CreateUiText(hudParent, "GlobalBestText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(260f, 60f), 30, TextAlignmentOptions.Top);
        }
        else if (globalBestText.transform.parent != hudParent)
        {
            globalBestText.transform.SetParent(hudParent, false);
        }
        else
        {
            RectTransform rect = globalBestText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(260f, 60f);
            globalBestText.fontSize = 30;
        }
        StyleHudText(globalBestText, TextAlignmentOptions.Top);

        if (!timerText)
        {
            timerText = CreateUiText(hudParent, "TimerText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 90f), 64, TextAlignmentOptions.Center);
        }
        StyleHudText(timerText, TextAlignmentOptions.Center);
        EnsureTimerPill();

        if (!weightText)
        {
            weightText = CreateUiText(hudParent, "WeightText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(540f, 42f), 30, TextAlignmentOptions.Top);
        }
        StyleHudText(weightText, TextAlignmentOptions.Top);

        EnsureWeightBar(hudParent);

        if (!startPromptText)
        {
            startPromptText = CreateUiText(hudParent, "StartPrompt", new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(0f, 420f), 78, TextAlignmentOptions.Center);
        }
        else if (startPromptText.transform.parent != hudParent)
        {
            startPromptText.transform.SetParent(hudParent, false);
        }
        // Always apply layout (regardless of create / reparent path): a box that
        // stretches 10%-90% of screen width so it can never run off the edges,
        // with word wrap + auto-sizing keeping the copy inside it.
        {
            RectTransform rect = startPromptText.rectTransform;
            rect.anchorMin = new Vector2(0.1f, 0.5f);
            rect.anchorMax = new Vector2(0.9f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 180f);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 420f);
        }
        // Two-tier mobile CTA: big bold title, smaller subtitle beneath.
        startPromptText.text = BuildStartPromptRichText(startPrompt);
        startPromptText.enableAutoSizing = true;
        startPromptText.fontSizeMin = 30f;
        startPromptText.fontSizeMax = 86f;
        startPromptText.lineSpacing = 8f;
        StyleHudText(startPromptText, TextAlignmentOptions.Center, true);

        if (!showScoreHud)
        {
            if (scoreText) scoreText.gameObject.SetActive(false);
            if (bestScoreText) bestScoreText.gameObject.SetActive(false);
            if (distanceText) distanceText.gameObject.SetActive(false);
            if (globalBestText) globalBestText.gameObject.SetActive(false);
        }

        EnsureGameOverUi(hudCanvas.transform);
    }

    TMP_Text CreateUiText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        if (scoreText)
        {
            tmp.font = scoreText.font;
            tmp.color = scoreText.color;
        }
        else
        {
            tmp.color = Color.white;
        }
        ApplyFont(tmp);

        return tmp;
    }

    void EnsureScoreHud(Transform parent)
    {
        if (!parent) return;

        GameObject scorePanel = GameObject.Find("ScoreHud");
        if (!scorePanel)
        {
            scorePanel = new GameObject("ScoreHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            scorePanel.layer = parent.gameObject.layer;
            scorePanel.transform.SetParent(parent, false);

            RectTransform rect = scorePanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(520f, 84f);

            Image img = scorePanel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.45f);
        }
        else if (scorePanel.transform.parent != parent)
        {
            scorePanel.transform.SetParent(parent, false);
        }

        if (!scoreText || scoreText.gameObject.name == "Text (TMP)")
        {
            if (scoreText && scoreText.gameObject.name == "Text (TMP)")
            {
                scoreText.gameObject.SetActive(false);
            }
            scoreText = scorePanel.GetComponentInChildren<TMP_Text>();
            if (!scoreText)
            {
                scoreText = CreateUiText(scorePanel.transform, "ScoreText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 80f), 74, TextAlignmentOptions.Center);
            }
        }

        if (scoreText)
        {
            scoreText.fontSize = 96;
            scoreText.text = "SCORE 0";
            scoreText.gameObject.SetActive(true);
            StyleHudText(scoreText, TextAlignmentOptions.Center);
            scoreText.transform.SetAsLastSibling();
        }
    }

    Canvas EnsureHudCanvas()
    {
        if (hudCanvas) return hudCanvas;

        GameObject existing = GameObject.Find("HudCanvas");
        if (existing)
        {
            hudCanvas = existing.GetComponent<Canvas>();
            if (hudCanvas) return hudCanvas;
        }

        GameObject hud = new GameObject("HudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        hudCanvas = hud.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 100;

        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = hud.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;

        return hudCanvas;
    }

    RectTransform EnsureSafeArea()
    {
        if (!hudCanvas) return null;

        if (!hudSafeArea)
        {
            GameObject existing = GameObject.Find("HudSafeArea");
            if (existing)
            {
                hudSafeArea = existing.GetComponent<RectTransform>();
            }
        }

        if (!hudSafeArea)
        {
            GameObject safe = new GameObject("HudSafeArea", typeof(RectTransform));
            safe.transform.SetParent(hudCanvas.transform, false);
            hudSafeArea = safe.GetComponent<RectTransform>();
        }

        ApplySafeArea(hudSafeArea);
        return hudSafeArea;
    }

    void ApplySafeArea(RectTransform rect)
    {
        if (!rect) return;
        Rect safe = Screen.safeArea;
        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;

        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    void StyleHudText(TMP_Text text, TextAlignmentOptions alignment, bool wrap = false)
    {
        if (!text) return;
        text.alignment = alignment;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.fontStyle = FontStyles.Normal;
        text.color = uiTextColor;
        text.outlineWidth = 0f;
        text.raycastTarget = false;
        ApplyFont(text);
        ApplySoftShadow(text);
    }

    // First line = big bold title, remaining lines = smaller subtitle.
    string BuildStartPromptRichText(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        int nl = raw.IndexOf('\n');
        if (nl < 0) return "<b>" + raw + "</b>";
        string title = raw.Substring(0, nl).Trim();
        string sub = raw.Substring(nl + 1).Trim();
        return "<b>" + title + "</b>\n<size=55%>" + sub + "</size>";
    }

    // Warm soft drop shadow via the TMP underlay feature (replaces the old
    // hard black outline). Font-agnostic — works with whatever uiFont is set.
    void ApplySoftShadow(TMP_Text text)
    {
        if (!text) return;
        Material mat = text.fontMaterial; // instanced copy, per-text
        if (!mat) return;
        mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, uiShadowColor);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.5f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);
    }

    void ApplyFont(TMP_Text text)
    {
        if (!text || !uiFont) return;
        text.font = uiFont;
    }

    void EnsureLighting()
    {
        if (!keyLight)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    keyLight = l;
                    break;
                }
            }
        }

        if (keyLight)
        {
            keyLight.intensity = keyLightIntensity;
            keyLight.color = keyLightColor;
        }

        if (autoCreateRimLight && !GameObject.Find("RimLight"))
        {
            GameObject rim = new GameObject("RimLight");
            Light rimLight = rim.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = rimLightColor;
            rimLight.intensity = rimLightIntensity;
            rim.transform.rotation = Quaternion.Euler(rimLightEuler);
        }
    }

    void ApplyAtmosphere()
    {
        if (!enableFog)
        {
            RenderSettings.fog = false;
            return;
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.ambientMode = AmbientMode.Skybox;
    }

    void EnsurePostProcessing()
    {
        if (!enablePostProcessing) return;

        Volume volume = FindObjectOfType<Volume>();
        if (!volume)
        {
            GameObject volObj = new GameObject("GlobalVolume");
            volume = volObj.AddComponent<Volume>();
        }

        volume.isGlobal = true;
        if (!volume.profile)
        {
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        VolumeProfile profile = volume.profile;

        if (!profile.TryGet(out Bloom bloom))
        {
            bloom = profile.Add<Bloom>(true);
        }
        bloom.intensity.value = bloomIntensity;
        bloom.threshold.value = bloomThreshold;

        if (!profile.TryGet(out ColorAdjustments color))
        {
            color = profile.Add<ColorAdjustments>(true);
        }
        color.postExposure.value = exposure;
        color.contrast.value = contrast;
        color.saturation.value = saturation;

        if (!profile.TryGet(out Vignette vignette))
        {
            vignette = profile.Add<Vignette>(true);
        }
        vignette.intensity.value = vignetteIntensity;
        vignette.smoothness.value = 0.6f;

        if (!profile.TryGet(out FilmGrain grain))
        {
            grain = profile.Add<FilmGrain>(true);
        }
        grain.type.value = FilmGrainLookup.Thin1;
        grain.intensity.value = filmGrainIntensity;
    }

    void FixMissingShaders()
    {
        if (!fixMissingShaders) return;

        Material fallback = GetFallbackMaterial();
        if (!fallback) return;

        Renderer[] renderers = FindObjectsOfType<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (!renderer) continue;
            Material[] shared = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < shared.Length; i++)
            {
                Material mat = shared[i];
                if (IsErrorShader(mat))
                {
                    shared[i] = fallback;
                    changed = true;
                }
            }
            if (changed)
            {
                renderer.sharedMaterials = shared;
            }
        }
    }

    bool IsErrorShader(Material mat)
    {
        if (!mat) return true;
        if (!mat.shader) return true;
        string name = mat.shader.name;
        if (name == "Hidden/InternalErrorShader") return true;
        if (name.StartsWith("SyntyStudios/", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    Material GetFallbackMaterial()
    {
        if (fallbackMaterial) return fallbackMaterial;

        // Try to find a road material from the POLYGON pack
        Material[] allMats = Resources.FindObjectsOfTypeAll<Material>();
        for (int i = 0; i < allMats.Length; i++)
        {
            Material mat = allMats[i];
            if (!mat || !mat.shader) continue;
            if (mat.name == "Road" || mat.name == "Road 1" || mat.name.Contains("Road"))
            {
                if (mat.shader.name != "Hidden/InternalErrorShader")
                {
                    fallbackMaterial = mat;
                    return fallbackMaterial;
                }
            }
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        if (!shader) return null;

        fallbackMaterial = new Material(shader);
        fallbackMaterial.hideFlags = HideFlags.DontSave;
        if (fallbackMaterial.HasProperty("_BaseColor"))
        {
            fallbackMaterial.SetColor("_BaseColor", missingShaderFallbackColor);
        }
        if (fallbackMaterial.HasProperty("_Color"))
        {
            fallbackMaterial.SetColor("_Color", missingShaderFallbackColor);
        }
        return fallbackMaterial;
    }

    bool HasSupabaseConfig()
    {
        return enableGlobalTopScore &&
               !string.IsNullOrEmpty(supabaseUrl) &&
               !string.IsNullOrEmpty(supabaseAnonKey);
    }

    void TrySubmitGlobalBest()
    {
        if (!HasSupabaseConfig()) return;
        int scoreInt = Mathf.FloorToInt(score);
        if (globalBestLoaded && scoreInt <= globalBestScore) return;
        StartCoroutine(PostScore(scoreInt, Mathf.FloorToInt(distance)));
    }

    IEnumerator FetchGlobalBest()
    {
        string url = supabaseUrl.TrimEnd('/') +
                     "/rest/v1/" + supabaseTable +
                     "?select=score&game_id=eq." + UnityWebRequest.EscapeURL(gameId) +
                     "&order=score.desc&limit=1";

        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("apikey", supabaseAnonKey);
        req.SetRequestHeader("Authorization", "Bearer " + supabaseAnonKey);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Supabase top score fetch failed: " + req.error);
            yield break;
        }

        var rows = ParseScoreRows(req.downloadHandler.text);
        if (rows != null && rows.Length > 0)
        {
            globalBestScore = Mathf.Max(globalBestScore, rows[0].score);
            globalBestLoaded = true;
            UpdateUi();
        }
    }

    IEnumerator PostScore(int scoreInt, int distanceInt)
    {
        string url = supabaseUrl.TrimEnd('/') + "/rest/v1/" + supabaseTable;
        string body = "{\"game_id\":\"" + EscapeJson(gameId) + "\",\"score\":" + scoreInt + ",\"distance\":" + distanceInt + "}";

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", supabaseAnonKey);
        req.SetRequestHeader("Authorization", "Bearer " + supabaseAnonKey);
        req.SetRequestHeader("Prefer", "return=minimal");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Supabase score insert failed: " + req.error);
            yield break;
        }

        if (scoreInt > globalBestScore)
        {
            globalBestScore = scoreInt;
            globalBestLoaded = true;
            UpdateUi();
        }
    }

    [Serializable]
    class ScoreRow
    {
        public int score;
    }

    [Serializable]
    class ScoreWrapper
    {
        public ScoreRow[] items;
    }

    static ScoreRow[] ParseScoreRows(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]") return Array.Empty<ScoreRow>();
        string wrapped = "{\"items\":" + json + "}";
        ScoreWrapper wrapper = JsonUtility.FromJson<ScoreWrapper>(wrapped);
        return wrapper != null ? wrapper.items : Array.Empty<ScoreRow>();
    }

    static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    void EnsureTimerPill()
    {
        if (timerPill || !timerText) return;
        if (!barPillSprite) barPillSprite = CreateRoundedSprite(64, 32, 16);

        GameObject pill = new GameObject("TimerPill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pill.transform.SetParent(timerText.transform.parent, false);
        timerPillRoot = pill.GetComponent<RectTransform>();
        timerPillRoot.anchorMin = new Vector2(0.5f, 1f);
        timerPillRoot.anchorMax = new Vector2(0.5f, 1f);
        timerPillRoot.pivot = new Vector2(0.5f, 1f);
        timerPillRoot.anchoredPosition = new Vector2(0f, -37f);
        timerPillRoot.sizeDelta = new Vector2(200f, 96f);

        timerPill = pill.GetComponent<Image>();
        timerPill.raycastTarget = false;
        timerPill.color = uiBarTrackColor;
        if (barPillSprite) { timerPill.sprite = barPillSprite; timerPill.type = Image.Type.Sliced; }

        // Render behind the timer text.
        timerPillRoot.SetSiblingIndex(timerText.transform.GetSiblingIndex());
    }

    void EnsureWeightBar(RectTransform parent)
    {
        if (weightBarFill) return;
        if (!barPillSprite) barPillSprite = CreateRoundedSprite(64, 32, 16);

        GameObject root = new GameObject("WeightBar", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        weightBarRoot = root.GetComponent<RectTransform>();
        weightBarRoot.anchorMin = new Vector2(0.5f, 1f);
        weightBarRoot.anchorMax = new Vector2(0.5f, 1f);
        weightBarRoot.pivot = new Vector2(0.5f, 1f);
        weightBarRoot.anchoredPosition = new Vector2(0f, -184f);
        weightBarRoot.sizeDelta = new Vector2(560f, 42f);

        GameObject track = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        track.transform.SetParent(root.transform, false);
        RectTransform trackRt = track.GetComponent<RectTransform>();
        trackRt.anchorMin = Vector2.zero;
        trackRt.anchorMax = Vector2.one;
        trackRt.offsetMin = Vector2.zero;
        trackRt.offsetMax = Vector2.zero;
        weightBarBg = track.GetComponent<Image>();
        weightBarBg.raycastTarget = false;
        weightBarBg.color = uiBarTrackColor;
        if (barPillSprite) { weightBarBg.sprite = barPillSprite; weightBarBg.type = Image.Type.Sliced; }

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(root.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);
        weightBarFill = fill.GetComponent<Image>();
        weightBarFill.raycastTarget = false;
        weightBarFill.color = uiAccentColor;
        if (barPillSprite) weightBarFill.sprite = barPillSprite;
        weightBarFill.type = Image.Type.Filled;
        weightBarFill.fillMethod = Image.FillMethod.Horizontal;
        weightBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        weightBarFill.fillAmount = 0f;
    }

    void ApplyRoundedCard(Image img)
    {
        if (!img) return;
        if (!roundedCardSprite)
        {
            roundedCardSprite = CreateRoundedSprite(256, 128, overlayCornerRadius);
        }
        if (roundedCardSprite)
        {
            img.sprite = roundedCardSprite;
            img.type = Image.Type.Sliced;
        }
    }

    Sprite CreateRoundedSprite(int width, int height, int radius)
    {
        int r = Mathf.Clamp(radius, 2, Mathf.Min(width, height) / 2);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.DontSave;

        Color solid = Color.white;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool inCorner =
                    (x < r && y < r) ||
                    (x < r && y >= height - r) ||
                    (x >= width - r && y < r) ||
                    (x >= width - r && y >= height - r);

                if (!inCorner)
                {
                    tex.SetPixel(x, y, solid);
                    continue;
                }

                int cx = x < r ? r : width - r - 1;
                int cy = y < r ? r : height - r - 1;
                int dx = x - cx;
                int dy = y - cy;
                bool inside = (dx * dx + dy * dy) <= (r * r);
                tex.SetPixel(x, y, inside ? solid : clear);
            }
        }

        tex.Apply();

        Vector4 border = new Vector4(r, r, r, r);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    void EnsureGameOverUi(Transform canvas)
    {
        if (gameOverPanel) return;

        GameObject panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = canvas.gameObject.layer;
        panel.transform.SetParent(canvas, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = panel.GetComponent<Image>();
        img.color = new Color(0.05f, 0.03f, 0.02f, 0.80f);
        gameOverGroup = panel.AddComponent<CanvasGroup>();

        GameObject card = new GameObject("GameOverCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.layer = canvas.gameObject.layer;
        card.transform.SetParent(panel.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(700f, 360f);
        gameOverCardRect = cardRect;

        Image cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.15f, 0.10f, 0.07f, 0.98f);

        gameOverTitleText = CreateUiText(card.transform, "GameOverTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(600f, 80f), gameOverFontSize, TextAlignmentOptions.Center);
        gameOverTitleText.text = "GAME OVER";
        StyleHudText(gameOverTitleText, TextAlignmentOptions.Center);

        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divider.transform.SetParent(card.transform, false);
        RectTransform divRt = divider.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 0.5f);
        divRt.anchorMax = new Vector2(0.5f, 0.5f);
        divRt.pivot = new Vector2(0.5f, 0.5f);
        divRt.anchoredPosition = new Vector2(0f, 84f);
        divRt.sizeDelta = new Vector2(120f, 5f);
        gameOverAccent = divider.GetComponent<Image>();
        gameOverAccent.raycastTarget = false;
        gameOverAccent.color = uiAccentColor;
        if (!barPillSprite) barPillSprite = CreateRoundedSprite(64, 32, 16);
        if (barPillSprite) { gameOverAccent.sprite = barPillSprite; gameOverAccent.type = Image.Type.Sliced; }

        gameOverScoreText = CreateUiText(card.transform, "GameOverScore", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(620f, 90f), gameOverFontSize, TextAlignmentOptions.Center);
        gameOverBestText = CreateUiText(card.transform, "GameOverBest", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(600f, 50f), gameOverFontSize, TextAlignmentOptions.Center);
        gameOverHintText = CreateUiText(card.transform, "GameOverHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(600f, 60f), gameOverFontSize, TextAlignmentOptions.Center);
        gameOverHintText.text = "TAP TO RESTART";
        StyleHudText(gameOverScoreText, TextAlignmentOptions.Center);
        StyleHudText(gameOverBestText, TextAlignmentOptions.Center);
        StyleHudText(gameOverHintText, TextAlignmentOptions.Center);

        ApplyRoundedCard(cardImg);

        gameOverPanel = panel;
        gameOverPanel.SetActive(false);
    }

    System.Collections.IEnumerator AnimateGameOverIn()
    {
        float dur = 0.38f;
        float t = 0f;
        if (gameOverGroup) gameOverGroup.alpha = 0f;
        if (gameOverCardRect) gameOverCardRect.localScale = Vector3.one * 0.85f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            if (gameOverGroup) gameOverGroup.alpha = p;
            if (gameOverCardRect) gameOverCardRect.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, EaseOutBack(p));
            yield return null;
        }
        if (gameOverGroup) gameOverGroup.alpha = 1f;
        if (gameOverCardRect) gameOverCardRect.localScale = Vector3.one;
    }

    System.Collections.IEnumerator PlayConfetti()
    {
        if (!gameOverPanel) yield break;
        if (!barPillSprite) barPillSprite = CreateRoundedSprite(64, 32, 16);

        Color[] cols = { uiBarFullColor, uiAccentColor, new Color(1f, 0.45f, 0.45f), new Color(0.55f, 0.8f, 1f), Color.white };
        const int n = 30;
        var rts = new RectTransform[n];
        var vel = new Vector2[n];
        var spin = new float[n];
        var imgs = new Image[n];

        for (int i = 0; i < n; i++)
        {
            GameObject go = new GameObject("Confetti", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(gameOverPanel.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Random.Range(10f, 20f), Random.Range(14f, 28f));
            rt.anchoredPosition = new Vector2(Random.Range(-140f, 140f), Random.Range(120f, 200f));
            Image img = go.GetComponent<Image>();
            img.sprite = barPillSprite;
            img.color = cols[Random.Range(0, cols.Length)];
            img.raycastTarget = false;
            rts[i] = rt; imgs[i] = img;
            vel[i] = new Vector2(Random.Range(-300f, 300f), Random.Range(300f, 620f));
            spin[i] = Random.Range(-360f, 360f);
        }

        float t = 0f;
        const float dur = 1.8f;
        const float grav = 1100f;
        while (t < dur)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            for (int i = 0; i < n; i++)
            {
                if (!rts[i]) continue;
                vel[i].y -= grav * dt;
                rts[i].anchoredPosition += vel[i] * dt;
                rts[i].Rotate(0f, 0f, spin[i] * dt);
                Color c = imgs[i].color;
                c.a = Mathf.Clamp01(1f - t / dur);
                imgs[i].color = c;
            }
            yield return null;
        }
        for (int i = 0; i < n; i++) if (rts[i]) Destroy(rts[i].gameObject);
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    void UpdateGameOverUi()
    {
        int finalWeight = dogWeight ? Mathf.RoundToInt(dogWeight.currentWeight) : 0;

        Color outcomeColor = won ? uiBarFullColor : uiUrgentColor;
        if (gameOverTitleText)
        {
            gameOverTitleText.text = won ? winTitle : loseTitle;
            gameOverTitleText.color = outcomeColor;
            ApplyFont(gameOverTitleText);
        }
        if (gameOverAccent) gameOverAccent.color = outcomeColor;
        if (gameOverScoreText)
        {
            gameOverScoreText.text = won ? winMessage : loseMessage;
            gameOverScoreText.fontSize = Mathf.RoundToInt(gameOverFontSize * 0.7f);
            ApplyFont(gameOverScoreText);
        }
        if (gameOverBestText)
        {
            gameOverBestText.text = finalWeight + " kg";
            gameOverBestText.fontSize = Mathf.RoundToInt(gameOverFontSize * 0.65f);
            gameOverBestText.color = uiAccentColor;
            ApplyFont(gameOverBestText);
        }
        if (gameOverHintText)
        {
            gameOverHintText.text = won ? "TAP TO PLAY AGAIN" : "TAP TO TRY AGAIN";
            gameOverHintText.fontSize = Mathf.RoundToInt(gameOverFontSize * 0.6f);
            ApplyFont(gameOverHintText);
        }
    }

    void StartRun()
    {
        score = 0f;
        distance = 0f;
        ElapsedTime = 0f;
        moveSpeed = startSpeed;
        won = false;
        timeRemaining = timeLimit;
        if (!dogWeight) dogWeight = FindObjectOfType<DogWeightVisual>();
        float startW = dogWeight ? dogWeight.currentWeight : barFloorKg;
        barFillDisplay = Mathf.Clamp01(Mathf.InverseLerp(barFloorKg, winWeightKg, startW));
        lastPunchWeight = startW;
        weightPunch = 0f;
        punchInit = true;
        UpdateUi();
        UpdateGoalUi();
        SetState(GameState.Running);
    }

    void SetState(GameState newState)
    {
        State = newState;
        IsRunning = (State == GameState.Running);

        if (startPromptText) startPromptText.gameObject.SetActive(State == GameState.Waiting);
        if (gameOverPanel) gameOverPanel.SetActive(State == GameState.GameOver);
        if (timerText) timerText.gameObject.SetActive(IsRunning);
        if (timerPillRoot) timerPillRoot.gameObject.SetActive(IsRunning);
        if (weightText) weightText.gameObject.SetActive(IsRunning);
        if (weightBarRoot) weightBarRoot.gameObject.SetActive(IsRunning);
        if (audioController) audioController.SetRunning(IsRunning);
    }

    bool IsStartInput()
    {
        return Input.GetMouseButtonDown(0) ||
               Input.GetKeyDown(KeyCode.Space) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    bool IsRestartInput()
    {
        return Input.GetMouseButtonDown(0) ||
               Input.GetKeyDown(KeyCode.R) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    void EnsureAudio()
    {
        if (!audioController)
        {
            audioController = FindObjectOfType<AudioController>();
        }
        if (!audioController)
        {
            audioController = gameObject.AddComponent<AudioController>();
        }
    }
}
