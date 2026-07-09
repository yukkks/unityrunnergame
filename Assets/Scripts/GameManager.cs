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
    [Tooltip("Serif display font (Fraunces) for card titles and verdict moments; matches the website. Body/HUD text stays on uiFont.")]
    public TMP_FontAsset uiDisplayFont;

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
    public Color uiBarTrackColor = new Color(0.15f, 0.10f, 0.07f, 0.94f);
    [Tooltip("Weight bar fill at low fill — soft green.")]
    public Color uiBarLowColor = new Color(0.46f, 0.83f, 0.40f, 1f);
    [Tooltip("Weight bar fill at full — warm gold.")]
    public Color uiBarFullColor = new Color(1f, 0.80f, 0.30f, 1f);

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
    [Range(0f, 1f)]
    public float keyLightShadowStrength = 0.35f;
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
    private RectTransform weightBarFillRect;
    private const float WeightBarInset = 6f;
    private Sprite barPillSprite;
    private Image timerPill;
    private RectTransform timerPillRoot;
    private RectTransform rulesRoot;
    private RectTransform rulesCtaPill;
    private Image gameOverAccent;
    private CanvasGroup gameOverGroup;
    private RectTransform gameOverCardRect;
    private Image gameOverIconBadge;
    private Image gameOverIconPaw;
    private RawImage gameOverWinImage;
    private TMP_Text gameOverNumber;
    private Image gameOverButtonBg;
    private float barFillDisplay;
    private float weightPunch;
    private float lastPunchWeight;
    private float weightHitFlash;      // 0..1 red flash + shake when Kenzo is hit
    private Vector2 weightBarBasePos;  // resting position of the bar (for shake)
    private bool weightBarBaseCaptured;
    private int lastMilestone = int.MinValue; // last 10kg mark Kenzo celebrated
    private Vignette runtimeVignette;  // cached so the timer can pulse it red
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
        NotifyHostReady();
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
            if (rulesCtaPill)
            {
                float s = 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 4f);
                rulesCtaPill.localScale = new Vector3(s, s, 1f);
            }
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

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void NotifyGameResult(string result);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void NotifyGameReady();
#endif

    // Tell the host page the game is interactive so it can lift its loading
    // veil at the right moment (the iframe 'load' event fires far too early).
    void NotifyHostReady()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { NotifyGameReady(); } catch { }
#else
        Debug.Log("[GameReady]");
#endif
    }

    public void EndGame(bool victory)
    {
        if (State != GameState.Running) return;
        won = victory;
        CommitBestScore();
        TrySubmitGlobalBest();
        UpdateUi();
        SetState(GameState.GameOver);

        // The website owns the ending: tell the host page win/lose so it can
        // play the outro video. The in-game card is suppressed in WebGL builds.
        NotifyHost(victory ? "win" : "lose");

        if (audioController)
        {
            if (victory) audioController.PlayWin();
            else audioController.PlayLose();
#if UNITY_WEBGL && !UNITY_EDITOR
            // The website's outro video owns the soundtrack from here — let the
            // short stinger ring out but stop the music bed underneath it.
            audioController.SetBgmEnabled(false);
#endif
        }
    }

    void NotifyHost(string result)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { NotifyGameResult(result); } catch { }
#else
        Debug.Log("[GameResult] " + result);
        // In the editor, still show the card so we can test outcomes locally.
        UpdateGameOverUi();
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            if (gameOverPanel.activeInHierarchy)
            {
                StartCoroutine(AnimateGameOverIn());
                if (result == "win") StartCoroutine(PlayConfetti());
            }
        }
#endif
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

        // Screen-edge red vignette pulse in the final seconds — panic you feel
        // peripherally rather than read.
        if (runtimeVignette != null)
        {
            if (urgent)
            {
                float vp = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                runtimeVignette.intensity.value = Mathf.Lerp(vignetteIntensity, 0.52f, vp);
                runtimeVignette.color.value = Color.Lerp(Color.black, new Color(0.72f, 0.10f, 0.08f), vp);
            }
            else
            {
                runtimeVignette.intensity.value = vignetteIntensity;
                runtimeVignette.color.value = Color.black;
            }
        }

        float cur = dogWeight ? dogWeight.currentWeight : barFloorKg;

        // Punch the weight number whenever a treat is eaten (weight ticks up).
        if (!punchInit) { lastPunchWeight = cur; punchInit = true; }
        if (cur > lastPunchWeight + 0.01f) weightPunch = 1f;
        lastPunchWeight = cur;
        weightPunch = Mathf.MoveTowards(weightPunch, 0f, Time.deltaTime * 3.5f);
        weightHitFlash = Mathf.MoveTowards(weightHitFlash, 0f, Time.deltaTime * 2.2f);

        // Kenzo does a happy hop + bark each time he crosses a new 10 kg mark.
        int milestone = Mathf.FloorToInt(cur / 10f);
        if (lastMilestone == int.MinValue) lastMilestone = milestone;
        else if (milestone > lastMilestone) { lastMilestone = milestone; CelebrateMilestone(); }

        if (weightText)
        {
            weightText.text = Mathf.RoundToInt(cur) + " / " + Mathf.RoundToInt(winWeightKg) + " kg";
            weightText.color = Color.Lerp(uiTextColor, uiUrgentColor, weightHitFlash);
            weightText.rectTransform.localScale = Vector3.one * (1f + 0.22f * weightPunch + 0.12f * weightHitFlash);
        }
        if (weightBarFill && weightBarFillRect && weightBarRoot)
        {
            if (!weightBarBaseCaptured) { weightBarBasePos = weightBarRoot.anchoredPosition; weightBarBaseCaptured = true; }

            float targetT = Mathf.Clamp01(Mathf.InverseLerp(barFloorKg, winWeightKg, cur));
            barFillDisplay = Mathf.Lerp(barFillDisplay, targetT, Time.deltaTime * 6f);

            // Animate WIDTH (not fillAmount) so rounded ends stay clean. Keep a
            // minimum so an empty-ish bar still reads as a rounded nub, not a sliver.
            float innerW = weightBarRoot.rect.width - 2f * WeightBarInset;
            float fillH = weightBarRoot.rect.height - 2f * WeightBarInset;
            float w = Mathf.Lerp(fillH, innerW, barFillDisplay); // fillH == pill cap diameter
            weightBarFillRect.sizeDelta = new Vector2(w, fillH);

            // Soft pulse on a treat; a sharper punch + shake when Kenzo is hit.
            weightBarRoot.localScale = Vector3.one * (1f + 0.05f * weightPunch + 0.06f * weightHitFlash);
            Vector2 shake = weightHitFlash > 0.001f
                ? new Vector2(Mathf.Sin(Time.unscaledTime * 90f), Mathf.Cos(Time.unscaledTime * 75f)) * 7f * weightHitFlash
                : Vector2.zero;
            weightBarRoot.anchoredPosition = weightBarBasePos + shake;

            // Green -> gold as it approaches the goal; flare brighter near full.
            Color baseCol = Color.Lerp(uiBarLowColor, uiBarFullColor, barFillDisplay);
            if (barFillDisplay > 0.85f)
            {
                float flare = (barFillDisplay - 0.85f) / 0.15f;
                baseCol = Color.Lerp(baseCol, Color.white, 0.25f * flare * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f)));
            }
            // Flash the fill red on a hit.
            weightBarFill.color = Color.Lerp(baseCol, uiUrgentColor, 0.85f * weightHitFlash);
        }
    }

    // Floating "+2 kg" / "-8 kg" that pops at the point of the chomp/hit and
    // drifts up as it fades — the moment-to-moment read of the game's stakes.
    public void ShowWeightDelta(float amount, Vector3 worldPos)
    {
        if (Mathf.Abs(amount) < 0.01f) return;
        if (amount < 0f) weightHitFlash = 1f; // drive the bar red-flash + shake

        if (!hudCanvas) return;
        Camera cam = Camera.main;
        if (!cam) return;
        RectTransform canvasRect = hudCanvas.transform as RectTransform;
        if (!canvasRect) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPos + Vector3.up * 1.0f);
        if (screen.z < 0f) return; // behind the camera
        Camera uiCam = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCam, out local)) return;

        bool gain = amount > 0f;
        TMP_Text t = CreateUiText(canvasRect, "WeightDelta",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            local, new Vector2(220f, 84f), 54, TextAlignmentOptions.Center);
        t.text = (gain ? "+" : "") + Mathf.RoundToInt(amount) + " kg";
        t.color = gain ? uiBarLowColor : uiUrgentColor;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        ApplyFont(t);
        ApplyTextOutline(t, uiShadowColor, 0.25f);
        StartCoroutine(FloatDelta(t, local));
    }

    System.Collections.IEnumerator FloatDelta(TMP_Text t, Vector2 startLocal)
    {
        if (!t) yield break;
        RectTransform rt = t.rectTransform;
        Color c = t.color;
        float dur = 0.85f, el = 0f;
        while (el < dur && t)
        {
            float p = el / dur;
            rt.anchoredPosition = startLocal + new Vector2(0f, 72f * p);
            float pop = p < 0.18f ? Mathf.Lerp(0.5f, 1.18f, p / 0.18f) : Mathf.Lerp(1.18f, 1f, (p - 0.18f) / 0.82f);
            rt.localScale = Vector3.one * pop;
            c.a = 1f - Mathf.Clamp01((p - 0.45f) / 0.55f);
            t.color = c;
            el += Time.unscaledDeltaTime;
            yield return null;
        }
        if (t) Destroy(t.gameObject);
    }

    void CelebrateMilestone()
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc) pc.Celebrate();
        if (audioController) audioController.PlayBark();
    }

    // A one-time "swipe to move" hint at the start of each run — teaches the
    // core control by showing, then fades so it never nags.
    public void ShowSwipeHint()
    {
        StartCoroutine(SwipeHintRoutine());
    }

    System.Collections.IEnumerator SwipeHintRoutine()
    {
        if (!hudCanvas) yield break;
        Transform parent = hudSafeArea ? (Transform)hudSafeArea : hudCanvas.transform;
        TMP_Text hint = CreateUiText(parent, "SwipeHint",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 300f), new Vector2(760f, 96f), 46, TextAlignmentOptions.Center);
        hint.text = "‹  swipe to move  ›";
        hint.color = uiTextColor;
        hint.fontStyle = FontStyles.Bold;
        hint.raycastTarget = false;
        ApplyFont(hint);
        ApplyTextOutline(hint, uiShadowColor, 0.22f);

        float dur = 3.2f, el = 0f;
        Color c = hint.color;
        while (el < dur && IsRunning && hint)
        {
            float p = el / dur;
            hint.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 6f));
            c.a = 1f - Mathf.Clamp01((p - 0.6f) / 0.4f); // hold, then fade over the last 40%
            hint.color = c;
            el += Time.unscaledDeltaTime;
            yield return null;
        }
        if (hint) Destroy(hint.gameObject);
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

        EnsureWeightBar(hudParent);

        // Right-align the kg readout inside the bar so it sits on the dark
        // remaining-track instead of straddling the moving fill edge.
        {
            weightText.transform.SetParent(weightBarRoot, false);
            RectTransform wt = weightText.rectTransform;
            wt.anchorMin = Vector2.zero;
            wt.anchorMax = Vector2.one;
            wt.offsetMin = new Vector2(0f, 0f);
            wt.offsetMax = new Vector2(-30f, 0f);
            weightText.fontSize = 32;
            weightText.fontStyle = FontStyles.Bold;
            weightText.transform.SetAsLastSibling(); // above the fill
        }
        StyleHudText(weightText, TextAlignmentOptions.MidlineRight);
        weightText.fontStyle = FontStyles.Bold;
        // Soft dark outline keeps the cream readout legible over the dark track
        // (and the gold fill it sits on once near full).
        ApplyTextOutline(weightText, uiShadowColor, 0.2f);

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

        // Rules / tutorial card shown during the Waiting state (reparents the
        // start prompt into its amber CTA pill).
        EnsureRulesCard(hudParent);

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

    // Dark outline around a glyph so light text stays readable on BOTH the dark
    // track and the light bar fill it sits over. Per-text instanced material.
    void ApplyTextOutline(TMP_Text text, Color col, float width)
    {
        if (!text) return;
        Material mat = text.fontMaterial;
        if (!mat) return;
        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, col);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
    }

    void ApplyFont(TMP_Text text)
    {
        if (!text || !uiFont) return;
        text.font = uiFont;
    }

    // Serif display font for card titles / verdicts (falls back to uiFont).
    void ApplyDisplayFont(TMP_Text text)
    {
        if (!text) return;
        text.font = uiDisplayFont ? uiDisplayFont : uiFont;
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
            // Soften harsh building shadows so food on the shadowed side stays
            // readable (was full-strength, hiding treats).
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = keyLightShadowStrength;
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
        runtimeVignette = vignette; // pulsed red in the final seconds (UpdateGoalUi)

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

    // Lifts a HUD pill off the busy scene: a soft warm drop shadow, and an
    // optional thin cream stroke so it reads as the same "paper UI" family as
    // the rules card / wrapper site.
    void StylePill(Image img, bool creamStroke = true)
    {
        if (!img) return;

        UnityEngine.UI.Shadow shadow = img.gameObject.GetComponent<UnityEngine.UI.Shadow>();
        if (!shadow) shadow = img.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.55f);
        shadow.effectDistance = new Vector2(0f, -6f);
        shadow.useGraphicAlpha = true;

        if (creamStroke)
        {
            UnityEngine.UI.Outline outline = img.gameObject.GetComponent<UnityEngine.UI.Outline>();
            if (!outline) outline = img.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.97f, 0.93f, 0.85f, 0.92f);
            outline.effectDistance = new Vector2(2f, 2f);
            outline.useGraphicAlpha = true;
        }
    }

    void EnsureTimerPill()
    {
        if (timerPill || !timerText) return;
        if (!barPillSprite) barPillSprite = CreateRoundedSprite(64, 32, 16);

        // Secondary element: a compact pill tucked into the top-right corner so
        // the weight bar can own the center as the hero element.
        RectTransform timerRt = timerText.rectTransform;
        timerRt.anchorMin = new Vector2(1f, 1f);
        timerRt.anchorMax = new Vector2(1f, 1f);
        timerRt.pivot = new Vector2(1f, 1f);
        timerRt.anchoredPosition = new Vector2(-28f, -28f);
        timerRt.sizeDelta = new Vector2(168f, 84f);
        timerText.fontSize = 52;
        timerText.fontStyle = FontStyles.Bold;

        GameObject pill = new GameObject("TimerPill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pill.transform.SetParent(timerText.transform.parent, false);
        timerPillRoot = pill.GetComponent<RectTransform>();
        timerPillRoot.anchorMin = new Vector2(1f, 1f);
        timerPillRoot.anchorMax = new Vector2(1f, 1f);
        timerPillRoot.pivot = new Vector2(1f, 1f);
        timerPillRoot.anchoredPosition = new Vector2(-20f, -20f);
        timerPillRoot.sizeDelta = new Vector2(184f, 100f);

        timerPill = pill.GetComponent<Image>();
        timerPill.raycastTarget = false;
        timerPill.color = uiBarTrackColor;
        if (barPillSprite) { timerPill.sprite = barPillSprite; timerPill.type = Image.Type.Sliced; }
        StylePill(timerPill);

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
        weightBarRoot.anchoredPosition = new Vector2(0f, -64f);
        weightBarRoot.sizeDelta = new Vector2(560f, 68f);

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
        StylePill(weightBarBg);

        // Fill is a left-anchored rounded pill whose WIDTH animates (in
        // UpdateGoalUi). A sliced sprite + width keeps clean rounded ends —
        // no distorted wedge tip like Image.Type.Filled produced.
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(root.transform, false);
        weightBarFillRect = fill.GetComponent<RectTransform>();
        weightBarFillRect.anchorMin = new Vector2(0f, 0.5f);
        weightBarFillRect.anchorMax = new Vector2(0f, 0.5f);
        weightBarFillRect.pivot = new Vector2(0f, 0.5f);
        weightBarFillRect.anchoredPosition = new Vector2(WeightBarInset, 0f);
        weightBarFillRect.sizeDelta = new Vector2(0f, weightBarRoot.sizeDelta.y - 2f * WeightBarInset);
        weightBarFill = fill.GetComponent<Image>();
        weightBarFill.raycastTarget = false;
        weightBarFill.color = uiBarLowColor;
        if (barPillSprite) { weightBarFill.sprite = barPillSprite; weightBarFill.type = Image.Type.Sliced; }

        // Themed left-cap badge: a circular chip with a paw glyph, overhanging
        // the bar's left end so the meter reads as "the dog's belly filling up".
        float cap = weightBarRoot.sizeDelta.y + 14f;
        GameObject badge = new GameObject("WeightIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badge.transform.SetParent(root.transform, false);
        RectTransform badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 0.5f);
        badgeRt.anchorMax = new Vector2(0f, 0.5f);
        badgeRt.pivot = new Vector2(0.5f, 0.5f);
        badgeRt.anchoredPosition = new Vector2(2f, 0f);
        badgeRt.sizeDelta = new Vector2(cap, cap);
        Image badgeImg = badge.GetComponent<Image>();
        badgeImg.raycastTarget = false;
        badgeImg.color = uiAccentColor;
        if (barPillSprite) { badgeImg.sprite = barPillSprite; badgeImg.type = Image.Type.Sliced; }
        StylePill(badgeImg, false);

        // Paw drawn procedurally (the Fredoka UI font has no emoji glyphs).
        GameObject pawObj = new GameObject("Paw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pawObj.transform.SetParent(badge.transform, false);
        RectTransform pawRt = pawObj.GetComponent<RectTransform>();
        pawRt.anchorMin = new Vector2(0.5f, 0.5f);
        pawRt.anchorMax = new Vector2(0.5f, 0.5f);
        pawRt.pivot = new Vector2(0.5f, 0.5f);
        pawRt.anchoredPosition = Vector2.zero;
        pawRt.sizeDelta = new Vector2(cap * 0.62f, cap * 0.62f);
        Image pawImg = pawObj.GetComponent<Image>();
        pawImg.raycastTarget = false;
        pawImg.color = uiShadowColor;
        if (pawSprite == null) pawSprite = CreatePawSprite();
        pawImg.sprite = pawSprite;
    }

    private Sprite pawSprite;

    // A simple paw: one big pad + four toe beans, drawn into an alpha texture.
    Sprite CreatePawSprite(int size = 96)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var clear = new Color(1f, 1f, 1f, 0f);
        var px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        float s = size;
        // (cx, cy, radius) in normalized [0..1], y up.
        Vector3[] blobs =
        {
            new Vector3(0.50f, 0.40f, 0.24f), // main pad
            new Vector3(0.26f, 0.62f, 0.11f), // toe
            new Vector3(0.43f, 0.74f, 0.115f),
            new Vector3(0.59f, 0.74f, 0.115f),
            new Vector3(0.75f, 0.62f, 0.11f),
        };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / s, ny = y / s;
                float a = 0f;
                foreach (var b in blobs)
                {
                    float d = Mathf.Sqrt((nx - b.x) * (nx - b.x) + (ny - b.y) * (ny - b.y));
                    float edge = (b.z - d) * s; // pixels inside the edge
                    a = Mathf.Max(a, Mathf.Clamp01(edge + 0.5f));
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
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

    // The "How to Play" card, shown while Waiting. Cinematic treatment matching
    // the landing page: dark ink card, small-caps eyebrow, serif (Fraunces)
    // title, hairline gold rule, cream body with gold keywords, gold CTA.
    void EnsureRulesCard(RectTransform parent)
    {
        if (!parent) return;
        if (rulesRoot)
        {
            if (rulesRoot.parent != parent) rulesRoot.SetParent(parent, false);
            return;
        }

        if (!roundedCardSprite) roundedCardSprite = CreateRoundedSprite(96, 96, 40);
        if (!barPillSprite) barPillSprite = CreateRoundedSprite(64, 32, 16);

        // Website palette: ink card, cream/taupe text, ember-gold accent.
        Color cream = new Color(0.957f, 0.906f, 0.827f, 1f);   // #f4e7d3
        Color creamDim = new Color(0.957f, 0.906f, 0.827f, 0.62f);
        Color taupe = new Color(0.851f, 0.776f, 0.663f, 1f);   // #d9c6a9
        Color goldHi = new Color(0.886f, 0.663f, 0.306f, 1f);  // #e2a94e
        string a0 = "<color=#" + ColorUtility.ToHtmlStringRGB(goldHi) + ">";
        string a1 = "</color>";

        // Full-screen dim backdrop
        GameObject root = new GameObject("RulesOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        rulesRoot = root.GetComponent<RectTransform>();
        rulesRoot.anchorMin = Vector2.zero;
        rulesRoot.anchorMax = Vector2.one;
        rulesRoot.offsetMin = Vector2.zero;
        rulesRoot.offsetMax = Vector2.zero;
        Image dim = root.GetComponent<Image>();
        dim.color = new Color(0.04f, 0.03f, 0.02f, 0.6f);
        dim.raycastTarget = false;

        // Ink card
        GameObject card = new GameObject("RulesCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.layer = parent.gameObject.layer;
        card.transform.SetParent(rulesRoot, false);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(0f, 60f);
        cardRt.sizeDelta = new Vector2(900f, 1080f);
        Image cardImg = card.GetComponent<Image>();
        cardImg.sprite = roundedCardSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.122f, 0.094f, 0.075f, 0.99f); // #1f1813
        cardImg.raycastTarget = false;

        // Small-caps eyebrow above the title (film-credit voice).
        TMP_Text eyebrow = CreateUiText(cardRt, "RulesEyebrow",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -66f), new Vector2(820f, 50f), 30, TextAlignmentOptions.Center);
        eyebrow.text = "ONE MORE WALK";
        eyebrow.characterSpacing = 22f;
        eyebrow.color = creamDim;
        ApplyFont(eyebrow);

        // Serif title, title-case — the website's display voice.
        TMP_Text title = CreateUiText(cardRt, "RulesTitle",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -112f), new Vector2(820f, 120f), 82, TextAlignmentOptions.Center);
        title.text = "How to Play";
        title.color = cream;
        title.enableWordWrapping = false;
        ApplyDisplayFont(title);

        // Hairline gold rule under the title
        GameObject div = new GameObject("RulesDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        div.layer = parent.gameObject.layer;
        div.transform.SetParent(cardRt, false);
        RectTransform divRt = div.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.5f, 1f);
        divRt.anchorMax = new Vector2(0.5f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -248f);
        divRt.sizeDelta = new Vector2(90f, 3f);
        Image divImg = div.GetComponent<Image>();
        divImg.sprite = barPillSprite;
        divImg.type = Image.Type.Sliced;
        divImg.color = uiAccentColor;
        divImg.raycastTarget = false;

        // Rules body — centered, taupe with gold keywords (no bold spam).
        TMP_Text body = CreateUiText(cardRt, "RulesBody",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -300f), new Vector2(760f, 560f), 44, TextAlignmentOptions.Top);
        body.color = taupe;
        body.enableWordWrapping = true;
        body.lineSpacing = 18f;
        body.text =
            "Fatten Kenzo to " + a0 + "75 kg" + a1 + " in " + a0 + "60 seconds" + a1 + "\n\n" +
            "Eat " + a0 + "treats" + a1 + " to pile on weight\n\n" +
            "Dodge " + a0 + "veggies" + a1 + " — they slim you down\n\n" +
            a0 + "Swipe" + a1 + " left / right to switch lanes";
        ApplyFont(body);

        // Gold CTA at the bottom of the card — flat, tracked caps, dark label
        // (the website's "Begin" button vocabulary).
        GameObject pill = new GameObject("RulesCta", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pill.layer = parent.gameObject.layer;
        pill.transform.SetParent(cardRt, false);
        rulesCtaPill = pill.GetComponent<RectTransform>();
        rulesCtaPill.anchorMin = new Vector2(0.5f, 0f);
        rulesCtaPill.anchorMax = new Vector2(0.5f, 0f);
        rulesCtaPill.pivot = new Vector2(0.5f, 0f);
        rulesCtaPill.anchoredPosition = new Vector2(0f, 70f);
        rulesCtaPill.sizeDelta = new Vector2(520f, 118f);
        Image pillImg = pill.GetComponent<Image>();
        pillImg.sprite = barPillSprite;
        pillImg.type = Image.Type.Sliced;
        pillImg.color = uiAccentColor;
        pillImg.raycastTarget = false;

        // Reuse the existing start prompt as the pill's label.
        if (startPromptText)
        {
            startPromptText.transform.SetParent(rulesCtaPill, false);
            RectTransform sp = startPromptText.rectTransform;
            sp.anchorMin = Vector2.zero;
            sp.anchorMax = Vector2.one;
            sp.offsetMin = Vector2.zero;
            sp.offsetMax = Vector2.zero;
            startPromptText.enableAutoSizing = false;
            startPromptText.fontSize = 42;
            startPromptText.characterSpacing = 18f;
            startPromptText.text = "TAP TO START";
            startPromptText.alignment = TextAlignmentOptions.Center;
            startPromptText.textWrappingMode = TextWrappingModes.NoWrap;
            startPromptText.color = new Color(0.086f, 0.055f, 0.016f, 1f); // dark on gold
            ApplyFont(startPromptText);
        }
    }

    void EnsureGameOverUi(Transform canvas)
    {
        if (gameOverPanel) return;
        if (!barPillSprite) barPillSprite = CreateRoundedSprite(64, 32, 16);
        if (pawSprite == null) pawSprite = CreatePawSprite();

        GameObject panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.layer = canvas.gameObject.layer;
        panel.transform.SetParent(canvas, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = panel.GetComponent<Image>();
        img.color = new Color(0.04f, 0.03f, 0.02f, 0.86f);
        gameOverGroup = panel.AddComponent<CanvasGroup>();

        // Card (taller, to host an icon up top and a button at the bottom).
        GameObject card = new GameObject("GameOverCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.layer = canvas.gameObject.layer;
        card.transform.SetParent(panel.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(720f, 640f);
        gameOverCardRect = cardRect;
        Image cardImg = card.GetComponent<Image>();
        cardImg.color = new Color(0.122f, 0.094f, 0.075f, 0.99f); // #1f1813, same ink as the rules card
        ApplyRoundedCard(cardImg);

        // Circular icon badge straddling the card's top edge.
        GameObject badge = new GameObject("GOIconBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badge.transform.SetParent(card.transform, false);
        RectTransform badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(0.5f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 0.5f);
        badgeRt.anchoredPosition = new Vector2(0f, 0f);
        badgeRt.sizeDelta = new Vector2(132f, 132f);
        gameOverIconBadge = badge.GetComponent<Image>();
        gameOverIconBadge.raycastTarget = false;
        gameOverIconBadge.color = uiAccentColor;
        gameOverIconBadge.sprite = barPillSprite;
        gameOverIconBadge.type = Image.Type.Sliced;

        GameObject paw = new GameObject("GOPaw", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        paw.transform.SetParent(badge.transform, false);
        RectTransform pawRt = paw.GetComponent<RectTransform>();
        pawRt.anchorMin = pawRt.anchorMax = pawRt.pivot = new Vector2(0.5f, 0.5f);
        pawRt.anchoredPosition = Vector2.zero;
        pawRt.sizeDelta = new Vector2(78f, 78f);
        gameOverIconPaw = paw.GetComponent<Image>();
        gameOverIconPaw.raycastTarget = false;
        gameOverIconPaw.color = uiShadowColor;
        gameOverIconPaw.sprite = pawSprite;

        // Win-only celebration image (woman lifting the too-chubby dog as the
        // plane leaves). Shown on win, hidden on loss; rounded to match the card.
        GameObject winImg = new GameObject("GOWinImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        winImg.transform.SetParent(card.transform, false);
        RectTransform wiRt = winImg.GetComponent<RectTransform>();
        wiRt.anchorMin = new Vector2(0.5f, 1f);
        wiRt.anchorMax = new Vector2(0.5f, 1f);
        wiRt.pivot = new Vector2(0.5f, 1f);
        wiRt.anchoredPosition = new Vector2(0f, -150f);
        wiRt.sizeDelta = new Vector2(560f, 300f);
        gameOverWinImage = winImg.GetComponent<RawImage>();
        gameOverWinImage.raycastTarget = false;
        gameOverWinImage.texture = Resources.Load<Texture2D>("win_scene");
        winImg.SetActive(false);

        gameOverTitleText = CreateUiText(card.transform, "GameOverTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(640f, 70f), gameOverFontSize, TextAlignmentOptions.Center);
        gameOverTitleText.text = "GAME OVER";
        StyleHudText(gameOverTitleText, TextAlignmentOptions.Center);
        gameOverTitleText.fontStyle = FontStyles.Bold;

        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divider.transform.SetParent(card.transform, false);
        RectTransform divRt = divider.GetComponent<RectTransform>();
        divRt.anchorMin = divRt.anchorMax = new Vector2(0.5f, 1f);
        divRt.pivot = new Vector2(0.5f, 0.5f);
        divRt.anchoredPosition = new Vector2(0f, -168f);
        divRt.sizeDelta = new Vector2(110f, 6f);
        gameOverAccent = divider.GetComponent<Image>();
        gameOverAccent.raycastTarget = false;
        gameOverAccent.color = uiAccentColor;
        gameOverAccent.sprite = barPillSprite;
        gameOverAccent.type = Image.Type.Sliced;

        // Flavor message.
        gameOverScoreText = CreateUiText(card.transform, "GameOverScore", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(640f, 90f), gameOverFontSize, TextAlignmentOptions.Center);
        StyleHudText(gameOverScoreText, TextAlignmentOptions.Center, true);

        // Hero number (the final weight) — the big celebratory stat.
        gameOverNumber = CreateUiText(card.transform, "GameOverNumber", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(640f, 110f), 78, TextAlignmentOptions.Center);
        StyleHudText(gameOverNumber, TextAlignmentOptions.Center);
        gameOverNumber.fontStyle = FontStyles.Bold;
        gameOverBestText = gameOverNumber; // keep legacy ref pointed at the number

        // Restart button: a pill with centered label.
        GameObject button = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        button.transform.SetParent(card.transform, false);
        RectTransform btnRt = button.GetComponent<RectTransform>();
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 40f);
        btnRt.sizeDelta = new Vector2(380f, 92f);
        gameOverButtonBg = button.GetComponent<Image>();
        gameOverButtonBg.raycastTarget = false;
        gameOverButtonBg.color = uiAccentColor;
        gameOverButtonBg.sprite = barPillSprite;
        gameOverButtonBg.type = Image.Type.Sliced;

        gameOverHintText = CreateUiText(button.transform, "GameOverHint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 86f), 34, TextAlignmentOptions.Center);
        gameOverHintText.text = "TAP TO RESTART";
        StyleHudText(gameOverHintText, TextAlignmentOptions.Center);
        gameOverHintText.color = uiShadowColor; // dark text on amber pill
        gameOverHintText.fontStyle = FontStyles.Bold;

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
            ApplyDisplayFont(gameOverTitleText); // serif verdict, like the website's endings
        }
        if (gameOverAccent) gameOverAccent.color = outcomeColor;

        // Icon badge + paw take the outcome color so win reads warm-gold,
        // loss reads warm-red — instant read before you parse the words.
        if (gameOverIconBadge) gameOverIconBadge.color = outcomeColor;
        if (gameOverIconPaw) gameOverIconPaw.color = uiShadowColor;

        if (gameOverScoreText)
        {
            string flavor = won ? winMessage : loseMessage;
            if (!won)
            {
                // Near-miss framing turns a loss into a hook: show how close Kenzo got.
                int toGo = Mathf.CeilToInt(winWeightKg - finalWeight);
                if (finalWeight > Mathf.RoundToInt(barFloorKg) && toGo > 0)
                    flavor = "So close — only " + toGo + " kg to go!\nOne more walk?";
            }
            gameOverScoreText.text = flavor;
            gameOverScoreText.fontSize = Mathf.RoundToInt(gameOverFontSize * 0.62f);
            ApplyFont(gameOverScoreText);
        }
        if (gameOverNumber)
        {
            gameOverNumber.text = finalWeight + " kg";
            gameOverNumber.color = outcomeColor;
            ApplyDisplayFont(gameOverNumber); // the hero stat is a display moment
        }
        if (gameOverButtonBg) gameOverButtonBg.color = outcomeColor;
        if (gameOverHintText)
        {
            gameOverHintText.text = won ? "PLAY AGAIN" : "TRY AGAIN";
            gameOverHintText.color = uiShadowColor;
            gameOverHintText.characterSpacing = 16f; // tracked caps, like the site's buttons
            gameOverHintText.fontStyle = FontStyles.Normal;
            ApplyFont(gameOverHintText);
        }

        // On a win, the celebration image takes over the card's middle: show it,
        // hide the paw badge + flavor message + divider so nothing overlaps, and
        // restack the title cleanly above the image and the weight number below
        // it. On a loss (no image) restore the original single-column layout.
        bool showWin = won && gameOverWinImage && gameOverWinImage.texture;
        if (gameOverWinImage) gameOverWinImage.gameObject.SetActive(showWin);
        if (gameOverIconBadge) gameOverIconBadge.gameObject.SetActive(!showWin);
        if (gameOverScoreText) gameOverScoreText.gameObject.SetActive(!showWin);
        if (gameOverAccent) gameOverAccent.gameObject.SetActive(!showWin);

        if (showWin)
        {
            SetCardTop(gameOverTitleText, -46f);
            RectTransform wi = gameOverWinImage.rectTransform;
            wi.sizeDelta = new Vector2(520f, 260f);
            wi.anchoredPosition = new Vector2(0f, -120f);
            SetCardTop(gameOverNumber, -395f);
        }
        else
        {
            SetCardTop(gameOverTitleText, -110f);
            SetCardTop(gameOverNumber, -370f);
        }
    }

    // Position a top-anchored card text by its Y offset from the card's top edge.
    void SetCardTop(TMP_Text text, float y)
    {
        if (!text) return;
        RectTransform rt = text.rectTransform;
        Vector2 p = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(p.x, y);
    }

    void StartRun()
    {
        score = 0f;
        distance = 0f;
        ElapsedTime = 0f;
        moveSpeed = startSpeed;
        won = false;
        timeRemaining = timeLimit;
        weightHitFlash = 0f;
        lastMilestone = int.MinValue;
        if (runtimeVignette != null)
        {
            runtimeVignette.intensity.value = vignetteIntensity;
            runtimeVignette.color.value = Color.black;
        }
        if (!dogWeight) dogWeight = FindObjectOfType<DogWeightVisual>();
        float startW = dogWeight ? dogWeight.currentWeight : barFloorKg;
        barFillDisplay = Mathf.Clamp01(Mathf.InverseLerp(barFloorKg, winWeightKg, startW));
        lastPunchWeight = startW;
        weightPunch = 0f;
        punchInit = true;
        UpdateUi();
        UpdateGoalUi();
        SetState(GameState.Running);
        ShowSwipeHint();
    }

    void SetState(GameState newState)
    {
        State = newState;
        IsRunning = (State == GameState.Running);

        if (rulesRoot) rulesRoot.gameObject.SetActive(State == GameState.Waiting);
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
