namespace Notifications.Contracts;
public sealed record NotificationStatusResponse(
    Guid NotificationId,
    NotificationStatusDto Status,
    ChannelTypeDto Channel,
    string Recipient,
    string Title,
    DateTime ScheduledAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int Attempts,
    string? LastError,
    DateTime? SentAtUtc,
    DateTime? CancelledAtUtc
);
