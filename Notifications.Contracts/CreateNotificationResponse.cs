namespace Notifications.Contracts;
public sealed record CreateNotificationResponse(
    Guid NotificationId,
    DateTime ScheduledAtUtc
);
