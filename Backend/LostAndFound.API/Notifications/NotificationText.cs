using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Contracts.Profile;

namespace LostAndFound.Api.Notifications;

/// <summary>
/// Builds the title and text a user sees. Nothing is stored, so old notifications
/// follow the language toggle and always show the post's current name.
/// </summary>
public static class NotificationText
{
    /// <param name="itemName">The post's item name, or null if the notification has no post.</param>
    /// <param name="actorName">Who caused it, for example the sender of a message, or null if unknown.</param>
    public static (string Title, string Message) For(
        NotificationType type, PreferredLanguage language, string? itemName, string? actorName)
    {
        bool arabic = language == PreferredLanguage.Arabic;
        string item = itemName is null ? (arabic ? "منشور" : "a post") : $"\"{itemName}\"";
        string actor = actorName ?? (arabic ? "شخص ما" : "Someone");

        return (type, arabic) switch
        {
            (NotificationType.NewMessage, false) => ("New message", $"{actor} sent you a message about {item}."),
            (NotificationType.NewMessage, true) => ("رسالة جديدة", $"أرسل لك {actor} رسالة بخصوص {item}."),

            (NotificationType.PostResolved, false) => ("Item resolved", $"{Capitalize(item)}, which you messaged about, has been marked as resolved."),
            (NotificationType.PostResolved, true) => ("تم حل البلاغ", $"تم وضع علامة \"تم الحل\" على {item} الذي راسلت بخصوصه."),

            (NotificationType.SimilarItemFound, false) => ("Possible match", $"A post similar to {item} was published."),
            (NotificationType.SimilarItemFound, true) => ("تطابق محتمل", $"تم نشر بلاغ مشابه لـ {item}."),

            (NotificationType.PostModerated, false) => ("Update on your post", $"An administrator changed the visibility of {item}."),
            (NotificationType.PostModerated, true) => ("تحديث على منشورك", $"قام أحد المشرفين بتغيير ظهور {item}."),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "No text defined for this notification type.")
        };
    }

    private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
