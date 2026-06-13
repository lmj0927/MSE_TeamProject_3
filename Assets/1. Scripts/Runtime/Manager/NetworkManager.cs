// Owned by MinJun Lee
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// MSE Server REST API client. JWT stored in memory after login.
/// </summary>
public class NetworkManager : Singleton<NetworkManager>
{
    [SerializeField] private bool useLocal = true; // use local server flag
    [SerializeField] private string baseUrl = "http://localhost:8080"; // local server URL
    [SerializeField] private string awsUrl = "https://mse-server.onrender.com"; // remote server URL

    const string DefaultLocalUrl = "http://localhost:8080";
    const string DefaultAwsUrl = "https://mse-server.onrender.com";

    public bool UseLocal => useLocal;

    public string BaseUrl
    {
        get => ResolveBaseUrl();
        set => baseUrl = string.IsNullOrWhiteSpace(value) ? DefaultLocalUrl : value.TrimEnd('/');
    }

    public string AccessToken { get; private set; } // JWT access token
    public string LocalUserId { get; private set; } // logged in user id
    public bool HasAccessToken => !string.IsNullOrEmpty(AccessToken);

    public void SetAccessToken(string token) => AccessToken = token;
    public void SetLocalUserId(string userId) => LocalUserId = userId ?? string.Empty;

    public void ClearAccessToken()
    {
        AccessToken = null;
        LocalUserId = null;
    }

    string Root => ResolveBaseUrl();

    // pick local or aws base URL
    string ResolveBaseUrl()
    {
        if (useLocal)
            return string.IsNullOrWhiteSpace(baseUrl) ? DefaultLocalUrl : baseUrl.TrimEnd('/');
        return string.IsNullOrWhiteSpace(awsUrl) ? DefaultAwsUrl : awsUrl.TrimEnd('/');
    }

    protected override void Initialize()
    {
        base.Initialize();
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = DefaultLocalUrl;
        if (string.IsNullOrWhiteSpace(awsUrl))
            awsUrl = DefaultAwsUrl;
        Debug.Log($"[NetworkManager] useLocal={useLocal} BaseUrl={BaseUrl}");
    }

    // wait until web request completes without throwing on HTTP errors
    static async UniTask SendWebRequestAsync(UnityWebRequest request, CancellationToken cancellationToken)
    {
        var op = request.SendWebRequest();
        try
        {
            await UniTask.WaitUntil(() => op.isDone, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // abort in-flight request on cancel
            if (!op.isDone)
                request.Abort();
            throw;
        }
    }

    static void SetBearerIfNeeded(UnityWebRequest request, bool useAuth, string token)
    {
        if (useAuth && !string.IsNullOrEmpty(token))
            request.SetRequestHeader("Authorization", "Bearer " + token);
    }

    static ApiErrorBody TryParseApiError(string body)
    {
        if (string.IsNullOrEmpty(body))
            return null;
        try
        {
            return JsonUtility.FromJson<ApiErrorBody>(body);
        }
        catch (Exception)
        {
            return null;
        }
    }

    static ApiResult<T> FailureFromRequest<T>(UnityWebRequest req, string body)
    {
        var api = TryParseApiError(body);
        var status = (int)req.responseCode;
        var message = api?.message;
        if (string.IsNullOrEmpty(message))
            message = string.IsNullOrEmpty(req.error) ? body : req.error;
        return ApiResult<T>.Failure(status, api?.code, message ?? string.Empty, body ?? string.Empty);
    }

    static ApiResult<bool> FailureVoid(UnityWebRequest req, string body)
    {
        var api = TryParseApiError(body);
        var status = (int)req.responseCode;
        var message = api?.message;
        if (string.IsNullOrEmpty(message))
            message = string.IsNullOrEmpty(req.error) ? body : req.error;
        return ApiResult.Failure(status, api?.code, message ?? string.Empty, body ?? string.Empty);
    }

    static bool IsNetworkFailure(UnityWebRequest req)
    {
        return req.result == UnityWebRequest.Result.ConnectionError
               || req.result == UnityWebRequest.Result.DataProcessingError;
    }

    // POST /api/auth/register
    public async UniTask<ApiResult<bool>> RegisterAsync(string userId, string password,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonUtility.ToJson(new AuthRequestBody { userId = userId, password = password });
        using var req = new UnityWebRequest(Root + "/api/auth/register", UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 201)
            return ApiResult.SuccessNoContent(201);

        return FailureVoid(req, body);
    }

    // POST /api/auth/login — caller stores token
    public async UniTask<ApiResult<string>> LoginAsync(string userId, string password,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonUtility.ToJson(new AuthRequestBody { userId = userId, password = password });
        using var req = new UnityWebRequest(Root + "/api/auth/login", UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<string>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
        {
            var login = JsonUtility.FromJson<LoginResponseBody>(body);
            // reject response without JWT token
            if (login == null || string.IsNullOrEmpty(login.token))
                return ApiResult<string>.Failure(code, null, "Missing token in response", body);
            return ApiResult<string>.Success(login.token, code, body);
        }

        return FailureFromRequest<string>(req, body);
    }

    // login and save token locally
    public async UniTask<ApiResult<string>> LoginAndStoreTokenAsync(string userId, string password,
        CancellationToken cancellationToken = default)
    {
        var r = await LoginAsync(userId, password, cancellationToken);
        if (r.Ok)
        {
            SetAccessToken(r.Value);
            SetLocalUserId(userId);
        }

        return r;
    }

    // GET /api/users/me
    public async UniTask<ApiResult<UserResponse>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        using var req = UnityWebRequest.Get(Root + "/api/users/me");
        req.downloadHandler = new DownloadHandlerBuffer();
        SetBearerIfNeeded(req, true, AccessToken);
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<UserResponse>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
        {
            var user = MseApiJsonParser.ParseUserResponse(body);
            return ApiResult<UserResponse>.Success(user, code, body);
        }

        return FailureFromRequest<UserResponse>(req, body);
    }

    // PATCH /api/users/me
    public async UniTask<ApiResult<UserResponse>> PatchMeAsync(UserPatchRequest patch,
        CancellationToken cancellationToken = default)
    {
        if (patch == null)
            patch = new UserPatchRequest();
        var payload = patch.ToJsonBody();
        using var req = new UnityWebRequest(Root + "/api/users/me", "PATCH");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        SetBearerIfNeeded(req, true, AccessToken);
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<UserResponse>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
        {
            var user = MseApiJsonParser.ParseUserResponse(body);
            return ApiResult<UserResponse>.Success(user, code, body);
        }

        return FailureFromRequest<UserResponse>(req, body);
    }

    // update stage best score via GET then PATCH merge
    public async UniTask<ApiResult<UserResponse>> UpdateStageBestScoreAsync(int stageNumber, int score,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccessToken)
            return ApiResult<UserResponse>.Failure(0, "CLIENT", "Not logged in", string.Empty);
        if (stageNumber < 1)
            return ApiResult<UserResponse>.Failure(0, "CLIENT", "Invalid stage number", string.Empty);
        if (score < 0)
            return ApiResult<UserResponse>.Failure(0, "CLIENT", "Invalid score", string.Empty);

        var me = await GetMeAsync(cancellationToken);
        if (!me.Ok)
            return me;

        var progress = me.Value?.gameProgress ?? new Dictionary<string, int>();
        var key = stageNumber.ToString();
        // skip PATCH if new score is not higher
        if (progress.TryGetValue(key, out var existing) && score <= existing)
            return me;

        // merge and PATCH full progress map
        var merged = new Dictionary<string, int>(progress) { [key] = score };
        return await PatchMeAsync(new UserPatchRequest { GameProgress = merged }, cancellationToken);
    }

    // POST /api/rooms
    public async UniTask<ApiResult<RoomResponse>> CreateRoomAsync(string title, int stage, int maxPlayers,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonUtility.ToJson(new CreateRoomRequestBody
        {
            title = title,
            stage = stage,
            maxPlayers = maxPlayers
        });
        using var req = new UnityWebRequest(Root + "/api/rooms", UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        SetBearerIfNeeded(req, true, AccessToken);
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<RoomResponse>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
        {
            var room = JsonUtility.FromJson<RoomResponse>(body);
            return ApiResult<RoomResponse>.Success(room, code, body);
        }

        return FailureFromRequest<RoomResponse>(req, body);
    }

    // GET /api/rooms — OPEN rooms only
    public async UniTask<ApiResult<RoomResponse[]>> GetOpenRoomsAsync(CancellationToken cancellationToken = default)
    {
        using var req = UnityWebRequest.Get(Root + "/api/rooms");
        req.downloadHandler = new DownloadHandlerBuffer();
        SetBearerIfNeeded(req, true, AccessToken);
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<RoomResponse[]>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
        {
            var rooms = MseApiJsonParser.ParseRoomArray(body);
            return ApiResult<RoomResponse[]>.Success(rooms, code, body);
        }

        return FailureFromRequest<RoomResponse[]>(req, body);
    }

    // POST /api/rooms/{roomId}/join
    public async UniTask<ApiResult<RoomResponse>> JoinRoomAsync(string roomId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(roomId))
            return ApiResult<RoomResponse>.Failure(0, "CLIENT", "roomId is empty", string.Empty);

        var url = $"{Root}/api/rooms/{Uri.EscapeDataString(roomId)}/join";
        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        SetBearerIfNeeded(req, true, AccessToken);
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<RoomResponse>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
        {
            var room = JsonUtility.FromJson<RoomResponse>(body);
            return ApiResult<RoomResponse>.Success(room, code, body);
        }

        return FailureFromRequest<RoomResponse>(req, body);
    }

    // POST /api/rooms/{roomId}/start — host only
    public async UniTask<ApiResult<RoomResponse>> StartRoomAsync(string roomId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(roomId))
            return ApiResult<RoomResponse>.Failure(0, "CLIENT", "roomId is empty", string.Empty);

        var url = $"{Root}/api/rooms/{Uri.EscapeDataString(roomId)}/start";
        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        SetBearerIfNeeded(req, true, AccessToken);
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<RoomResponse>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
        {
            var room = JsonUtility.FromJson<RoomResponse>(body);
            return ApiResult<RoomResponse>.Success(room, code, body);
        }

        return FailureFromRequest<RoomResponse>(req, body);
    }

    // POST /api/rooms/{roomId}/leave
    public async UniTask<ApiResult<RoomResponse>> LeaveRoomAsync(string roomId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(roomId))
            return ApiResult<RoomResponse>.Failure(0, "CLIENT", "roomId is empty", string.Empty);

        var url = $"{Root}/api/rooms/{Uri.EscapeDataString(roomId)}/leave";
        using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        SetBearerIfNeeded(req, true, AccessToken);
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<RoomResponse>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 204)
            return ApiResult<RoomResponse>.Success(null, code, body);

        // guest leave returns updated room
        if (code == 200)
        {
            var room = JsonUtility.FromJson<RoomResponse>(body);
            return ApiResult<RoomResponse>.Success(room, code, body);
        }

        return FailureFromRequest<RoomResponse>(req, body);
    }

    // GET /actuator/health — no auth
    public async UniTask<ApiResult<string>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        using var req = UnityWebRequest.Get(Root + "/actuator/health");
        req.downloadHandler = new DownloadHandlerBuffer();
        await SendWebRequestAsync(req, cancellationToken);
        var body = req.downloadHandler?.text ?? string.Empty;
        var code = (int)req.responseCode;

        if (IsNetworkFailure(req))
            return ApiResult<string>.Failure(0, "NETWORK", req.error ?? "Network error", body);

        if (code == 200)
            return ApiResult<string>.Success(body, code, body);

        return FailureFromRequest<string>(req, body);
    }
}
