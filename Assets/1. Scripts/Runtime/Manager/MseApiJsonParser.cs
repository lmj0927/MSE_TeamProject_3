using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Spring API JSON 중 <see cref="UnityEngine.JsonUtility"/>로 처리하기 어려운 부분 보조.
/// </summary>
static class MseApiJsonParser
{
    static readonly Regex RegexGameProgressBlock = new Regex(
        "\"gameProgress\"\\s*:\\s*\\{([^}]*)\\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    static readonly Regex RegexMapEntry = new Regex(
        "\"([^\"]+)\"\\s*:\\s*(-?\\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// <c>GET /api/users/me</c> 응답 JSON을 <see cref="UserResponse"/>로 파싱합니다.
    /// </summary>
    public static UserResponse ParseUserResponse(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new UserResponse();

        var u = new UserResponse();
        u.userId = ReadJsonString(json, "userId");
        u.currency = ReadJsonInt(json, "currency", 0);
        u.gameProgress = ParseGameProgressMap(json);
        u.ownedItems = ReadJsonIntArray(json, "ownedItems");
        return u;
    }

    public static Dictionary<string, int> ParseGameProgressMap(string json)
    {
        var dict = new Dictionary<string, int>();
        var block = RegexGameProgressBlock.Match(json);
        if (!block.Success)
            return dict;
        var inner = block.Groups[1].Value;
        foreach (Match m in RegexMapEntry.Matches(inner))
        {
            var key = m.Groups[1].Value;
            if (int.TryParse(m.Groups[2].Value, out var val))
                dict[key] = val;
        }
        return dict;
    }

    public static string ReadJsonString(string json, string field)
    {
        var m = Regex.Match(json,
            $"\"{Regex.Escape(field)}\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.CultureInvariant);
        if (!m.Success)
            return null;
        return Regex.Unescape(m.Groups[1].Value);
    }

    public static int ReadJsonInt(string json, string field, int defaultValue)
    {
        var m = Regex.Match(json,
            $"\"{Regex.Escape(field)}\"\\s*:\\s*(-?\\d+)",
            RegexOptions.CultureInvariant);
        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var v))
            return defaultValue;
        return v;
    }

    public static int[] ReadJsonIntArray(string json, string field)
    {
        var m = Regex.Match(json,
            $"\"{Regex.Escape(field)}\"\\s*:\\s*\\[([^\\]]*)\\]",
            RegexOptions.CultureInvariant);
        if (!m.Success)
            return Array.Empty<int>();
        var inner = m.Groups[1].Value.Trim();
        if (inner.Length == 0)
            return Array.Empty<int>();
        var parts = inner.Split(',');
        var list = new List<int>(parts.Length);
        foreach (var p in parts)
        {
            var t = p.Trim();
            if (int.TryParse(t, out var n))
                list.Add(n);
        }
        return list.ToArray();
    }

    /// <summary>
    /// 루트가 배열인 JSON을 <see cref="JsonUtility"/>로 역직렬화하기 위한 래퍼.
    /// </summary>
    [Serializable]
    class RoomListWrapper
    {
        public RoomResponse[] items;
    }

    public static RoomResponse[] ParseRoomArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<RoomResponse>();
        var wrapped = "{\"items\":" + json + "}";
        var w = JsonUtility.FromJson<RoomListWrapper>(wrapped);
        return w?.items ?? Array.Empty<RoomResponse>();
    }
}
