using MassTransit;
using Notifications.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailDispatchConsumer>();

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

        cfg.ReceiveEndpoint("email.dispatch", e =>
        {
            // wymaganie: 1 wiadomoœæ na raz
            e.PrefetchCount = 1;
            e.ConcurrentMessageLimit = 1;

            e.ConfigureConsumer<EmailDispatchConsumer>(context);
        });
    });
});

var host = builder.Build();
await host.RunAsync();

public sealed class EmailDispatchConsumer : IConsumer<DispatchNotification>
{
    public Task Consume(ConsumeContext<DispatchNotification> context)
    {
        // Na razie tylko log — w³aœciw¹ logikê przeniesiemy do SendingSimulator.
        Console.WriteLine($"[EMAIL] Got message: {context.Message.NotificationId} to {context.Message.Recipient}");
        return Task.CompletedTask;
    }
}
