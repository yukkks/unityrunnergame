using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SideBuildings : MonoBehaviour
{
    public int countPerSide = 12;
    public float spacing = 6f;
    public float sideOffset = 5.2f;
    public float baseY = 0.1f;
    public float startZ = 0f;
    public float recycleZ = -12f;

    public Vector2 heightRange = new Vector2(1.5f, 5f);
    public Vector2 widthRange = new Vector2(0.8f, 2f);
    public Vector2 depthRange = new Vector2(2f, 5f);

    [Header("Materials")]
    public Material[] buildingMaterials;
    public Material buildingMaterial;
    public Color baseColor = new Color(0.06f, 0.07f, 0.09f, 1f);
    public Color windowColor = new Color(1f, 0.82f, 0.55f, 1f);
    public float emissionIntensity = 1.2f;
    public Vector2 windowTiling = new Vector2(1f, 4f);
    public float metallic = 0.05f;
    public float smoothness = 0.6f;
    public bool useWindowEmission = true;

    [Header("Prefabs")]
    public bool usePrefabBuildings = true;
    public GameObject[] buildingPrefabs;
    public Vector2 prefabScaleRange = new Vector2(0.35f, 0.6f);
    public bool alignPrefabToGround = true;
    public float prefabYOffset = 0f;
    public bool randomYaw = false;
    public float leftYaw = 90f;
    public float rightYaw = -90f;

    [Header("Prefab Color Variants")]
    public bool randomizePrefabColors = true;
    public Color prefabBaseMin = new Color(0.18f, 0.2f, 0.24f, 1f);
    public Color prefabBaseMax = new Color(0.55f, 0.6f, 0.7f, 1f);
    public Color prefabEmissionMin = new Color(0.15f, 0.35f, 0.85f, 1f);
    public Color prefabEmissionMax = new Color(1f, 0.75f, 0.25f, 1f);
    public float prefabEmissionScaleMin = 0.7f;
    public float prefabEmissionScaleMax = 1.5f;

    [Header("Shape")]
    [Range(0f, 1f)]
    public float topBlockChance = 0.7f;
    public Vector2 topScaleRange = new Vector2(0.55f, 0.85f);
    public Vector2 topHeightRange = new Vector2(0.25f, 0.6f);

    private Transform[] leftBuildings;
    private Transform[] rightBuildings;
    private float loopLength;
    private Material[] materialPool;
    private Material fallbackMaterial;
    private Texture2D windowTexture;
    private MaterialPropertyBlock block;

    void Start()
    {
        Build();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsRunning) return;

        float speed = GameManager.Instance ? GameManager.Instance.moveSpeed : 10f;
        float dz = speed * Time.deltaTime;

        MoveBuildings(leftBuildings, dz);
        MoveBuildings(rightBuildings, dz);
    }

    void Build()
    {
        ClearChildren();

        if (countPerSide <= 0) return;

        if (!UsePrefabs())
        {
            BuildMaterialPool();
        }

        loopLength = countPerSide * spacing;

        leftBuildings = new Transform[countPerSide];
        rightBuildings = new Transform[countPerSide];

        for (int i = 0; i < countPerSide; i++)
        {
            float z = startZ + i * spacing;
            leftBuildings[i] = CreateBuilding("Building_L_" + i, -sideOffset, z, true);
            rightBuildings[i] = CreateBuilding("Building_R_" + i, sideOffset, z + spacing * 0.5f, false);
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    Transform CreateBuilding(string name, float x, float z, bool isLeft)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform);
        root.transform.localPosition = new Vector3(x, 0f, z);

        if (UsePrefabs())
        {
            GameObject prefab = GetPrefab();
            if (prefab)
            {
                GameObject instance = Instantiate(prefab, root.transform);
                instance.transform.localPosition = Vector3.zero;
                float scale = Random.Range(prefabScaleRange.x, prefabScaleRange.y);
                instance.transform.localScale = Vector3.one * scale;

                float yaw = isLeft ? leftYaw : rightYaw;
                if (randomYaw) yaw = Random.Range(0f, 360f);
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

                if (alignPrefabToGround)
                {
                    AlignToGround(instance.transform, baseY + prefabYOffset);
                }
                if (randomizePrefabColors)
                {
                    ApplyPrefabColorVariants(instance);
                }
                return root.transform;
            }
        }

        float height = Random.Range(heightRange.x, heightRange.y);
        float width = Random.Range(widthRange.x, widthRange.y);
        float depth = Random.Range(depthRange.x, depthRange.y);

        CreateBlock(root.transform, width, height, depth, baseY, 0f);

        if (Random.value < topBlockChance)
        {
            float topScale = Random.Range(topScaleRange.x, topScaleRange.y);
            float topHeight = height * Random.Range(topHeightRange.x, topHeightRange.y);
            CreateBlock(root.transform, width * topScale, topHeight, depth * topScale, baseY + height, 0.02f);
        }

        return root.transform;
    }

    void CreateBlock(Transform parent, float width, float height, float depth, float yBase, float zOffset)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.SetParent(parent);
        obj.transform.localScale = new Vector3(width, height, depth);
        obj.transform.localPosition = new Vector3(0f, yBase + height * 0.5f, zOffset);

        Collider col = obj.GetComponent<Collider>();
        if (col) Destroy(col);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer)
        {
            renderer.sharedMaterial = GetMaterialForBuilding();
            ApplyMaterialVariants(renderer);
        }
    }

    void MoveBuildings(Transform[] buildings, float dz)
    {
        if (buildings == null) return;

        for (int i = 0; i < buildings.Length; i++)
        {
            Transform t = buildings[i];
            if (!t) continue;

            Vector3 pos = t.localPosition;
            pos.z -= dz;
            if (pos.z < recycleZ)
            {
                pos.z += loopLength;
            }
            t.localPosition = pos;
        }
    }

    void BuildMaterialPool()
    {
        Material[] sources = (buildingMaterials != null && buildingMaterials.Length > 0)
            ? buildingMaterials
            : (buildingMaterial ? new Material[] { buildingMaterial } : null);

        if (sources == null || sources.Length == 0)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            fallbackMaterial = new Material(shader);
            sources = new Material[] { fallbackMaterial };
        }

        materialPool = new Material[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            Material mat = new Material(sources[i]);
            ConfigureMaterial(mat);
            materialPool[i] = mat;
        }
    }

    Material GetMaterialForBuilding()
    {
        if (materialPool == null || materialPool.Length == 0)
        {
            BuildMaterialPool();
        }
        int index = Random.Range(0, materialPool.Length);
        return materialPool[index];
    }

    void ConfigureMaterial(Material mat)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", baseColor);
        }
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", baseColor);
        }
        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", metallic);
        }
        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }

        if (useWindowEmission)
        {
            if (!windowTexture) windowTexture = BuildWindowTexture(24, 96);
            if (mat.HasProperty("_EmissionMap"))
            {
                mat.SetTexture("_EmissionMap", windowTexture);
                mat.SetTextureScale("_EmissionMap", windowTiling);
            }
            mat.EnableKeyword("_EMISSION");
        }
    }

    void ApplyMaterialVariants(Renderer renderer)
    {
        if (!renderer) return;
        if (block == null) block = new MaterialPropertyBlock();

        renderer.GetPropertyBlock(block);

        Color baseVar = baseColor * Random.Range(0.8f, 1.15f);
        Color emission = windowColor * (emissionIntensity * Random.Range(0.6f, 1.4f));

        if (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_BaseColor"))
        {
            block.SetColor("_BaseColor", baseVar);
        }
        if (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_Color"))
        {
            block.SetColor("_Color", baseVar);
        }
        if (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_EmissionColor"))
        {
            block.SetColor("_EmissionColor", emission);
        }

        renderer.SetPropertyBlock(block);
    }

    Texture2D BuildWindowTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;
        tex.hideFlags = HideFlags.DontSave;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool frame = (x % 4 == 0) || (y % 6 == 0);
                bool lit = !frame && Random.value > 0.65f;
                Color c = lit ? Color.white : Color.black;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }

    bool UsePrefabs()
    {
        return usePrefabBuildings && buildingPrefabs != null && buildingPrefabs.Length > 0;
    }

    GameObject GetPrefab()
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return null;
        int index = Random.Range(0, buildingPrefabs.Length);
        return buildingPrefabs[index];
    }

    void AlignToGround(Transform target, float groundY)
    {
        if (!target) return;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float offset = groundY - bounds.min.y;
        target.position += new Vector3(0f, offset, 0f);
    }

    void ApplyPrefabColorVariants(GameObject instance)
    {
        if (!instance) return;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Color baseColorVar = new Color(
            Random.Range(prefabBaseMin.r, prefabBaseMax.r),
            Random.Range(prefabBaseMin.g, prefabBaseMax.g),
            Random.Range(prefabBaseMin.b, prefabBaseMax.b),
            1f
        );

        Color emissionVar = new Color(
            Random.Range(prefabEmissionMin.r, prefabEmissionMax.r),
            Random.Range(prefabEmissionMin.g, prefabEmissionMax.g),
            Random.Range(prefabEmissionMin.b, prefabEmissionMax.b),
            1f
        );
        float emissionScale = Random.Range(prefabEmissionScaleMin, prefabEmissionScaleMax);
        emissionVar *= emissionScale;

        if (block == null) block = new MaterialPropertyBlock();

        foreach (Renderer renderer in renderers)
        {
            if (!renderer) continue;
            renderer.GetPropertyBlock(block);

            if (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                block.SetColor("_BaseColor", baseColorVar);
            }
            else if (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_Color"))
            {
                block.SetColor("_Color", baseColorVar);
            }

            if (renderer.sharedMaterial && renderer.sharedMaterial.HasProperty("_EmissionColor"))
            {
                block.SetColor("_EmissionColor", emissionVar);
            }

            renderer.SetPropertyBlock(block);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (usePrefabBuildings && (buildingPrefabs == null || buildingPrefabs.Length == 0))
        {
            AutoPopulatePrefabs();
        }
    }

    [ContextMenu("Auto Populate Prefabs")]
    void AutoPopulatePrefabs()
    {
        string[] searchIn = new string[] { "Assets/PolygonSciFiCity/Prefabs/Buildings" };
        string[] guids = AssetDatabase.FindAssets("t:prefab", searchIn);
        if (guids == null || guids.Length == 0) return;

        var list = new System.Collections.Generic.List<GameObject>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (file.Contains("Section")) continue;
            if (file.Contains("Interior")) continue;

            bool allow =
                file.Contains("Background_Small") ||
                file.Contains("Background_Med") ||
                file.Contains("Background_Medium") ||
                file.Contains("Large_Part") ||
                file.Contains("Industrial") ||
                file.Contains("Advanced") ||
                file.Contains("Power") ||
                file.Contains("Raised");

            // Exclude specific oversized/awkward prefabs
            if (file.Contains("Large_Part_05"))
            {
                allow = false;
            }

            if (!allow) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab) list.Add(prefab);
        }

        if (list.Count > 0)
        {
            buildingPrefabs = list.ToArray();
            EditorUtility.SetDirty(this);
        }
    }
#endif
}
