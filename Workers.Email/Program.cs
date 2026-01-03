using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Notifications.SendingSimulator;
using Prometheus;
using Workers.Email.Consumers;

var builder = WebApplication.CreateBuilder(args);

// Wymuszamy nas³uch na 0.0.0.0:8081 (wa¿ne w Dockerze)
builder.WebHost.UseUrls("http://0.0.0.0:8081");

builder.Services.AddSingleton<INotificationSendingSimulator, InMemoryNotificationSendingSimulator>();

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailDispatchConsumer>();

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

        cfg.ReceiveEndpoint("email.dispatch", e =>
        {
            e.PrefetchCount = 1;
            e.ConcurrentMessageLimit = 1;
            e.UseMessageRetry(r => r.Immediate(2));

            e.ConfigureConsumer<EmailDispatchConsumer>(context);
        });
    });
});

var app = builder.Build();

// Endpoint metryk
app.MapMetrics("/metrics");

await app.RunAsync();
