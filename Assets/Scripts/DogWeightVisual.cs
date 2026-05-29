using UnityEngine;

public class DogWeightVisual : MonoBehaviour
{
    [Header("Assign visible dog model here")]
    public Transform dogVisual;

    [Header("Weight")]
    public float minWeight = 0f;
    public float maxWeight = 100f;
    public float currentWeight = 30f;

    [Header("Visual Size")]
    public float slimScale = 0.95f;
    public float fatScale = 1.25f;

    [Header("Chubby Shape")]
    public float fatWidthMultiplier = 1.15f;
    public float fatHeightMultiplier = 0.95f;

    [Header("Smoothness")]
    public float scaleSmoothSpeed = 4f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Start()
    {
        if (!dogVisual)
        {
            Debug.LogWarning("DogWeightVisual: dogVisual is not assigned.");
            return;
        }

        originalScale = dogVisual.localScale;
        targetScale = originalScale;
        CalculateTargetScale();
        dogVisual.localScale = targetScale;
    }

    void Update()
    {
        if (!dogVisual) return;

        dogVisual.localScale = Vector3.Lerp(
            dogVisual.localScale,
            targetScale,
            Time.deltaTime * scaleSmoothSpeed
        );
    }

    public void GainWeight(float amount)
    {
        currentWeight = Mathf.Clamp(currentWeight + amount, minWeight, maxWeight);
        CalculateTargetScale();
    }

    public void LoseWeight(float amount)
    {
        currentWeight = Mathf.Clamp(currentWeight - amount, minWeight, maxWeight);
        CalculateTargetScale();
    }

    void CalculateTargetScale()
    {
        if (!dogVisual) return;

        float t = Mathf.InverseLerp(minWeight, maxWeight, currentWeight);

        float baseScale = Mathf.Lerp(slimScale, fatScale, t);
        float width = Mathf.Lerp(1f, fatWidthMultiplier, t);
        float height = Mathf.Lerp(1f, fatHeightMultiplier, t);

        targetScale = new Vector3(
            originalScale.x * baseScale * width,
            originalScale.y * baseScale * height,
            originalScale.z * baseScale * width
        );
    }
}