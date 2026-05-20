using System;
using System.Collections.Generic;

/// <summary>
/// MSE Server REST API 요청/응답 모델 (<c>docs/API.md</c> 기준).
/// </summary>
[Serializable]
public class AuthRequestBody
{
    public string userId;
    public string password;
}

[Serializable]
public class LoginResponseBody
{
    public string token;
}

[Serializable]
public class ApiErrorBody
{
    public string code;
    public string message;
}

[Serializable]
public class CreateRoomRequestBody
{
    public string title;
    public int maxPlayers;
}

[Serializable]
public class RoomResponse
{
    public string roomId;
    public string hostUserId;
    public string title;
    public int maxPlayers;
    public int currentPlayerCount;
    public string status;
    public string createdAt;
    public string[] participantUserIds;
}

/// <summary>
/// <c>GET/PATCH /api/users/me</c> 응답. <c>gameProgress</c> 키는 문자열(스테이지 번호)입니다.
/// </summary>
public class UserResponse
{
    public string userId;
    public int currency;
    public Dictionary<string, int> gameProgress = new Dictionary<string, int>();
    public int[] ownedItems = Array.Empty<int>();
}

/// <summary>
/// <c>PATCH /api/users/me</c> 본문. 설정한 필드만 JSON에 포함됩니다.
/// </summary>
public class UserPatchRequest
{
    public int? Currency;
    public Dictionary<string, int> GameProgress;
    public int[] OwnedItems;

    public string ToJsonBody()
    {
        var parts = new System.Collections.Generic.List<string>(3);
        if (Currency.HasValue)
            parts.Add($"\"currency\":{Currency.Value}");
        if (GameProgress != null && GameProgress.Count > 0)
        {
            var mapParts = new System.Collections.Generic.List<string>(GameProgress.Count);
            foreach (var kv in GameProgress)
                mapParts.Add($"\"{EscapeJson(kv.Key)}\":{kv.Value}");
            parts.Add($"\"gameProgress\":{{{string.Join(",", mapParts)}}}");
        }
        if (OwnedItems != null)
        {
            var nums = new System.Collections.Generic.List<string>(OwnedItems.Length);
            foreach (var id in OwnedItems)
                nums.Add(id.ToString());
            parts.Add($"\"ownedItems\":[{string.Join(",", nums)}]");
        }
        if (parts.Count == 0)
            return "{}";
        return "{" + string.Join(",", parts) + "}";
    }

    static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

/// <summary>
/// API 호출 결과. <see cref="Ok"/>가 false이면 <see cref="ErrorCode"/> / <see cref="ErrorMessage"/>를 확인합니다.
/// </summary>
public readonly struct ApiResult<T>
{
    public bool Ok { get; }
    public T Value { get; }
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public string ErrorMessage { get; }
    public string RawBody { get; }

    ApiResult(bool ok, T value, int statusCode, string errorCode, string errorMessage, string rawBody)
    {
        Ok = ok;
        Value = value;
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RawBody = rawBody;
    }

    public static ApiResult<T> Success(T value, int statusCode, string rawBody) =>
        new ApiResult<T>(true, value, statusCode, null, null, rawBody);

    public static ApiResult<T> Failure(int statusCode, string errorCode, string errorMessage, string rawBody) =>
        new ApiResult<T>(false, default, statusCode, errorCode, errorMessage, rawBody);
}

public static class ApiResult
{
    public static ApiResult<bool> SuccessNoContent(int statusCode) =>
        ApiResult<bool>.Success(true, statusCode, string.Empty);

    public static ApiResult<bool> Failure(int statusCode, string errorCode, string errorMessage, string rawBody) =>
        ApiResult<bool>.Failure(statusCode, errorCode, errorMessage, rawBody);
}
