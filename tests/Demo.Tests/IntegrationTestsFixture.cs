using System;
using System.Threading.Tasks;
using Demo.Tests;
using DotNet.Testcontainers.Builders;
using Testcontainers.ServiceBus;

[assembly: AssemblyFixture(typeof(IntegrationTestsFixture))]

namespace Demo.Tests;

public class IntegrationTestsFixture : IAsyncLifetime
{
    private const int SERVICE_BUS_ADMIN_PORT = 5300;
    private readonly ServiceBusContainer _serviceBusContainer;

    public IntegrationTestsFixture()
        => _serviceBusContainer = new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.0")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithOutputConsumer(
                Consume.RedirectStdoutAndStderrToStream(
                    Console.OpenStandardOutput(),
                    Console.OpenStandardError()))
            .Build();


    public string GetServiceBusConnectionString()
        => _serviceBusContainer.GetConnectionString();

    public string GetAdminServiceBusConnectionString()
        => $"Endpoint=sb://{_serviceBusContainer.Hostname}:{_serviceBusContainer.GetMappedPublicPort(SERVICE_BUS_ADMIN_PORT)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public async ValueTask InitializeAsync() => await _serviceBusContainer.StartAsync();
    public async ValueTask DisposeAsync() => await _serviceBusContainer.StopAsync();
}
