// Owned by MinJun Lee
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Popup base with scale show animation.
/// </summary>
public class BasePopupUI : MonoBehaviour, IBaseUI
{
    private Vector3 defaultScale; // original local scale
    private Tween showTween; // active show tween
    protected virtual void Awake()
    {
        defaultScale = transform.localScale;
        // start hidden until Show is called
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        ShowAnimation();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    protected virtual void ResetTop() { }

    protected virtual void OnShow() { }

    private void ShowAnimation()
    {
        ResetTop();

        transform.localScale = Vector3.zero;

        showTween?.Kill();
        // pop-in: overshoot then settle to default scale
        showTween = DOTween.Sequence()
            .Append(transform.DOScale(defaultScale * 1.1f, 0.12f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(defaultScale, 0.08f).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                OnShow();
            });
    }
}
