using UnityEngine;

public class SideBuildings : MonoBehaviour
{
    public int countPerSide = 12;
    public float spacing = 6f;
    public float sideOffset = 3.12f;
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

        BuildMaterialPool();

        loopLength = countPerSide * spacing;

        leftBuildings = new Transform[countPerSide];
        rightBuildings = new Transform[countPerSide];

        for (int i = 0; i < countPerSide; i++)
        {
            float z = startZ + i * spacing;
            leftBuildings[i] = CreateBuilding("Building_L_" + i, -sideOffset, z);
            rightBuildings[i] = CreateBuilding("Building_R_" + i, sideOffset, z + spacing * 0.5f);
        }
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    Transform CreateBuilding(string name, float x, float z)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform);
        root.transform.localPosition = new Vector3(x, 0f, z);

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
}
