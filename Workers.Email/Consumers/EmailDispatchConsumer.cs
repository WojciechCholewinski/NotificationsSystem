using MassTransit;
using Notifications.Contracts;
using Notifications.SendingSimulator;
using Prometheus;

namespace Workers.Email.Consumers;

public sealed class EmailDispatchConsumer : IConsumer<DispatchNotification>
{
    private readonly INotificationSendingSimulator _simulator;
    private readonly IPublishEndpoint _publish;

    private static readonly Counter SentCounter =
        Metrics.CreateCounter("worker_email_sent_total", "Number of sent email notifications.");

    private static readonly Counter FailedCounter =
        Metrics.CreateCounter("worker_email_failed_total", "Number of failed email notifications.");

    private static readonly Counter DuplicateCounter =
        Metrics.CreateCounter("worker_email_duplicate_total", "Number of duplicate email notifications.");

    public EmailDispatchConsumer(INotificationSendingSimulator simulator, IPublishEndpoint publish)
    {
        _simulator = simulator;
        _publish = publish;
    }

    public async Task Consume(ConsumeContext<DispatchNotification> context)
    {
        var msg = context.Message;

        var outcome = await _simulator.SendAsync(
            new NotificationSendRequest(
                msg.NotificationId,
                msg.Channel,
                msg.Recipient,
                msg.Title,
                msg.Body),
            context.CancellationToken);

        switch (outcome)
        {
            case SendOutcome.Sent:
                SentCounter.Inc();
                await _publish.Publish(
                    new NotificationSent(msg.NotificationId, msg.Channel, DateTime.UtcNow),
                    context.CancellationToken);
                return;

            case SendOutcome.Duplicate:
                // Duplicate traktujemy jak sukces (idempotencja)
                DuplicateCounter.Inc();
                await _publish.Publish(
                    new NotificationSent(msg.NotificationId, msg.Channel, DateTime.UtcNow),
                    context.CancellationToken);
                return;

            case SendOutcome.Failed:
            default:
                FailedCounter.Inc();
                await _publish.Publish(
                    new NotificationFailed(msg.NotificationId, msg.Channel, "Simulator failure", DateTime.UtcNow),
                    context.CancellationToken);
                return;
        }
    }
}