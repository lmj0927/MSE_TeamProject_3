using UnityEngine;
using UnityEngine.UI;

public class RadialProgressBar : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float progressValue;
    [SerializeField] private Image fillImage;

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

        fillImage.fillAmount = progressValue;
    }
}
