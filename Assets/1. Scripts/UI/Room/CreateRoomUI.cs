using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Room creation popup: <see cref="NetworkManager.CreateRoomAsync"/> with title, stage (≥1), and max players (2–4).
/// </summary>
public class CreateRoomUI : BasePopupUI
{
    const int MinStage = 1;
    const int MinPlayers = 2;
    const int MaxPlayers = 4;
    const int MaxTitleLength = 128;

    [SerializeField] private TMP_InputField titleInputField;
    [SerializeField] private TMP_InputField stageInputField;
    [SerializeField] private TMP_InputField maxPlayersInputField;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private string defaultStageText = "1";
    [SerializeField] private string defaultMaxPlayersText = "4";

    public event Action<RoomResponse> RoomCreated;

    public RoomResponse LastCreatedRoom { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    void OnDestroy()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.RemoveListener(OnCreateClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    protected override void ResetTop()
    {
        if (titleInputField != null)
            titleInputField.text = string.Empty;
        if (stageInputField != null)
            stageInputField.text = defaultStageText;
        if (maxPlayersInputField != null)
            maxPlayersInputField.text = defaultMaxPlayersText;
    }

    protected override void OnShow()
    {
        if (stageInputField != null && string.IsNullOrWhiteSpace(stageInputField.text))
            stageInputField.text = defaultStageText;
        if (maxPlayersInputField != null && string.IsNullOrWhiteSpace(maxPlayersInputField.text))
            maxPlayersInputField.text = defaultMaxPlayersText;

        if (!NetworkManager.Instance.HasAccessToken)
            Debug.LogWarning("[CreateRoomUI] Not logged in. Create room will fail until a token is set.");
    }

    void OnCreateClicked()
    {
        CreateRoomFlow().Forget();
    }

    async UniTaskVoid CreateRoomFlow()
    {
        if (!TryReadInputs(out var title, out var stage, out var maxPlayers, out var validationMessage))
        {
            Debug.LogWarning($"[CreateRoomUI] {validationMessage}");
            return;
        }

        if (!NetworkManager.Instance.HasAccessToken)
        {
            Debug.LogWarning("[CreateRoomUI] Not logged in.");
            return;
        }

        SetBusy(true);

        var result = await NetworkManager.Instance.CreateRoomAsync(title, stage, maxPlayers, destroyCancellationToken);

        SetBusy(false);

        if (result.Ok)
        {
            LastCreatedRoom = result.Value;
            Debug.Log(
                $"[CreateRoomUI] Room created id={result.Value?.roomId} title={result.Value?.title} stage={result.Value?.stage}");
            RoomCreated?.Invoke(result.Value);
            Hide();
            return;
        }

        Debug.LogError(
            $"[CreateRoomUI] Create room failed | HTTP={result.StatusCode} code={result.ErrorCode} message={result.ErrorMessage} raw={result.RawBody}");
    }

    bool TryReadInputs(out string title, out int stage, out int maxPlayers, out string errorMessage)
    {
        title = titleInputField != null ? titleInputField.text.Trim() : string.Empty;
        var stageText = stageInputField != null ? stageInputField.text.Trim() : string.Empty;
        var maxText = maxPlayersInputField != null ? maxPlayersInputField.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(title))
        {
            stage = 0;
            maxPlayers = 0;
            errorMessage = "Enter a room title.";
            return false;
        }

        if (title.Length > MaxTitleLength)
        {
            stage = 0;
            maxPlayers = 0;
            errorMessage = $"Title must be at most {MaxTitleLength} characters.";
            return false;
        }

        if (!int.TryParse(stageText, out stage))
        {
            maxPlayers = 0;
            errorMessage = "Stage must be a number.";
            return false;
        }

        if (stage < MinStage)
        {
            maxPlayers = 0;
            errorMessage = $"Stage must be at least {MinStage}.";
            return false;
        }

        if (!int.TryParse(maxText, out maxPlayers))
        {
            errorMessage = "Max players must be a number.";
            return false;
        }

        if (maxPlayers < MinPlayers || maxPlayers > MaxPlayers)
        {
            errorMessage = $"Max players must be between {MinPlayers} and {MaxPlayers}.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    void SetBusy(bool busy)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = !busy;
        if (closeButton != null)
            closeButton.interactable = !busy;
        if (titleInputField != null)
            titleInputField.interactable = !busy;
        if (stageInputField != null)
            stageInputField.interactable = !busy;
        if (maxPlayersInputField != null)
            maxPlayersInputField.interactable = !busy;
    }
}
