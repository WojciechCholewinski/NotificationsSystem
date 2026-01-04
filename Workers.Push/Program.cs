using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Notifications.SendingSimulator;
using Prometheus;
using Workers.Push.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Wymuszamy nas³uch na 0.0.0.0:8082 (wa¿ne w Dockerze)
builder.WebHost.UseUrls("http://0.0.0.0:8082");

builder.Services.AddSingleton<INotificationSendingSimulator, InMemoryNotificationSendingSimulator>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PushDispatchConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "rabbitmq";
        var user = builder.Configuration["RabbitMq:Username"] ?? "guest";
        var pass = builder.Configuration["RabbitMq:Password"] ?? "guest";

        cfg.Host(host, h =>
        {
            h.Username(user);
            h.Password(pass);
        });

        cfg.ReceiveEndpoint("push.dispatch", e =>
        {
            // 1 wiadomoœæ na raz
            e.PrefetchCount = 1;
            e.ConcurrentMessageLimit = 1;

            e.ConfigureConsumer<PushDispatchConsumer>(context);
        });
    });
});

var app = builder.Build();

app.MapMetrics("/metrics");

await app.RunAsync();
