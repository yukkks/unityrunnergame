using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public GameObject coinPrefab;
    public float laneOffset = 1.2f;
    public float spawnY = 0.5f;
    public float spawnZ = 12f;
    public float coinY = 0.9f;
    public float coinZ = 12f;

    [Header("Spawn Rate")]
    public float startMinInterval = 0.8f;
    public float startMaxInterval = 1.25f;
    public float endMinInterval = 0.35f;
    public float endMaxInterval = 0.7f;
    public float rampDuration = 60f; // seconds to reach hardest

    [Header("Patterns")]
    public bool usePatterns = true;

    [Header("Coins")]
    [Range(0f, 1f)]
    public float coinSpawnChance = 1f;
    public bool coinStreaks = true;
    public Vector2Int coinStreakLength = new Vector2Int(3, 6);
    public float coinStreakChance = 0.35f;
    public float bonusLaneChance = 0.08f;

    private float nextTime;
    private int[] currentPattern;
    private int patternIndex;
    private int[][] patternBank;
    private int coinStreakRemaining;
    private int coinStreakLane = -1;
    private int bonusLane = -1;
    private float bonusLaneUntil;

    void Start()
    {
        patternBank = new int[][]
        {
            new int[] { 0, 1 },
            new int[] { 1, 0 },
            new int[] { 0, 0, 1 },
            new int[] { 1, 1, 0 },
            new int[] { 0, 1, 1 },
            new int[] { 1, 0, 0 },
            new int[] { 0, 1, 0, 1 },
            new int[] { 1, 0, 1, 0 }
        };
        Schedule();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        if (Time.time >= nextTime)
        {
            Spawn();
            Schedule();
        }
    }

    void Schedule()
    {
        float t = 0f;
        if (GameManager.Instance != null)
        {
            t = Mathf.Clamp01(GameManager.Instance.ElapsedTime / rampDuration);
        }

        float minI = Mathf.Lerp(startMinInterval, endMinInterval, t);
        float maxI = Mathf.Lerp(startMaxInterval, endMaxInterval, t);

        nextTime = Time.time + Random.Range(minI, maxI);
    }

    void Spawn()
    {
        if (!obstaclePrefab) return;

        UpdateBonusLane();

        int obstacleLane = GetNextLane();
        float x = (obstacleLane == 0) ? -laneOffset : laneOffset;
        Vector3 pos = new Vector3(x, spawnY, spawnZ);

        Instantiate(obstaclePrefab, pos, Quaternion.identity);

        if (ShouldSpawnCoin(obstacleLane))
        {
            int coinLane = GetCoinLane(obstacleLane);
            float coinX = (coinLane == 0) ? -laneOffset : laneOffset;
            Vector3 coinPos = new Vector3(coinX, coinY, coinZ);
            SpawnCoin(coinPos);
        }
    }

    int GetNextLane()
    {
        if (!usePatterns || patternBank == null || patternBank.Length == 0)
        {
            return Random.Range(0, 2);
        }

        if (currentPattern == null || patternIndex >= currentPattern.Length)
        {
            currentPattern = patternBank[Random.Range(0, patternBank.Length)];
            patternIndex = 0;
        }

        int lane = currentPattern[patternIndex];
        patternIndex += 1;
        return lane;
    }

    bool ShouldSpawnCoin(int obstacleLane)
    {
        if (bonusLane >= 0) return true;
        if (coinStreaks && coinStreakRemaining > 0) return true;
        return Random.value < coinSpawnChance;
    }

    int GetCoinLane(int obstacleLane)
    {
        int lane = bonusLane >= 0 ? bonusLane : -1;

        if (coinStreaks)
        {
            if (coinStreakRemaining > 0)
            {
                coinStreakRemaining -= 1;
                lane = coinStreakLane;
            }

            if (Random.value < coinStreakChance)
            {
                coinStreakRemaining = Random.Range(coinStreakLength.x, coinStreakLength.y + 1);
                coinStreakLane = Random.Range(0, 2);
                coinStreakRemaining -= 1;
                lane = coinStreakLane;
            }
        }

        if (lane < 0)
        {
            lane = 1 - obstacleLane;
        }

        if (lane == obstacleLane)
        {
            lane = 1 - obstacleLane;
            if (coinStreakRemaining > 0 && coinStreakLane == obstacleLane)
            {
                coinStreakLane = lane;
            }
        }

        return lane;
    }

    void UpdateBonusLane()
    {
        if (bonusLane >= 0)
        {
            if (Time.time >= bonusLaneUntil)
            {
                bonusLane = -1;
            }
            return;
        }

        if (Random.value < bonusLaneChance)
        {
            bonusLane = Random.Range(0, 2);
            bonusLaneUntil = Time.time + Random.Range(2f, 4f);
        }
    }

    void SpawnCoin(Vector3 pos)
    {
        if (coinPrefab)
        {
            GameObject coin = Instantiate(coinPrefab, pos, Quaternion.identity);
            EnsureCoinSetup(coin);
            return;
        }

        GameObject runtimeCoin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        runtimeCoin.name = "Coin";
        runtimeCoin.transform.position = pos;
        runtimeCoin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        runtimeCoin.transform.localScale = new Vector3(0.8f, 0.12f, 0.8f);

        EnsureCoinSetup(runtimeCoin);
        ApplyRuntimeCoinMaterial(runtimeCoin);
    }

    void EnsureCoinSetup(GameObject coin)
    {
        Collider col = coin.GetComponent<Collider>();
        if (col) col.isTrigger = true;

        if (!coin.GetComponent<CoinPickup>())
        {
            coin.AddComponent<CoinPickup>();
        }
    }

    void ApplyRuntimeCoinMaterial(GameObject coin)
    {
        Renderer renderer = coin.GetComponent<Renderer>();
        if (!renderer) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        Color baseColor = new Color(1f, 0.86f, 0.1f);
        mat.color = baseColor;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_EmissionColor"))
        {
            Color emission = new Color(1f, 0.6f, 0.1f);
            mat.SetColor("_EmissionColor", emission);
            mat.EnableKeyword("_EMISSION");
        }

        renderer.material = mat;
    }
}
