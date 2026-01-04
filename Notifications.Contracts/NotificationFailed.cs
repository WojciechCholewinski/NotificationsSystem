namespace Notifications.Contracts;
public sealed record NotificationFailed(
    Guid NotificationId,
    ChannelTypeDto Channel,
    string Error,
    DateTime FailedAtUtc);
