namespace Notifications.Contracts
{
    public sealed record DispatchNotification(
        Guid NotificationId,
        ChannelTypeDto Channel,
        string Recipient,
        string Title,
        string Body,
        DateTime CreatedAtUtc
    );
}
