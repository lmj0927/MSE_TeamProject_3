using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// NetworkTest scene: register / login against <see cref="NetworkManager"/> using TMP_InputField and Button.
/// Builds uGUI and EventSystem at runtime.
/// </summary>
public class NetworkAuthTestUI : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:8080";
    private string joinRoomSceneName = "JoinRoom";

    TMP_InputField _userIdInput;
    TMP_InputField _passwordInput;
    TextMeshProUGUI _statusText;
    Button _registerButton;
    Button _loginButton;

    void Awake()
    {
        EnsureEventSystem();
        BuildUi();
    }

    void Start()
    {
        var net = NetworkManager.Instance;
        if (!string.IsNullOrWhiteSpace(serverUrl))
            net.BaseUrl = serverUrl;
        Log($"Server: {net.BaseUrl}");
    }

    void OnDestroy()
    {
        if (_registerButton != null) _registerButton.onClick.RemoveAllListeners();
        if (_loginButton != null) _loginButton.onClick.RemoveAllListeners();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        var uiModule = es.AddComponent<InputSystemUIInputModule>();
        uiModule.AssignDefaultActions();
    }

    void BuildUi()
    {
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            Debug.LogError("[NetworkAuthTestUI] TMP_Settings.defaultFontAsset is missing. Import TextMesh Pro essentials.");
            return;
        }

        var canvasGo = new GameObject("Canvas_NetworkAuthTest");
        canvasGo.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = CreateUiObject("Panel", canvasGo.transform);
        var panelRt = panel.GetComponent<RectTransform>();
        Stretch(panelRt, 40, 40, -40, -40);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(32, 32, 28, 28);
        vlg.spacing = 14;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;

        AddLabel(panel.transform, "Title", "Network API Test (Register / Login)", 28, font, true);

        _statusText = AddLabel(panel.transform, "Status", "Status: Idle", 18, font, false);
        var statusLe = _statusText.gameObject.AddComponent<LayoutElement>();
        statusLe.minHeight = 72;
        statusLe.preferredHeight = 72;

        AddLabel(panel.transform, "LblUserId", "User ID", 16, font, true);
        _userIdInput = CreateTmpInput(panel.transform, "Input_UserId", "3–64 characters", font, false);
        var leUser = _userIdInput.gameObject.AddComponent<LayoutElement>();
        leUser.minHeight = 52;
        leUser.preferredHeight = 52;

        AddLabel(panel.transform, "LblPassword", "Password", 16, font, true);
        _passwordInput = CreateTmpInput(panel.transform, "Input_Password", "8–128 characters", font, true);
        var lePw = _passwordInput.gameObject.AddComponent<LayoutElement>();
        lePw.minHeight = 52;
        lePw.preferredHeight = 52;

        var row = CreateUiObject("ButtonRow", panel.transform);
        var rowRt = row.GetComponent<RectTransform>();
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 56;
        rowLe.preferredHeight = 56;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;

        _registerButton = CreateButton(row.transform, "BtnRegister", "Register", font);
        _loginButton = CreateButton(row.transform, "BtnLogin", "Login", font);

        _registerButton.onClick.AddListener(OnRegisterClicked);
        _loginButton.onClick.AddListener(OnLoginClicked);
    }

    void Log(string msg)
    {
        if (_statusText != null)
            _statusText.text = "Status: " + msg;
        Debug.Log("[NetworkAuthTestUI] " + msg);
    }

    void SetBusy(bool busy)
    {
        _registerButton.interactable = !busy;
        _loginButton.interactable = !busy;
        _userIdInput.interactable = !busy;
        _passwordInput.interactable = !busy;
    }

    void OnRegisterClicked()
    {
        RegisterFlow().Forget();
    }

    void OnLoginClicked()
    {
        LoginFlow().Forget();
    }

    async UniTaskVoid RegisterFlow()
    {
        var userId = _userIdInput.text?.Trim() ?? string.Empty;
        var password = _passwordInput.text ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            Log("Enter user ID and password.");
            return;
        }

        SetBusy(true);
        Log("Registering…");
        var result = await NetworkManager.Instance.RegisterAsync(userId, password, destroyCancellationToken);
        SetBusy(false);
        if (result.Ok)
            Log("Register OK (201)");
        else
            ReportServerFailure("Register", result.StatusCode, result.ErrorCode, result.ErrorMessage, result.RawBody);
    }

    async UniTaskVoid LoginFlow()
    {
        var userId = _userIdInput.text?.Trim() ?? string.Empty;
        var password = _passwordInput.text ?? string.Empty;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
        {
            Log("Enter user ID and password.");
            return;
        }

        SetBusy(true);
        Log("Logging in…");
        var result = await NetworkManager.Instance.LoginAndStoreTokenAsync(userId, password, destroyCancellationToken);
        SetBusy(false);
        if (result.Ok)
        {
            Log($"Login OK. Loading {joinRoomSceneName}…");
            SceneManager.LoadScene(joinRoomSceneName);
            return;
        }

        ReportServerFailure("Login", result.StatusCode, result.ErrorCode, result.ErrorMessage, result.RawBody);
    }

    /// <summary>
    /// Server <c>message</c> is not shown on TMP (font / privacy); full details go to <see cref="Debug.LogError"/> only.
    /// </summary>
    void ReportServerFailure(string operation, int statusCode, string errorCode, string errorMessage, string rawBody)
    {
        var codePart = string.IsNullOrEmpty(errorCode) ? string.Empty : $", {errorCode}";
        var uiLine = $"{operation} failed (HTTP {statusCode}{codePart}). See Console.";
        if (_statusText != null)
            _statusText.text = "Status: " + uiLine;
        Debug.LogError(
            $"[NetworkAuthTestUI] {operation} failed | HTTP={statusCode} code={errorCode} message={errorMessage} raw={rawBody}");
    }

    static GameObject CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    static TextMeshProUGUI AddLabel(Transform parent, string name, string text, float fontSize, TMP_FontAsset font, bool bold)
    {
        var go = CreateUiObject(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = fontSize + 8;
        le.preferredHeight = fontSize + 8;
        return tmp;
    }

    static TMP_InputField CreateTmpInput(Transform parent, string name, string placeholder, TMP_FontAsset font, bool password)
    {
        var root = CreateUiObject(name, parent);
        var rootRt = root.GetComponent<RectTransform>();
        Stretch(rootRt, 0, 0, 0, 0);

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.22f, 1f);

        var input = root.AddComponent<TMP_InputField>();

        var textArea = CreateUiObject("Text Area", root.transform);
        var textAreaRt = textArea.GetComponent<RectTransform>();
        Stretch(textAreaRt, 12, 8, -12, -8);
        textArea.AddComponent<RectMask2D>();

        var textGo = CreateUiObject("Text", textArea.transform);
        var textRt = textGo.GetComponent<RectTransform>();
        Stretch(textRt, 0, 0, 0, 0);
        var textTmp = textGo.AddComponent<TextMeshProUGUI>();
        textTmp.font = font;
        textTmp.fontSize = 22;
        textTmp.color = Color.white;
        textTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var phGo = CreateUiObject("Placeholder", textArea.transform);
        var phRt = phGo.GetComponent<RectTransform>();
        Stretch(phRt, 0, 0, 0, 0);
        var phTmp = phGo.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.font = font;
        phTmp.fontSize = 22;
        phTmp.color = new Color(1f, 1f, 1f, 0.45f);
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;

        input.textViewport = textAreaRt;
        input.textComponent = textTmp;
        input.placeholder = phTmp;
        input.lineType = TMP_InputField.LineType.SingleLine;
        if (password)
            input.contentType = TMP_InputField.ContentType.Password;

        return input;
    }

    static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font)
    {
        var go = CreateUiObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.45f, 0.75f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = CreateUiObject("Text", go.transform);
        var textRt = textGo.GetComponent<RectTransform>();
        Stretch(textRt, 0, 0, 0, 0);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.font = font;
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
