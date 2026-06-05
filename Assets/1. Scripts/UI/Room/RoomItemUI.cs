using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single room row: displays <see cref="RoomResponse"/> and joins via <see cref="NetworkManager.JoinRoomAsync"/>.
/// List refresh is handled by <see cref="JoinRoomUI"/>.
/// </summary>
public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI currentPlayerBadgeText;
    [SerializeField] private Button joinButton;

    RoomResponse _room;
    IReadOnlyDictionary<string, int> _gameProgress;
    StageSO[] _stages;
    bool _joinBusy;

    public RoomResponse Room => _room;

    public event Action<RoomResponse> RoomJoined;

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

    /// <summary>Apply room data to title, player count, and badge.</summary>
    public void Bind(RoomResponse room, IReadOnlyDictionary<string, int> gameProgress = null, StageSO[] stages = null)
    {
        _room = room;
        _gameProgress = gameProgress;
        _stages = stages;
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
            Debug.LogWarning("[RoomItemUI] No room bound.");
            return;
        }

        if (!NetworkManager.Instance.HasAccessToken)
        {
            Debug.LogWarning("[RoomItemUI] Not logged in.");
            return;
        }

        if (!StageProgressGate.IsStageUnlocked(_room.stage, _gameProgress, _stages))
        {
            Debug.LogWarning(
                $"[RoomItemUI] Stage {_room.stage} is locked. Clear the previous stage with at least 1 star first.");
            return;
        }

        SetJoinBusy(true);

        var result = await NetworkManager.Instance.JoinRoomAsync(_room.roomId, destroyCancellationToken);

        SetJoinBusy(false);

        if (result.Ok)
        {
            _room = result.Value;
            RefreshDisplay();
            Debug.Log(
                $"[RoomItemUI] Joined room id={_room.roomId} stage={_room.stage} players={_room.currentPlayerCount}/{_room.maxPlayers}");
            RoomJoined?.Invoke(_room);
            return;
        }

        Debug.LogError(
            $"[RoomItemUI] Join failed | roomId={_room.roomId} HTTP={result.StatusCode} code={result.ErrorCode} message={result.ErrorMessage} raw={result.RawBody}");
    }

    void SetJoinBusy(bool busy)
    {
        _joinBusy = busy;
        UpdateJoinInteractable();
    }
}
