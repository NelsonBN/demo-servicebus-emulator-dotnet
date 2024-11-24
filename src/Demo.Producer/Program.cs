using Azure.Messaging.ServiceBus;
using Demo.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddTransient(sp
        => new ServiceBusClient(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("ServiceBus"),
            new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp
            }));

builder.Services
    .AddHostedService<ProducerWorker>();

var host = builder.Build();
host.Run();
