using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ErrorPopup : BasePopupUI
{
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
    }

    void OnCloseClicked()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (errorText != null)
            errorText.text = message ?? string.Empty;
        base.Show();
    }
}
