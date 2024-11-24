using Azure.Messaging.ServiceBus;
using Demo.Consumer;
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
    .AddHostedService<ConsumerWorker>();

var host = builder.Build();
host.Run();
