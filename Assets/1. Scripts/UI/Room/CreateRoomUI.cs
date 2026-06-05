using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Room creation popup: <see cref="NetworkManager.CreateRoomAsync"/> with title, stage (dropdown), and max players (2–4).
/// Unlocked stages only (previous stage cleared with 1-star score).
/// </summary>
public class CreateRoomUI : BasePopupUI
{
    const int MinStage = 1;
    const int MinPlayers = 2;
    const int MaxPlayers = 4;
    const int MaxTitleLength = 128;

    static readonly Regex StageNumberRegex = new(@"\d+", RegexOptions.Compiled);

    [SerializeField] private TMP_InputField titleInputField;
    [SerializeField] private TMP_Dropdown stageDropdown;
    [SerializeField] private TMP_InputField maxPlayersInputField;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private StageSO[] stages;
    [SerializeField] private string defaultMaxPlayersText = "4";

    readonly List<int> _allStageNumbers = new();
    readonly List<string> _allStageLabels = new();
    readonly List<int> _unlockedStageNumbers = new();
    Dictionary<string, int> _gameProgress = new();

    public event Action<RoomResponse> RoomCreated;

    public RoomResponse LastCreatedRoom { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        BuildStageCatalog();

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

    void BuildStageCatalog()
    {
        _allStageNumbers.Clear();
        _allStageLabels.Clear();

        if (stages != null && stages.Length > 0)
        {
            for (var i = 0; i < stages.Length; i++)
            {
                var stageNumber = i + 1;
                _allStageNumbers.Add(stageNumber);
                _allStageLabels.Add(StageProgressGate.FormatStageLabel(stageNumber, stages[i]));
            }
            return;
        }

        if (stageDropdown?.options == null)
            return;

        foreach (var opt in stageDropdown.options)
        {
            if (!TryParseStageNumber(opt.text, out var stageNumber))
                continue;
            _allStageNumbers.Add(stageNumber);
            _allStageLabels.Add(opt.text);
        }
    }

    protected override void ResetTop()
    {
        if (titleInputField != null)
            titleInputField.text = string.Empty;
        if (maxPlayersInputField != null)
            maxPlayersInputField.text = defaultMaxPlayersText;
    }

    protected override void OnShow()
    {
        if (maxPlayersInputField != null && string.IsNullOrWhiteSpace(maxPlayersInputField.text))
            maxPlayersInputField.text = defaultMaxPlayersText;

        if (!NetworkManager.Instance.HasAccessToken)
            Debug.LogWarning("[CreateRoomUI] Not logged in. Create room will fail until a token is set.");

        RefreshStageDropdownFlow().Forget();
    }

    async UniTaskVoid RefreshStageDropdownFlow()
    {
        if (stageDropdown == null)
            return;

        SetBusy(true);

        _gameProgress = await FetchGameProgressAsync();
        ApplyUnlockedStageOptions();

        SetBusy(false);
    }

    async UniTask<Dictionary<string, int>> FetchGameProgressAsync()
    {
        if (!NetworkManager.Instance.HasAccessToken)
            return new Dictionary<string, int>();

        var result = await NetworkManager.Instance.GetMeAsync(destroyCancellationToken);
        if (!result.Ok)
        {
            Debug.LogWarning(
                $"[CreateRoomUI] GetMe failed; only Stage 1 will be available. {result.ErrorMessage}");
            return new Dictionary<string, int>();
        }

        return result.Value?.gameProgress ?? new Dictionary<string, int>();
    }

    void ApplyUnlockedStageOptions()
    {
        if (stageDropdown == null)
            return;

        _unlockedStageNumbers.Clear();
        var unlockedLabels = new List<string>();
        foreach (var stageNumber in _allStageNumbers)
        {
            if (!StageProgressGate.IsStageUnlocked(stageNumber, _gameProgress, stages))
                continue;
            _unlockedStageNumbers.Add(stageNumber);
            unlockedLabels.Add(GetLabelForStage(stageNumber));
        }

        if (unlockedLabels.Count == 0)
        {
            _unlockedStageNumbers.Add(MinStage);
            unlockedLabels.Add(GetLabelForStage(MinStage));
        }

        stageDropdown.ClearOptions();
        stageDropdown.AddOptions(unlockedLabels);
        stageDropdown.SetValueWithoutNotify(0);
        stageDropdown.RefreshShownValue();
    }

    string GetLabelForStage(int stageNumber)
    {
        var idx = _allStageNumbers.IndexOf(stageNumber);
        if (idx >= 0)
            return _allStageLabels[idx];
        return StageProgressGate.FormatStageLabel(stageNumber, StageProgressGate.GetStageDef(stages, stageNumber));
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

        if (!TryReadStageFromDropdown(out stage, out errorMessage))
        {
            maxPlayers = 0;
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

    bool TryReadStageFromDropdown(out int stage, out string errorMessage)
    {
        stage = 0;
        errorMessage = null;

        if (stageDropdown == null || stageDropdown.options == null || stageDropdown.options.Count == 0
            || _unlockedStageNumbers.Count == 0)
        {
            errorMessage = "No unlocked stage available.";
            return false;
        }

        var index = Mathf.Clamp(stageDropdown.value, 0, _unlockedStageNumbers.Count - 1);
        stage = _unlockedStageNumbers[index];

        if (stage < MinStage)
        {
            errorMessage = $"Stage must be at least {MinStage}.";
            return false;
        }

        if (!StageProgressGate.IsStageUnlocked(stage, _gameProgress, stages))
        {
            errorMessage = $"Clear Stage {stage - 1} with at least 1 star before selecting Stage {stage}.";
            return false;
        }

        return true;
    }

    /// <summary>Extracts the last number from option labels such as "Level 1" or "Level 2".</summary>
    static bool TryParseStageNumber(string optionText, out int stage)
    {
        stage = 0;
        if (string.IsNullOrWhiteSpace(optionText))
            return false;

        var matches = StageNumberRegex.Matches(optionText);
        if (matches.Count == 0)
            return false;

        return int.TryParse(matches[matches.Count - 1].Value, out stage);
    }

    void SetBusy(bool busy)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = !busy;
        if (closeButton != null)
            closeButton.interactable = !busy;
        if (titleInputField != null)
            titleInputField.interactable = !busy;
        if (stageDropdown != null)
            stageDropdown.interactable = !busy;
        if (maxPlayersInputField != null)
            maxPlayersInputField.interactable = !busy;
    }
}
