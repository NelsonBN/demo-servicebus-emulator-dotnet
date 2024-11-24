using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.Producer;

public class ProducerWorker(
    ILogger<ProducerWorker> logger,
    IConfiguration configuration,
    ServiceBusClient client) : BackgroundService
{
    private readonly ILogger<ProducerWorker> _logger = logger;
    private readonly ServiceBusSender _sender = client
        .CreateSender(configuration["QueueName"]);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            var message = $"My new message {DateTimeOffset.UtcNow}";

            await _sender.SendMessageAsync(new ServiceBusMessage(message), stoppingToken);

            _logger.LogInformation($"Sent message: {message}");

            await Task.Delay(1000, stoppingToken);
        }
    }
}
