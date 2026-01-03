using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Notifications.SendingSimulator;

public sealed class InMemoryNotificationSendingSimulator : INotificationSendingSimulator
{
    private readonly ILogger<InMemoryNotificationSendingSimulator> _logger;

    // Idempotencja: zapisujemy tylko te, które zakończyły się SUKCESEM
    private readonly ConcurrentDictionary<Guid, byte> _sentIds = new();

    private readonly double _failureRate;

    public InMemoryNotificationSendingSimulator(
        ILogger<InMemoryNotificationSendingSimulator> logger,
        double failureRate = 0.30)
    {
        _logger = logger;
        _failureRate = failureRate;
    }

    public Task<SendOutcome> SendAsync(NotificationSendRequest request, CancellationToken ct)
    {
        if (_sentIds.ContainsKey(request.NotificationId))
        {
            _logger.LogInformation("[SIMULATOR] Duplicate ignored. NotificationId={NotificationId}", request.NotificationId);
            return Task.FromResult(SendOutcome.AlreadySent);
        }

        // ~30% niepowodzeń
        var failed = Random.Shared.NextDouble() < _failureRate;

        if (failed)
        {
            _logger.LogWarning("[SIMULATOR] FAILED ({Channel}) NotificationId={NotificationId} Recipient={Recipient}",
                request.Channel, request.NotificationId, request.Recipient);

            // Rzucamy wyjątek -> MassTransit uruchomi retry
            throw new SimulatedSendFailedException($"Simulated failure for {request.Channel}, id={request.NotificationId}");
        }

        // SUKCES: zapisujemy ID (idempotencja)
        _sentIds.TryAdd(request.NotificationId, 0);

        _logger.LogInformation("[SIMULATOR] SENT ({Channel}) NotificationId={NotificationId} Recipient={Recipient}",
            request.Channel, request.NotificationId, request.Recipient);

        return Task.FromResult(SendOutcome.Sent);
    }
}
