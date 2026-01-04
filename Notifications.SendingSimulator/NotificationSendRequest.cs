using Notifications.Contracts;

namespace Notifications.SendingSimulator
{
    public sealed record NotificationSendRequest(
        Guid NotificationId,
        ChannelTypeDto Channel,
        string Recipient,
        string Title,
        string Body);
}
