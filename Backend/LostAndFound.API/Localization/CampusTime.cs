namespace LostAndFound.Api.Localization;

/// <summary>Yarmouk University's local time. Dates users see and pick (today, months) follow it, not UTC.</summary>
public static class CampusTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Amman");

    public static DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);

    public static DateOnly Today(DateTimeOffset utcNow) => DateOnly.FromDateTime(ToLocal(utcNow).DateTime);

    /// <summary>The instant the given local calendar month starts on campus.</summary>
    public static DateTimeOffset StartOfMonth(int year, int month)
    {
        var localMidnight = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(localMidnight, Zone.GetUtcOffset(localMidnight));
    }
}
