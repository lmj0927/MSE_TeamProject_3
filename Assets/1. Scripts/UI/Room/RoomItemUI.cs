// Owned by MinJun Lee
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single room row with join button.
/// </summary>
public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText; // room title
    [SerializeField] private TextMeshProUGUI stageText; // stage label
    [SerializeField] private TextMeshProUGUI playerCountText; // player count
    [SerializeField] private TextMeshProUGUI currentPlayerBadgeText; // player badge
    [SerializeField] private Button joinButton; // join button

    RoomResponse _room; // bound room data
    IReadOnlyDictionary<string, int> _gameProgress; // user progress
    StageSO[] _stages; // stage definitions
    ErrorPopup _errorPopup; // error popup
    bool _joinBusy; // join in progress

    public RoomResponse Room => _room;
    public event Action<RoomResponse> RoomJoined; // join success event

    void Awake()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinClicked);
    }

    void OnDestroy()
    {
        if (joinButton != null)
            joinButton.onClick.RemoveListener(OnJoinClicked);
    }

    // apply room data to UI
    public void Bind(RoomResponse room, IReadOnlyDictionary<string, int> gameProgress = null, StageSO[] stages = null,
        ErrorPopup errorPopup = null)
    {
        _room = room;
        _gameProgress = gameProgress;
        _stages = stages;
        _errorPopup = errorPopup;
        RefreshDisplay();
        UpdateJoinInteractable();
    }

    void RefreshDisplay()
    {
        if (_room == null)
        {
            if (titleText != null) titleText.text = string.Empty;
            if (stageText != null) stageText.text = string.Empty;
            if (playerCountText != null) playerCountText.text = string.Empty;
            if (currentPlayerBadgeText != null) currentPlayerBadgeText.text = string.Empty;
            return;
        }

        if (titleText != null)
            titleText.text = _room.title ?? string.Empty;

        if (stageText != null)
            stageText.text = $"Stage : {_room.stage}";

        if (playerCountText != null)
            playerCountText.text = $"{_room.currentPlayerCount}/{_room.maxPlayers}";

        if (currentPlayerBadgeText != null)
            currentPlayerBadgeText.text = _room.currentPlayerCount.ToString();

        UpdateJoinInteractable();
    }

    void UpdateJoinInteractable()
    {
        if (joinButton == null)
            return;

        if (_joinBusy)
        {
            joinButton.interactable = false;
            return;
        }

        if (_room == null)
        {
            joinButton.interactable = false;
            return;
        }

        // disable join if stage is locked for this user
        joinButton.interactable = StageProgressGate.IsStageUnlocked(_room.stage, _gameProgress, _stages);
    }

    void OnJoinClicked()
    {
        JoinFlow().Forget();
    }

    async UniTaskVoid JoinFlow()
    {
        if (_room == null || string.IsNullOrEmpty(_room.roomId))
        {
            UserErrorPresenter.Show(_errorPopup, "Join room", "This room is no longer available.");
            return;
        }

        if (!NetworkManager.Instance.HasAccessToken)
        {
            UserErrorPresenter.Show(_errorPopup, "Join room", "You are not logged in. Please log in again.");
            return;
        }

        if (!StageProgressGate.IsStageUnlocked(_room.stage, _gameProgress, _stages))
        {
            UserErrorPresenter.Show(_errorPopup, "Join room",
                $"Clear Stage {_room.stage - 1} with at least 1 star before joining Stage {_room.stage}.");
            return;
        }

        SetJoinBusy(true);

        var result = await NetworkManager.Instance.JoinRoomAsync(_room.roomId, destroyCancellationToken);

        SetJoinBusy(false);

        if (result.Ok)
        {
            // refresh row with updated player count from server
            _room = result.Value;
            RefreshDisplay();
            Debug.Log(
                $"[RoomItemUI] Joined room id={_room.roomId} stage={_room.stage} players={_room.currentPlayerCount}/{_room.maxPlayers}");
            RoomJoined?.Invoke(_room);
            return;
        }

        UserErrorPresenter.ShowApiFailure(_errorPopup, "Join room", result.StatusCode, result.ErrorCode,
            result.ErrorMessage, result.RawBody);
    }

    void SetJoinBusy(bool busy)
    {
        _joinBusy = busy;
        UpdateJoinInteractable();
    }
}
