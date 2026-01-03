using MassTransit;
using Notifications.Contracts;
using Notifications.SendingSimulator;
using Prometheus;

namespace Workers.Push.Consumers
{
    public sealed class PushDispatchConsumer : IConsumer<DispatchNotification>
    {
        private readonly INotificationSendingSimulator _simulator;

        private static readonly Counter SentCounter =
            Metrics.CreateCounter("worker_push_sent_total", "Number of sent push notifications.");

        public PushDispatchConsumer(INotificationSendingSimulator simulator)
            => _simulator = simulator;

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

            // Liczymy tylko realny “SENT”, nie duplikaty
            if (outcome == SendOutcome.Sent)
                SentCounter.Inc();
        }
    }
}
