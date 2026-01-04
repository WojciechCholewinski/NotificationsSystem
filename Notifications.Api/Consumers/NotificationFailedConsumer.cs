using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Contracts;
using Notifications.Domain;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Api.Consumers;

public sealed class NotificationFailedConsumer : IConsumer<NotificationFailed>
{
    // opóźnienie ponowienia (możesz zmienić)
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    private readonly NotificationsDbContext _db;

    public NotificationFailedConsumer(NotificationsDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<NotificationFailed> context)
    {
        var msg = context.Message;

        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == msg.NotificationId, context.CancellationToken);

        if (n is null)
            return;

        // jak już terminalny — ignorujemy
        if (n.Status is NotificationStatus.Sent or NotificationStatus.Failed or NotificationStatus.Canceled)
            return;

        // 1) zwiększ attempts / ustaw błąd / ewentualnie przejdź do Failed
        n.RegisterAttemptFailure(msg.Error);

        // 2) jeśli NIE zrobiło się terminalne — to planujemy kolejną próbę
        if (!n.IsTerminal())
        {
            n.Reschedule(DateTime.UtcNow.Add(RetryDelay));
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}