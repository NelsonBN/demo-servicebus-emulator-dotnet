using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus.Administration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSingleton(sp
        => new ServiceBusAdministrationClient(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("ServiceBusAdmin")))
    .AddSingleton(sp
        => new ServiceBusClient(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("ServiceBus"),
            new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp
            }))
    .AddSingleton(sp
        => sp.GetRequiredService<ServiceBusClient>().CreateSender(
            sp.GetRequiredService<IConfiguration>()["QueueName"]));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();


class Worker(
    ILogger<Worker> logger,
    ServiceBusSender sender,
    ServiceBusAdministrationClient admin,
        IConfiguration configuration) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly ServiceBusAdministrationClient _admin = admin;
    private readonly ServiceBusSender _sender = sender;
    private readonly string _queueName = configuration["QueueName"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting producing messages");

        if (await _admin.QueueExistsAsync(_queueName))
        {
            _logger.LogInformation("Queue {QueueName} already exists", _queueName);
        }
        else
        {
            _logger.LogInformation("Queue {QueueName} does not exist. Creating...", _queueName);
            await _admin.CreateQueueAsync(_queueName);
            _logger.LogInformation("Created queue {QueueName}", _queueName);
        }

        while(!stoppingToken.IsCancellationRequested)
        {
            var message = $"My new message {DateTimeOffset.UtcNow}";

            await _sender.SendMessageAsync(new ServiceBusMessage(message), stoppingToken);

            _logger.LogInformation("Sent message: {Message}", message);

            await Task.Delay(1000, stoppingToken);
        }
    }
}
