namespace Notifications.SendingSimulator
{
    public interface INotificationSendingSimulator
    {
        Task<SendOutcome> SendAsync(NotificationSendRequest request, CancellationToken ct);
    }
}
