// Owned by MinJun Lee
using UnityEngine;

/// <summary>
/// Maps API errors to user-friendly messages and shows ErrorPopup.
/// </summary>
public static class UserErrorPresenter
{
    public static string PendingMessage { get; private set; } // message to show on next scene

    public static void SetPending(string message) => PendingMessage = message;

    public static void ShowPending(ErrorPopup popup)
    {
        if (string.IsNullOrEmpty(PendingMessage))
            return;
        Show(popup, PendingMessage);
        PendingMessage = null;
    }

    public static void Show(ErrorPopup popup, string context, string message)
    {
        var text = Format(context, message);
        Show(popup, text);
    }

    public static void Show(ErrorPopup popup, string fullMessage)
    {
        if (popup != null)
            popup.Show(fullMessage);
        Debug.LogWarning($"[UserError] {fullMessage}");
    }

    public static void ShowApiFailure(ErrorPopup popup, string context, int statusCode, string errorCode,
        string serverMessage, string rawBody = null)
    {
        var userMessage = FormatApiFailure(context, statusCode, errorCode, serverMessage);
        Show(popup, userMessage);
        Debug.LogError(
            $"[{context}] HTTP={statusCode} code={errorCode} message={serverMessage} raw={rawBody ?? string.Empty}");
    }

    static string Format(string context, string message)
    {
        if (string.IsNullOrWhiteSpace(context))
            return message ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message))
            return context;
        return $"{context}: {message}";
    }

    static string FormatApiFailure(string context, int statusCode, string errorCode, string serverMessage)
    {
        // prefer mapped error code message
        var detail = MapErrorCode(errorCode, serverMessage);
        if (!string.IsNullOrEmpty(detail))
            return Format(context, detail);

        if (statusCode == 0 || errorCode == "NETWORK")
            return Format(context, "Cannot reach the server. Check your connection and try again.");

        // fallback to raw server message
        if (!string.IsNullOrWhiteSpace(serverMessage))
            return Format(context, serverMessage);

        return Format(context, "Something went wrong. Please try again.");
    }

    // map server error code to user message
    static string MapErrorCode(string errorCode, string serverMessage)
    {
        if (string.IsNullOrEmpty(errorCode))
            return null;

        switch (errorCode)
        {
            case "BAD_CREDENTIALS":
                return "Invalid user ID or password.";
            case "DUPLICATE_USER":
                return "This user ID is already taken.";
            case "NOT_FOUND":
                return "The requested item was not found. It may have been removed.";
            case "ROOM_FULL":
                return "This room is full.";
            case "ROOM_NOT_OPEN":
                return "This room is no longer open.";
            case "NOT_ROOM_PARTICIPANT":
                return "You are not a member of this room.";
            case "NOT_ROOM_HOST":
                return "Only the host can do that.";
            case "VALIDATION_ERROR":
                return MapValidationMessage(serverMessage);
            case "CLIENT":
                return string.IsNullOrWhiteSpace(serverMessage) ? null : serverMessage;
            case "NETWORK":
                return "Cannot reach the server. Check your connection and try again.";
            default:
                return null;
        }
    }

    // parse validation error text into friendly message
    static string MapValidationMessage(string serverMessage)
    {
        if (string.IsNullOrWhiteSpace(serverMessage))
            return "Please check your input and try again.";

        var msg = serverMessage.ToLowerInvariant();
        if (msg.Contains("userid") && msg.Contains("between 3 and 64"))
            return "User ID must be 3–64 characters.";
        if (msg.Contains("password") && msg.Contains("between 8 and 128"))
            return "Password must be 8–128 characters.";
        if (msg.Contains("must not be blank"))
            return "Please fill in all required fields.";
        if (msg.Contains("title"))
            return "Please enter a valid room title.";
        if (msg.Contains("maxplayers"))
            return "Max players must be between 2 and 4.";
        if (msg.Contains("stage"))
            return "Please select a valid stage.";

        return "Please check your input and try again.";
    }
}
