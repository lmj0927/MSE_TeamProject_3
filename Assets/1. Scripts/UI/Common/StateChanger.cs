// Owned by JunYoung Park
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// Handles color transitions for UI
public class StateChanger : MonoBehaviour
{
    [SerializeField] private Image[] targetImages;
    private Color[] originalColors;
    private int currentState = -1;

    void Awake()
    {
        TryInitialize();
    }

    // Cache original colors
    private void TryInitialize()
    {
        if (originalColors != null) return;

        originalColors = new Color[targetImages.Length];
        for (int i = 0; i < targetImages.Length; i++)
        {
            originalColors[i] = targetImages[i].color;
        }
    }

    // Hue shifting with tween
    public void SetColorState(int state, float duration = 0.5f)
    {
        if (targetImages == null || originalColors == null) return;
        if (duration > 0f && currentState == state) return;
        currentState = state;

        float hueShift = 0f;
        if (state == 1) hueShift = -0.15f;
        else if (state == 2) hueShift = -0.33f;

        for (int i = 0; i < targetImages.Length; i++)
        {
            if (targetImages[i] == null) continue;

            Color.RGBToHSV(originalColors[i], out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueShift, 1f);
            Color targetColor = Color.HSVToRGB(h, s, v);
            targetColor.a = originalColors[i].a;

            targetImages[i].DOKill();

            if (duration <= 0f) targetImages[i].color = targetColor;
            else targetImages[i].DOColor(targetColor, duration).SetEase(Ease.InCubic);
        }
    }

    // Get Hue value difference
    public float GetCurrentHueShift()
    {
        TryInitialize();

        if (targetImages == null || targetImages.Length == 0 || targetImages[0] == null) return 0f;

        Color.RGBToHSV(targetImages[0].color, out float currentH, out _, out _);
        Color.RGBToHSV(originalColors[0], out float originalH, out _, out _);

        float shift = currentH - originalH;

        // Correction
        if (shift > 0.5f) shift -= 1f;
        else if (shift < -0.5f) shift += 1f;

        return shift;
    }
}