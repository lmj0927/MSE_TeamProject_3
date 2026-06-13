// Owned by MinJun Lee
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sign up popup UI.
/// </summary>
public class SignUpUI : BasePopupUI
{
    [SerializeField] private TMP_InputField idInputField; // user id input
    [SerializeField] private TMP_InputField passwordInputField; // password input
    [SerializeField] private Button signUpButton; // sign up button
    [SerializeField] private Button closeButton; // close button
    [SerializeField] private ErrorPopup errorPopup; // error popup

    protected override void Awake()
    {
        base.Awake();
        ConfigureUserIdInput(idInputField);

        if (signUpButton != null)
            signUpButton.onClick.AddListener(OnSignUpClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    void OnDestroy()
    {
        if (signUpButton != null)
            signUpButton.onClick.RemoveListener(OnSignUpClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    protected override void ResetTop()
    {
        if (idInputField != null)
            idInputField.text = string.Empty;
        if (passwordInputField != null)
            passwordInputField.text = string.Empty;
    }

    void OnSignUpClicked()
    {
        RegisterFlow().Forget();
    }

    static void ConfigureUserIdInput(TMP_InputField field)
    {
        if (field == null)
            return;

        field.contentType = TMP_InputField.ContentType.Standard;
        field.characterValidation = TMP_InputField.CharacterValidation.None;
        field.lineType = TMP_InputField.LineType.SingleLine;
    }

    async UniTaskVoid RegisterFlow()
    {
        var userId = idInputField != null ? idInputField.text.Trim() : string.Empty;
        var password = passwordInputField != null ? passwordInputField.text : string.Empty;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            UserErrorPresenter.Show(errorPopup, "Sign up", "Enter your user ID and password.");
            return;
        }

        // client-side length check before API call
        if (userId.Length < 3)
        {
            UserErrorPresenter.Show(errorPopup, "Sign up", "User ID must be 3–64 characters.");
            return;
        }

        if (password.Length < 8)
        {
            UserErrorPresenter.Show(errorPopup, "Sign up", "Password must be 8–128 characters.");
            return;
        }

        SetBusy(true);

        var result = await NetworkManager.Instance.RegisterAsync(userId, password, destroyCancellationToken);

        SetBusy(false);

        if (result.Ok)
        {
            Debug.Log("[SignUpUI] Register OK (201).");
            Hide();
            return;
        }

        UserErrorPresenter.ShowApiFailure(errorPopup, "Sign up", result.StatusCode, result.ErrorCode,
            result.ErrorMessage, result.RawBody);
    }

    void SetBusy(bool busy)
    {
        if (signUpButton != null)
            signUpButton.interactable = !busy;
        if (closeButton != null)
            closeButton.interactable = !busy;
        if (idInputField != null)
            idInputField.interactable = !busy;
        if (passwordInputField != null)
            passwordInputField.interactable = !busy;
    }
}
