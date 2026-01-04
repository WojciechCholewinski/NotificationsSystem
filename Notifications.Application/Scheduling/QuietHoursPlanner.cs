using Notifications.Domain;

namespace Notifications.Application.Scheduling;

public interface IQuietHoursPlanner
{
    DateTime GetNextAllowedUtc(Notification n, DateTime utcNow);
    bool IsAllowedNow(Notification n, DateTime utcNow);
}

public sealed class QuietHoursPlanner : IQuietHoursPlanner
{
    // Cisza: 22:00-07:00 w strefie odbiorcy
    private static readonly TimeSpan QuietStart = new(22, 0, 0);
    private static readonly TimeSpan QuietEnd = new(7, 0, 0);

    public bool IsAllowedNow(Notification n, DateTime utcNow)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(n.RecipientTimeZone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var t = local.TimeOfDay;

        // cisza "przez północ": [22:00, 24:00) U [00:00, 07:00)
        var inQuiet = t >= QuietStart || t < QuietEnd;
        return !inQuiet;
    }

    public DateTime GetNextAllowedUtc(Notification n, DateTime utcNow)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(n.RecipientTimeZone);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        if (IsAllowedNow(n, utcNow))
            return utcNow;

        // jeżeli jesteśmy w ciszy:
        // - jeśli jest po 22:00 → następny dzień 07:00
        // - jeśli jest przed 07:00 → dziś 07:00
        DateTime nextLocal;

        if (localNow.TimeOfDay >= QuietStart)
            nextLocal = localNow.Date.AddDays(1).Add(QuietEnd);
        else
            nextLocal = localNow.Date.Add(QuietEnd);

        // konwersja do UTC
        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified),
            tz);

        return DateTime.SpecifyKind(nextUtc, DateTimeKind.Utc);
    }
}
