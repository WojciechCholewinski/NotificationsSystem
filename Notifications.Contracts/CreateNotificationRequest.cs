namespace Notifications.Contracts;
public sealed record CreateNotificationRequest(
    ChannelTypeDto Channel,
    string Recipient,
    string RecipientTimeZone,
    string Title,
    string Body,
    DateTime ScheduledAtLocal   // czas w strefie użytkownika (nie UTC)
);