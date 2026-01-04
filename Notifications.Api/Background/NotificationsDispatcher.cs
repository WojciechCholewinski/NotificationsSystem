using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Scheduling;
using Notifications.Contracts;
using Notifications.Domain;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Api.Background;

public sealed class NotificationsDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationsDispatcher> _logger;

    public NotificationsDispatcher(IServiceScopeFactory scopeFactory, ILogger<NotificationsDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // prosty polling – narazie wystarczy (potem sobie dodam Quartz/Hangfire)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Tick(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dispatcher tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task Tick(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        var planner = scope.ServiceProvider.GetRequiredService<IQuietHoursPlanner>();

        var now = DateTime.UtcNow;

        // bierzemy porcję, żeby nie zabić bazy (możesz zmienić limit)
        var batch = await db.Notifications
            .Where(n => n.Status == NotificationStatus.Scheduled && n.ScheduledAtUtc <= now)
            .OrderBy(n => n.Priority ?? int.MaxValue)
            .ThenBy(n => n.ScheduledAtUtc)
            .Take(50)
            .ToListAsync(ct);

        if (batch.Count == 0)
            return;

        foreach (var n in batch)
        {
            // cisza nocna – przesuń czas i leć dalej
            if (!planner.IsAllowedNow(n, now))
            {
                var nextUtc = planner.GetNextAllowedUtc(n, now);
                // ustawiamy nowy termin wysyłki
                n.Reschedule(nextUtc); // tu używamy metody zmieniającej ScheduledAtUtc
                continue;
            }

            // oznacz jako Sending i wyślij na broker
            n.MarkSending();

            var msg = new DispatchNotification(
                n.Id,
                n.Channel switch
                {
                    ChannelType.Email => ChannelTypeDto.Email,
                    ChannelType.Push => ChannelTypeDto.Push,
                    _ => throw new ArgumentOutOfRangeException()
                },
                n.Recipient,
                n.Title,
                n.Body,
                n.CreatedAtUtc);

            var queue = n.Channel == ChannelType.Email ? "queue:email.dispatch" : "queue:push.dispatch";
            var endpoint = await bus.GetSendEndpoint(new Uri(queue));
            await endpoint.Send(msg, ct);

            _logger.LogInformation("Dispatched {Id} to {Queue}", n.Id, queue);
        }

        await db.SaveChangesAsync(ct);
    }
}
