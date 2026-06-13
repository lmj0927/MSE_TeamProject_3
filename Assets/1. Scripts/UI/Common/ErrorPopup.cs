// Owned by MinJun Lee
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Popup that displays error messages.
/// </summary>
public class ErrorPopup : BasePopupUI
{
    [SerializeField] private TMP_Text errorText; // error message label
    [SerializeField] private Button closeButton; // close button

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
        // set text before playing show animation
        if (errorText != null)
            errorText.text = message ?? string.Empty;
        base.Show();
    }
}
