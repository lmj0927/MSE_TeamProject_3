using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogInUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button logInButton;
    [SerializeField] private Button signUpButton;
    [SerializeField] private SignUpUI signUpUI;
    [SerializeField] private ErrorPopup errorPopup;
    [SerializeField] private string joinRoomSceneName = "JoinRoom";

    void Awake()
    {
        ConfigureUserIdInput(idInputField);

        if (logInButton != null)
            logInButton.onClick.AddListener(OnLogInClicked);
        if (signUpButton != null)
            signUpButton.onClick.AddListener(OnSignUpClicked);
    }

    static void ConfigureUserIdInput(TMP_InputField field)
    {
        if (field == null)
            return;

        field.contentType = TMP_InputField.ContentType.Standard;
        field.characterValidation = TMP_InputField.CharacterValidation.None;
        field.lineType = TMP_InputField.LineType.SingleLine;
    }

    void OnDestroy()
    {
        if (logInButton != null)
            logInButton.onClick.RemoveListener(OnLogInClicked);
        if (signUpButton != null)
            signUpButton.onClick.RemoveListener(OnSignUpClicked);
    }

    void OnLogInClicked()
    {
        LogInFlow().Forget();
    }

    void OnSignUpClicked()
    {
        if (signUpUI == null)
        {
            UserErrorPresenter.Show(errorPopup, "Sign up", "Sign-up screen is not available.");
            return;
        }

        signUpUI.Show();
    }

    async UniTaskVoid LogInFlow()
    {
        var userId = idInputField != null ? idInputField.text.Trim() : string.Empty;
        var password = passwordInputField != null ? passwordInputField.text : string.Empty;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            UserErrorPresenter.Show(errorPopup, "Login", "Enter your user ID and password.");
            return;
        }

        SetBusy(true);

        var result = await NetworkManager.Instance.LoginAndStoreTokenAsync(userId, password, destroyCancellationToken);

        SetBusy(false);

        if (result.Ok)
        {
            Debug.Log($"[LogInUI] Login OK. Loading {joinRoomSceneName}.");
            SceneManager.LoadScene(joinRoomSceneName);
            return;
        }

        if (result.ErrorMessage == "Missing token in response")
        {
            UserErrorPresenter.Show(errorPopup, "Login", "Login response was invalid. Please try again.");
            Debug.LogError($"[LogInUI] Login failed | raw={result.RawBody}");
            return;
        }

        UserErrorPresenter.ShowApiFailure(errorPopup, "Login", result.StatusCode, result.ErrorCode,
            result.ErrorMessage, result.RawBody);
    }

    void SetBusy(bool busy)
    {
        if (logInButton != null)
            logInButton.interactable = !busy;
        if (signUpButton != null)
            signUpButton.interactable = !busy;
        if (idInputField != null)
            idInputField.interactable = !busy;
        if (passwordInputField != null)
            passwordInputField.interactable = !busy;
    }
}
