using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SplashTipsCycler : MonoBehaviour
{
    [Tooltip("Seconds between tips.")]
    public float intervalSeconds = 3f;

    [Tooltip("Tips shown in order. Edit in Inspector.")]
    public List<string> tips = new List<string>
    {
        "Tap to switch lanes and dodge bombs.",
        "Collect coins to boost your score.",
        "Stay centered when obstacles cluster."
    };

    private Text uiText;
    private int index;
    private float nextTime;

    void Awake()
    {
        uiText = GetComponent<Text>();
        if (!uiText)
        {
            enabled = false;
        }
    }

    void OnEnable()
    {
        index = 0;
        ApplyTip();
        nextTime = Time.unscaledTime + Mathf.Max(0.1f, intervalSeconds);
    }

    void Update()
    {
        if (!uiText || tips == null || tips.Count == 0) return;

        if (Time.unscaledTime >= nextTime)
        {
            index = (index + 1) % tips.Count;
            ApplyTip();
            nextTime = Time.unscaledTime + Mathf.Max(0.1f, intervalSeconds);
        }
    }

    void ApplyTip()
    {
        if (!uiText || tips == null || tips.Count == 0)
        {
            if (uiText) uiText.text = string.Empty;
            return;
        }

        uiText.text = tips[index] ?? string.Empty;
    }
}
