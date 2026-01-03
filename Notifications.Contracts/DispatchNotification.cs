namespace Notifications.Contracts
{
    public enum ChannelType
    {
        Email = 1,
        Push = 2
    }

    public sealed record DispatchNotification(
        Guid NotificationId,
        ChannelType Channel,
        string Recipient,
        string Title,
        string Body,
        DateTime CreatedAtUtc
    );
}
