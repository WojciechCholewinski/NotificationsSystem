namespace Notifications.Domain;

public sealed class Notification
{
    public const int MaxAttempts = 3;

    public Guid Id { get; private set; } = Guid.NewGuid();

    // wymagane w definicji "Powiadomienie" (kanał, odbiorca, strefa czasowa, treść, data/godzina) + opcjonalny priorytet
    public ChannelType Channel { get; private set; }
    public string Recipient { get; private set; } = default!;
    public string RecipientTimeZone { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime ScheduledAtUtc { get; private set; }
    public int? Priority { get; private set; }

    // statusy i audyt
    public NotificationStatus Status { get; private set; }

    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public DateTime? SentAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    private Notification() { } // EF

    private Notification(
        ChannelType channel,
        string recipient,
        string recipientTimeZone,
        string title,
        string body,
        int? priority)
    {
        Channel = channel;
        Recipient = recipient;
        RecipientTimeZone = recipientTimeZone;
        Title = title;
        Body = body;
        Priority = priority;

        Status = NotificationStatus.Created;
        Attempts = 0;
    }

    /// <summary>
    /// Tworzy powiadomienie i od razu ustawia je jako zaplanowane na wskazany czas (UTC).
    /// </summary>
    public static Notification CreateScheduled(
        ChannelType channel,
        string recipient,
        string recipientTimeZone,
        string title,
        string body,
        DateTime scheduledAtUtc,
        int? priority = null)
    {
        ValidateBasics(recipient, recipientTimeZone, title, body);
        ValidateScheduledAt(scheduledAtUtc);

        var n = new Notification(channel, recipient, recipientTimeZone, title, body, priority);
        n.Schedule(scheduledAtUtc);
        return n;
    }

    /// <summary>
    /// Przejście: Created -> Scheduled
    /// </summary>
    public void Schedule(DateTime scheduledAtUtc)
    {
        EnsureState(NotificationStatus.Created);

        ValidateScheduledAt(scheduledAtUtc);

        ScheduledAtUtc = scheduledAtUtc;
        Status = NotificationStatus.Scheduled;
        Touch();
    }

    /// <summary>
    /// Przejście: Scheduled -> Sending
    /// (wywoływane przez Dispatcher, gdy zadanie jest "gotowe do wysyłki")
    /// </summary>
    public void MarkSending()
    {
        EnsureState(NotificationStatus.Scheduled);

        Status = NotificationStatus.Sending;
        Touch();
    }

    /// <summary>
    /// Przejście: Sending -> Sent
    /// </summary>
    public void MarkSent()
    {
        EnsureState(NotificationStatus.Sending);

        Status = NotificationStatus.Sent;
        SentAtUtc = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Obsługa błędu próby wysyłki:
    /// - zwiększa Attempts
    /// - zapisuje LastError
    /// - jeśli osiągnięto MaxAttempts, przechodzi terminalnie do Failed
    /// - w przeciwnym razie wraca do Scheduled (będzie ponowione)
    /// </summary>
    public void RegisterAttemptFailure(string error)
    {
        if (Status is not (NotificationStatus.Sending or NotificationStatus.Scheduled))
            throw new InvalidOperationException($"Cannot fail attempt from state {Status}.");

        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Unknown error" : error;

        if (Attempts >= MaxAttempts)
        {
            Status = NotificationStatus.Failed;
            Touch();
            return;
        }

        // wracamy do kolejki ponowień (samo planowanie retry / opóźnień to rola warstwy aplikacyjnej)
        Status = NotificationStatus.Scheduled;
        Touch();
    }

    /// <summary>
    /// Anulowanie niewysłanego powiadomienia przez API.
    /// Dozwolone tylko, gdy jeszcze nie jest terminalne (Sent/Failed/Cancelled).
    /// </summary>
    public void Cancel()
    {
        if (Status is NotificationStatus.Sent or NotificationStatus.Failed or NotificationStatus.Canceled)
            throw new InvalidOperationException($"Cannot cancel notification in state {Status}.");

        Status = NotificationStatus.Canceled;
        CancelledAtUtc = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Wymuszenie natychmiastowej wysyłki (Admin).
    /// Dozwolone tylko dla niewysłanego (Scheduled).
    /// Ustawiamy czas na "teraz" i zostawiamy w Scheduled, żeby Dispatcher je podjął od razu.
    /// </summary>
    public void ForceSendNow(DateTime utcNow)
    {
        EnsureState(NotificationStatus.Scheduled);

        ScheduledAtUtc = utcNow;
        Touch();
    }

    public void Reschedule(DateTime newScheduledAtUtc)
    {
        ScheduledAtUtc = DateTime.SpecifyKind(newScheduledAtUtc, DateTimeKind.Utc);
        Status = NotificationStatus.Scheduled;
        Touch();
    }

    public bool IsTerminal()
        => Status is NotificationStatus.Sent or NotificationStatus.Failed or NotificationStatus.Canceled;

    private static void ValidateBasics(string recipient, string recipientTimeZone, string title, string body)
    {
        if (string.IsNullOrWhiteSpace(recipient)) throw new ArgumentException("Recipient is required.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(recipientTimeZone)) throw new ArgumentException("RecipientTimeZone is required.", nameof(recipientTimeZone));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));
    }

    private static void ValidateScheduledAt(DateTime scheduledAtUtc)
    {
        // wymaganie: zaplanować na konkretną datę i godzinę w przyszłości
        // (dopuszczamy lekkie "teraz" gdy admin wymusza lub zegary się minimalnie rozjadą)
        if (scheduledAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("ScheduledAtUtc must be in UTC (DateTimeKind.Utc).", nameof(scheduledAtUtc));

        if (scheduledAtUtc < DateTime.UtcNow.AddMinutes(-1))
            throw new ArgumentException("ScheduledAtUtc must be in the future (UTC).", nameof(scheduledAtUtc));
    }

    private void EnsureState(NotificationStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Invalid state transition. Expected {expected}, got {Status}.");
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
