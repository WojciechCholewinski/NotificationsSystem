using MassTransit;
using Notifications.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PushDispatchConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var user = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var pass = builder.Configuration["RabbitMq:Password"] ?? "guest";

        cfg.Host(host, h =>
        {
            h.Username(user);
            h.Password(pass);
        });

        cfg.ReceiveEndpoint("push.dispatch", e =>
        {
            // wymaganie: 1 wiadomoœæ na raz
            e.PrefetchCount = 1;
            e.ConcurrentMessageLimit = 1;

            e.ConfigureConsumer<PushDispatchConsumer>(context);
        });
    });
});

var host = builder.Build();
await host.RunAsync();

public sealed class PushDispatchConsumer : IConsumer<DispatchNotification>
{
    public Task Consume(ConsumeContext<DispatchNotification> context)
    {
        // Na razie tylko log — w³aœciw¹ logikê przeniesiemy do SendingSimulator.
        Console.WriteLine($"[PUSH] Got message: {context.Message.NotificationId} to {context.Message.Recipient}");
        return Task.CompletedTask;
    }
}
