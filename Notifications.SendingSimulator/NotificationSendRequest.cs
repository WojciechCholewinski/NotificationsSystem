using Notifications.Contracts;

namespace Notifications.SendingSimulator
{
    public sealed record NotificationSendRequest(
        Guid NotificationId,
        ChannelType Channel,
        string Recipient,
        string Title,
        string Body);
}
