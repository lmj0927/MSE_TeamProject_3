// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// Horizontal fill progress bar.
/// </summary>
public class ProgressBar : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float progressValue; // current progress 0-1
    [SerializeField] private RectTransform fill; // fill rect transform

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

        // stretch fill width via anchorMax.x
        Vector2 anchorMax = fill.anchorMax;
        anchorMax.x = progressValue;
        fill.anchorMax = anchorMax;
    }
}
