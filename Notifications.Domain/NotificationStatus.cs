namespace Notifications.Domain;

public enum NotificationStatus
{
    Created = 0,    // utworzone
    Scheduled = 1,    // oczekujące / zaplanowane do wysłania
    Sending = 2,    // w trakcie
    Sent = 3,       // wysłane
    Failed = 4,     // nieudane (po 3 próbach)
    Canceled = 5    // anulowane
}
