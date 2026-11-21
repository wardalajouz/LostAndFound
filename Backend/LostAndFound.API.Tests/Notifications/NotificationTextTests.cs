using LostAndFound.Api.Contracts.Notifications;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Notifications;

namespace LostAndFound.Api.Tests.Notifications;

public sealed class NotificationTextTests
{
    public static TheoryData<NotificationType, PreferredLanguage> EveryTypeAndLanguage
    {
        get
        {
            var data = new TheoryData<NotificationType, PreferredLanguage>();
            foreach (NotificationType type in Enum.GetValues<NotificationType>())
            {
                foreach (PreferredLanguage language in Enum.GetValues<PreferredLanguage>())
                {
                    data.Add(type, language);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EveryTypeAndLanguage))]
    public void Every_type_has_text_in_every_language(NotificationType type, PreferredLanguage language)
    {
        (string title, string message) = NotificationText.For(type, language, "Black wallet", "Ward");

        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.Contains("\"Black wallet\"", message);
    }

    [Theory]
    [MemberData(nameof(EveryTypeAndLanguage))]
    public void Missing_names_fall_back_to_generic_words(NotificationType type, PreferredLanguage language)
    {
        (string title, string message) = NotificationText.For(type, language, itemName: null, actorName: null);

        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.DoesNotContain("\"\"", message);
        Assert.DoesNotContain("  ", message);
    }

    [Fact]
    public void New_message_names_the_sender_and_the_item()
    {
        Assert.Equal(
            ("New message", "Ward sent you a message about \"Black wallet\"."),
            NotificationText.For(NotificationType.NewMessage, PreferredLanguage.English, "Black wallet", "Ward"));
        Assert.Equal(
            ("رسالة جديدة", "أرسل لك وارد رسالة بخصوص \"جزدان أسود\"."),
            NotificationText.For(NotificationType.NewMessage, PreferredLanguage.Arabic, "جزدان أسود", "وارد"));
    }

    [Fact]
    public void Resolved_text_starts_with_a_capital_when_the_item_is_unknown()
    {
        (_, string message) = NotificationText.For(NotificationType.PostResolved, PreferredLanguage.English, null, null);

        Assert.StartsWith("A post", message);
    }

    [Fact]
    public void Unknown_type_is_a_programming_error()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NotificationText.For((NotificationType)99, PreferredLanguage.English, "x", "y"));
    }
}
