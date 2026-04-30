using UnityEngine;

public class ProgressBar : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float progressValue;
    [SerializeField] private RectTransform fill;

    public void SetProgress(float value)
    {
        progressValue = Mathf.Clamp01(value);
        ApplyProgress();
    }

    private void OnValidate()
    {
        progressValue = Mathf.Clamp01(progressValue);

        ApplyProgress();
    }

    private void ApplyProgress()
    {
        if (fill == null)
        {
            return;
        }

        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = progressValue;
        fill.anchorMax = anchorMax;
    }
}
