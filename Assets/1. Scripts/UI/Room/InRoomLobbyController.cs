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
    [SerializeField] private string joinRoomSceneName = "JoinRoom";
    [SerializeField] private float pollIntervalSeconds = 2.5f;

    readonly List<GameObject> _spawnedCharacters = new();
    string[] _lastParticipantIds = System.Array.Empty<string>();
    CancellationTokenSource _lobbyCts;
    bool _isLeavingScene;

    void Awake()
    {
        _lobbyCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    void Start()
    {
        if (!RoomSession.HasRoom)
        {
            Debug.LogWarning("[InRoomLobbyController] No room in RoomSession. Returning to JoinRoom.");
            ReturnToJoinRoom("No active room.");
            return;
        }

        ApplyRoomState(RoomSession.CurrentRoom);
        PollRoomLoop().Forget();
    }

    void OnDestroy()
    {
        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(OnLeaveClicked);

        _lobbyCts?.Cancel();
        _lobbyCts?.Dispose();
        ClearSpawnedCharacters();
    }

    void OnLeaveClicked()
    {
        LeaveRoomFlow().Forget();
    }

    async UniTaskVoid LeaveRoomFlow()
    {
        if (_isLeavingScene || !RoomSession.HasRoom)
            return;

        if (!NetworkManager.Instance.HasAccessToken)
        {
            Debug.LogWarning("[InRoomLobbyController] Not logged in.");
            return;
        }

        SetLeaveBusy(true);

        var roomId = RoomSession.RoomId;
        var result = await NetworkManager.Instance.LeaveRoomAsync(roomId, _lobbyCts.Token);

        SetLeaveBusy(false);

        if (!result.Ok)
        {
            Debug.LogError(
                $"[InRoomLobbyController] Leave failed | roomId={roomId} HTTP={result.StatusCode} code={result.ErrorCode} message={result.ErrorMessage} raw={result.RawBody}");
            return;
        }

        if (result.StatusCode == 204)
            ReturnToJoinRoom("Left room (host — room deleted).");
        else
            ReturnToJoinRoom("Left room.");
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
    }

    void RebuildCharacters(RoomResponse room)
    {
        ClearSpawnedCharacters();

        var ids = room.participantUserIds;
        if (ids == null || ids.Length == 0 || playerSlots == null || playerSlots.Length == 0)
            return;

        if (playerCharacterPrefab == null)
        {
            Debug.LogWarning("[InRoomLobbyController] playerCharacterPrefab is not assigned.");
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
                    ReturnToJoinRoom("Room was closed (host left or room no longer exists).");
                continue;
            }

            if (result.Value == null)
                continue;

            var ids = result.Value.participantUserIds;
            if (!ParticipantIdsEqual(_lastParticipantIds, ids))
                ApplyRoomState(result.Value);
        }
    }

    static bool IsRoomGone(int statusCode, string errorCode) =>
        statusCode == 404 || errorCode == "NOT_FOUND";

    void ReturnToJoinRoom(string reason)
    {
        if (_isLeavingScene)
            return;

        _isLeavingScene = true;
        _lobbyCts?.Cancel();
        RoomSession.Clear();
        Debug.Log($"[InRoomLobbyController] {reason} Loading {joinRoomSceneName}.");
        SceneManager.LoadScene(joinRoomSceneName);
    }

    void SetLeaveBusy(bool busy)
    {
        if (leaveButton != null)
            leaveButton.interactable = !busy;
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
