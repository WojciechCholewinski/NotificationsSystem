namespace Notifications.Contracts;
public enum NotificationStatusDto
{
    Created = 0,
    Scheduled = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4,
    Cancelled = 5
}
