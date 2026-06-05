using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lobby hub: opens <see cref="CreateRoomUI"/>, refreshes open rooms, enters <see cref="RoomSession"/> + InRoom scene on create/join.
/// </summary>
public class JoinRoomUI : MonoBehaviour
{
    [SerializeField] private Button createButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private CreateRoomUI createRoomUI;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private RoomItemUI roomItemPrefab;
    [SerializeField] private StageSO[] stages;
    [SerializeField] private string inRoomSceneName = "InRoom";

    readonly List<RoomItemUI> _spawnedItems = new();
    Dictionary<string, int> _cachedGameProgress = new();

    void Awake()
    {
        if (createButton != null)
            createButton.onClick.AddListener(OnCreateClicked);
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshClicked);

        if (createRoomUI != null)
            createRoomUI.RoomCreated += OnRoomCreated;

        RefreshRoomListFlow().Forget();
    }

    void OnDestroy()
    {
        if (createButton != null)
            createButton.onClick.RemoveListener(OnCreateClicked);
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(OnRefreshClicked);

        if (createRoomUI != null)
            createRoomUI.RoomCreated -= OnRoomCreated;

        ClearRoomList();
    }

    void OnCreateClicked()
    {
        if (createRoomUI == null)
        {
            Debug.LogWarning("[JoinRoomUI] CreateRoomUI is not assigned.");
            return;
        }

        createRoomUI.Show();
    }

    void OnRefreshClicked()
    {
        RefreshRoomListFlow().Forget();
    }

    void OnRoomCreated(RoomResponse room)
    {
        EnterInRoom(room);
    }

    void OnRoomItemJoined(RoomResponse room)
    {
        EnterInRoom(room);
    }

    void EnterInRoom(RoomResponse room)
    {
        if (room == null || string.IsNullOrEmpty(room.roomId))
        {
            Debug.LogWarning("[JoinRoomUI] Cannot enter InRoom: invalid room.");
            return;
        }

        if (!NetworkManager.Instance.HasAccessToken)
        {
            Debug.LogWarning("[JoinRoomUI] Not logged in.");
            return;
        }

        var localUserId = NetworkManager.Instance.LocalUserId;
        if (string.IsNullOrEmpty(localUserId))
        {
            Debug.LogWarning("[JoinRoomUI] Local user id is missing. Log in again.");
            return;
        }

        RoomSession.Enter(room, localUserId);
        Debug.Log($"[JoinRoomUI] Entering InRoom roomId={room.roomId} stage={room.stage}");
        SceneManager.LoadScene(inRoomSceneName);
    }

    async UniTaskVoid RefreshRoomListFlow()
    {
        if (!NetworkManager.Instance.HasAccessToken)
        {
            Debug.LogWarning("[JoinRoomUI] Not logged in.");
            return;
        }

        SetBusy(true);

        var roomsTask = NetworkManager.Instance.GetOpenRoomsAsync(destroyCancellationToken);
        var meTask = NetworkManager.Instance.GetMeAsync(destroyCancellationToken);
        var (roomsResult, meResult) = await UniTask.WhenAll(roomsTask, meTask);

        SetBusy(false);

        if (!roomsResult.Ok)
        {
            Debug.LogError(
                $"[JoinRoomUI] Get open rooms failed | HTTP={roomsResult.StatusCode} code={roomsResult.ErrorCode} message={roomsResult.ErrorMessage} raw={roomsResult.RawBody}");
            return;
        }

        _cachedGameProgress = meResult.Ok && meResult.Value?.gameProgress != null
            ? new Dictionary<string, int>(meResult.Value.gameProgress)
            : new Dictionary<string, int>();

        RebuildRoomList(roomsResult.Value);
        var count = roomsResult.Value?.Length ?? 0;
        Debug.Log($"[JoinRoomUI] Room list refreshed count={count}");
    }

    void RebuildRoomList(RoomResponse[] rooms)
    {
        ClearRoomList();

        if (rooms == null || rooms.Length == 0)
            return;

        if (roomListContent == null || roomItemPrefab == null)
        {
            Debug.LogWarning("[JoinRoomUI] roomListContent or roomItemPrefab is not assigned.");
            return;
        }

        foreach (var room in rooms)
        {
            if (room == null)
                continue;

            var item = Instantiate(roomItemPrefab, roomListContent);
            item.Bind(room, _cachedGameProgress, stages);
            item.RoomJoined += OnRoomItemJoined;
            _spawnedItems.Add(item);
        }
    }

    void ClearRoomList()
    {
        for (var i = _spawnedItems.Count - 1; i >= 0; i--)
        {
            var item = _spawnedItems[i];
            if (item != null)
            {
                item.RoomJoined -= OnRoomItemJoined;
                Destroy(item.gameObject);
            }
        }

        _spawnedItems.Clear();
    }

    void SetBusy(bool busy)
    {
        if (createButton != null)
            createButton.interactable = !busy;
        if (refreshButton != null)
            refreshButton.interactable = !busy;
    }
}
