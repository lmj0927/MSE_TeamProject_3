// Owned by MinJun Lee
using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Radial fill progress bar using Image fillAmount.
/// </summary>
public class RadialProgressBar : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float progressValue; // current progress 0-1
    [SerializeField] private Image fillImage; // radial fill image

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
        if (fillImage == null)
        {
            return;
        }

        // radial Image type uses fillAmount 0-1
        fillImage.fillAmount = progressValue;
    }
}
