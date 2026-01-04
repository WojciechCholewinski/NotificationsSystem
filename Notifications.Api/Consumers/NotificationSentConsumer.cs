using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Contracts;
using Notifications.Domain;
using Notifications.Infrastructure.Persistence;

public sealed class NotificationSentConsumer : IConsumer<NotificationSent>
{
    private readonly NotificationsDbContext _db;

    public NotificationSentConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<NotificationSent> context)
    {
        var msg = context.Message;

        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == msg.NotificationId, context.CancellationToken);

        if (n is null)
            return;

        // idempotencja eventów:
        if (n.Status is NotificationStatus.Sent or NotificationStatus.Failed or NotificationStatus.Canceled)
            return;

        // oczekujemy Sending, ale jakby był Scheduled (np. edge-case) to i tak nie psujemy:
        try
        {
            // jeżeli masz EnsureState(Sending) w MarkSent(), to upewnij się,
            // że Dispatcher ustawia MarkSending() przed wysyłką (u Ciebie ustawia).
            n.MarkSent();
        }
        catch (InvalidOperationException)
        {
            // jeżeli stan nie pasuje, nie wywracamy systemu — event mógł przyjść “za wcześnie”
            // (w labie najczęściej nie wystąpi)
            return;
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
