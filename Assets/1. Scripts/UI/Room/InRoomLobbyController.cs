using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// InRoom waiting lobby: character slots, leave room, poll join for roster / detect host-disbanded room (404).
/// </summary>
public class InRoomLobbyController : MonoBehaviour
{
    [SerializeField] private Transform[] playerSlots;
    [SerializeField] private GameObject playerCharacterPrefab;
    [SerializeField] private TextMeshProUGUI roomTitleText;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startButton;   // 게임 시작 → Photon 세션 진입
    [SerializeField] private ErrorPopup errorPopup;
    [SerializeField] private string joinRoomSceneName = "JoinRoom";
    [SerializeField] private float pollIntervalSeconds = 2.5f;

    readonly List<GameObject> _spawnedCharacters = new();
    string[] _lastParticipantIds = System.Array.Empty<string>();
    string _lastRoomStatus;
    CancellationTokenSource _lobbyCts;
    bool _isLeavingScene;
    bool _photonSessionLaunched;

    void Awake()
    {
        _lobbyCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
    }

    void Start()
    {
        if (!RoomSession.HasRoom)
        {
            ReturnToJoinRoom("No active room session was found.");
            return;
        }

        ApplyRoomState(RoomSession.CurrentRoom);
        PollRoomLoop().Forget();
    }

    void OnDestroy()
    {
        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(OnLeaveClicked);
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartClicked);

        _lobbyCts?.Cancel();
        _lobbyCts?.Dispose();
        ClearSpawnedCharacters();
    }

    void OnLeaveClicked()
    {
        LeaveRoomFlow().Forget();
    }

    void OnStartClicked()
    {
        StartRoomFlow().Forget();
    }

    async UniTaskVoid StartRoomFlow()
    {
        if (_isLeavingScene || !RoomSession.HasRoom)
            return;

        var room = RoomSession.CurrentRoom;
        if (!IsLocalHost(room))
        {
            UserErrorPresenter.Show(errorPopup, "Start game", "Only the host can start the game.");
            return;
        }

        if (!IsRoomOpen(room))
        {
            UserErrorPresenter.Show(errorPopup, "Start game", "This room is no longer open.");
            return;
        }

        if (!NetworkManager.Instance.HasAccessToken)
        {
            UserErrorPresenter.Show(errorPopup, "Start game", "You are not logged in. Please log in again.");
            return;
        }

        SetStartBusy(true);

        var roomId = RoomSession.RoomId;
        var result = await NetworkManager.Instance.StartRoomAsync(roomId, _lobbyCts.Token);

        SetStartBusy(false);

        if (!result.Ok)
        {
            UserErrorPresenter.ShowApiFailure(errorPopup, "Start game", result.StatusCode, result.ErrorCode,
                result.ErrorMessage, result.RawBody);
            return;
        }

        ApplyRoomState(result.Value);
        Debug.Log($"[InRoomLobbyController] Game started roomId={roomId} status={result.Value?.status}");
    }

    async UniTaskVoid LeaveRoomFlow()
    {
        if (_isLeavingScene || !RoomSession.HasRoom)
            return;

        if (!NetworkManager.Instance.HasAccessToken)
        {
            UserErrorPresenter.Show(errorPopup, "Leave room", "You are not logged in. Please log in again.");
            return;
        }

        SetLeaveBusy(true);

        var roomId = RoomSession.RoomId;
        var result = await NetworkManager.Instance.LeaveRoomAsync(roomId, _lobbyCts.Token);

        SetLeaveBusy(false);

        if (!result.Ok)
        {
            UserErrorPresenter.ShowApiFailure(errorPopup, "Leave room", result.StatusCode, result.ErrorCode,
                result.ErrorMessage, result.RawBody);
            return;
        }

        if (result.StatusCode == 204)
            ReturnToJoinRoom("You left the room. The room was closed because you were the host.");
        else
            ReturnToJoinRoom("You left the room.");
    }

    void ApplyRoomState(RoomResponse room)
    {
        if (room == null)
            return;

        RoomSession.UpdateRoom(room);

        if (roomTitleText != null)
            roomTitleText.text = $"{room.title}  |  Stage : {room.stage}";

        RebuildCharacters(room);
        _lastParticipantIds = CopyParticipantIds(room.participantUserIds);
        _lastRoomStatus = room.status;
        UpdateStartButton(room);
        TryEnterPhotonSession(room);
    }

    void TryEnterPhotonSession(RoomResponse room)
    {
        if (_photonSessionLaunched || _isLeavingScene || room == null)
            return;

        if (!IsRoomInProgress(room))
            return;

        if (string.IsNullOrEmpty(room.roomId))
            return;

        if (GameLauncher.IsRunning)
        {
            _photonSessionLaunched = true;
            return;
        }

        _photonSessionLaunched = true;
        _lobbyCts?.Cancel();
        Debug.Log($"[InRoomLobbyController] Room IN_PROGRESS → launching Photon session roomId={room.roomId}");
        GameLauncher.Launch(room.roomId);
    }

    void UpdateStartButton(RoomResponse room)
    {
        if (startButton == null)
            return;

        var isHost = IsLocalHost(room);
        var canStart = isHost && IsRoomOpen(room);
        startButton.interactable = canStart;
        startButton.gameObject.SetActive(isHost);
    }

    static bool IsLocalHost(RoomResponse room)
    {
        if (room == null || string.IsNullOrEmpty(room.hostUserId))
            return false;

        var localUserId = NetworkManager.Instance.LocalUserId;
        if (string.IsNullOrEmpty(localUserId))
            localUserId = RoomSession.LocalUserId;

        return room.hostUserId == localUserId;
    }

    static bool IsRoomOpen(RoomResponse room) =>
        room != null && string.Equals(room.status, "OPEN", System.StringComparison.OrdinalIgnoreCase);

    static bool IsRoomInProgress(RoomResponse room) =>
        room != null && string.Equals(room.status, "IN_PROGRESS", System.StringComparison.OrdinalIgnoreCase);

    void RebuildCharacters(RoomResponse room)
    {
        ClearSpawnedCharacters();

        var ids = room.participantUserIds;
        if (ids == null || ids.Length == 0 || playerSlots == null || playerSlots.Length == 0)
            return;

        if (playerCharacterPrefab == null)
        {
            UserErrorPresenter.Show(errorPopup, "In-room lobby", "Player display is not configured.");
            return;
        }

        var count = Mathf.Min(ids.Length, playerSlots.Length);
        for (var i = 0; i < count; i++)
        {
            if (string.IsNullOrEmpty(ids[i]) || playerSlots[i] == null)
                continue;

            var slot = playerSlots[i];
            var character = Instantiate(playerCharacterPrefab, slot.position, slot.rotation, slot);
            character.name = $"Player_{ids[i]}";
            _spawnedCharacters.Add(character);
        }

        Debug.Log($"[InRoomLobbyController] Spawned {count} character(s) for room {room.roomId}");
    }

    void ClearSpawnedCharacters()
    {
        for (var i = _spawnedCharacters.Count - 1; i >= 0; i--)
        {
            if (_spawnedCharacters[i] != null)
                Destroy(_spawnedCharacters[i]);
        }

        _spawnedCharacters.Clear();
    }

    async UniTaskVoid PollRoomLoop()
    {
        var interval = Mathf.Max(0.5f, pollIntervalSeconds);
        var roomId = RoomSession.RoomId;
        var token = _lobbyCts.Token;

        while (!token.IsCancellationRequested && !string.IsNullOrEmpty(roomId))
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(interval), cancellationToken: token);

            if (_isLeavingScene || !NetworkManager.Instance.HasAccessToken)
                continue;

            var result = await NetworkManager.Instance.JoinRoomAsync(roomId, token);
            if (!result.Ok)
            {
                if (IsRoomGone(result.StatusCode, result.ErrorCode))
                    ReturnToJoinRoom("This room was closed. The host may have left.");
                continue;
            }

            if (result.Value == null)
                continue;

            var ids = result.Value.participantUserIds;
            var status = result.Value.status;
            if (!ParticipantIdsEqual(_lastParticipantIds, ids) || status != _lastRoomStatus)
                ApplyRoomState(result.Value);
        }
    }

    static bool IsRoomGone(int statusCode, string errorCode) =>
        statusCode == 404 || errorCode == "NOT_FOUND";

    void ReturnToJoinRoom(string userMessage)
    {
        if (_isLeavingScene)
            return;

        _isLeavingScene = true;
        _lobbyCts?.Cancel();
        RoomSession.Clear();
        UserErrorPresenter.SetPending(userMessage);
        Debug.Log($"[InRoomLobbyController] {userMessage} Loading {joinRoomSceneName}.");
        SceneManager.LoadScene(joinRoomSceneName);
    }

    void SetLeaveBusy(bool busy)
    {
        if (leaveButton != null)
            leaveButton.interactable = !busy;
    }

    void SetStartBusy(bool busy)
    {
        if (startButton == null || !RoomSession.HasRoom)
            return;

        if (busy)
        {
            startButton.interactable = false;
            return;
        }

        UpdateStartButton(RoomSession.CurrentRoom);
    }

    static bool ParticipantIdsEqual(string[] a, string[] b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        if (a.Length != b.Length)
            return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    static string[] CopyParticipantIds(string[] ids) =>
        ids == null ? System.Array.Empty<string>() : (string[])ids.Clone();
}
