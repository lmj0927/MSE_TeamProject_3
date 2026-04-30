using UnityEngine;
using minjun;
using System.Collections;
using DG.Tweening;

public class BasePopupUI : MonoBehaviour, IBaseUI
{
    private Vector3 defaultScale;
    private Tween showTween;
    protected virtual void Awake()
    {
        defaultScale = transform.localScale;
        transform.localScale = Vector3.zero;
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

    protected virtual void OnShow() {}

    private void ShowAnimation()
    {
        transform.localScale = Vector3.zero;
        showTween?.Kill();
        showTween = DOTween.Sequence()
            .Append(transform.DOScale(defaultScale * 1.1f, 0.12f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(defaultScale, 0.08f).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                OnShow();
            });
    }
}
