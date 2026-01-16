using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        => sp.GetRequiredService<ServiceBusClient>().CreateProcessor(
            sp.GetRequiredService<IConfiguration>()["QueueName"],
            new ServiceBusProcessorOptions()));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();


class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;
    private readonly ServiceBusAdministrationClient _admin;
    private readonly ServiceBusProcessor _processor;
    private readonly string _queueName;

    public Worker(
        ILogger<Worker> logger,
        ServiceBusAdministrationClient admin,
        ServiceBusProcessor processor,
        IConfiguration configuration)
    {
        _logger = logger;
        _admin = admin;
        _processor = processor;
        _queueName = configuration["QueueName"];
        _processor.ProcessMessageAsync += _processMessageAsync;
        _processor.ProcessErrorAsync += _processErrorAsync;
    }

    private async Task _processMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message.Body.ToString();
        _logger.LogInformation("Received: {Message}", message);
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

        await _processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Started processing messages");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        _logger.LogInformation("Stopped processing messages");
    }
}
