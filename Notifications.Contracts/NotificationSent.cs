namespace Notifications.Contracts;
public sealed record NotificationSent(
    Guid NotificationId,
    ChannelTypeDto Channel,
    DateTime SentAtUtc);
