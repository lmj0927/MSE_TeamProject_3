using UnityEngine;
using UnityEngine.UI;

public class StateChanger : MonoBehaviour
{
    [SerializeField] private Image[] targetImages;

    private Color[] originalColors;

    void Awake()
    {
        if (targetImages != null && targetImages.Length > 0)
        {
            originalColors = new Color[targetImages.Length];
            for (int i = 0; i < targetImages.Length; i++)
            {
                if (targetImages[i] != null)
                {
                    originalColors[i] = targetImages[i].color;
                }
            }
        }
    }

    // 0 is happy (default)
    // 1 is uncomfortable
    // 2 is angry
    public void SetColorState(int state)
    {
        if (targetImages == null || originalColors == null) return;

        float hueShift = 0f;
        if (state == 1) hueShift = -0.15f;    
        else if (state == 2) hueShift = -0.33f;

        for (int i = 0; i < targetImages.Length; i++)
        {
            if (targetImages[i] == null) continue;

            Color.RGBToHSV(originalColors[i], out float h, out float s, out float v);


            h = Mathf.Repeat(h + hueShift, 1f);

            Color newColor = Color.HSVToRGB(h, s, v);
            newColor.a = originalColors[i].a;

            targetImages[i].color = newColor;
        }
    }
}