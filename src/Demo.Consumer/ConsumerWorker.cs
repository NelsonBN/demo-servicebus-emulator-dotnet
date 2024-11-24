using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.Consumer;

public class ConsumerWorker : IHostedService
{
    private readonly ILogger<ConsumerWorker> _logger;
    private readonly string _queueName;
    private readonly ServiceBusProcessor _processor;

    public ConsumerWorker(
        ILogger<ConsumerWorker> logger,
        IConfiguration configuration,
        ServiceBusClient client)
    {
        _logger = logger;
        _queueName = configuration["QueueName"] ?? throw new ArgumentNullException(nameof(configuration));

        _processor = client.CreateProcessor(
            _queueName,
            new ServiceBusProcessorOptions());

        _processor.ProcessMessageAsync += _processMessageAsync;
        _processor.ProcessErrorAsync += _processErrorAsync;
    }

    private async Task _processMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message.Body.ToString();
        _logger.LogInformation("Received: {Message}", message);

        // Complete the message. message is deleted from the queue.
        await args.CompleteMessageAsync(args.Message);
    }

    private async Task _processErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error processing message: {EntityPath}", args.EntityPath);
        await Task.CompletedTask;
    }


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting processing messages");

        await _processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Started processing messages");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        _logger.LogInformation("Stopped processing messages");
    }
}
