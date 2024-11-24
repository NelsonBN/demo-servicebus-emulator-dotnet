using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Demo.Tests.Config;

public class IntegrationTestsFactory : IAsyncLifetime
{
    private const int SERVICE_BUS_PORT = 5672;

    private readonly INetwork _containersNetwork;
    private readonly IContainer _azureSqlEdgeContainer;
    private readonly IContainer _serviceBusContainer;



    public IntegrationTestsFactory()
    {
        _containersNetwork = new NetworkBuilder()
            .WithName($"integration-tests-network-{Guid.NewGuid()}")
            .Build();

        const string SQL_PASSWORD = "Ab123456789!";
        var sqlContainerName = $"sqledge-integration-tests-{Guid.NewGuid()}";

        _azureSqlEdgeContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-sql-edge:2.0.0")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SQL_PASSWORD)
            .WithNetwork(_containersNetwork)
            .WithNetworkAliases(sqlContainerName)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("SQL Server is now ready for client connections.", w => w.WithTimeout(TimeSpan.FromSeconds(10))))
            .WithOutputConsumer(
                Consume.RedirectStdoutAndStderrToStream(
                    Console.OpenStandardOutput(),
                    Console.OpenStandardError()))
            .Build();

        _serviceBusContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:1.0.1")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SQL_PASSWORD)
            .WithEnvironment("SQL_SERVER", sqlContainerName)
            .WithPortBinding(SERVICE_BUS_PORT, true)
            .WithBindMount(
                Path.GetFullPath("./Config/ServiceBusEmulator.Config.json"),
                "/ServiceBus_Emulator/ConfigFiles/Config.json")
            .WithNetwork(_containersNetwork)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Emulator Service is Successfully Up!", w => w.WithTimeout(TimeSpan.FromSeconds(60))))
            .WithOutputConsumer(
                Consume.RedirectStdoutAndStderrToStream(
                    Console.OpenStandardOutput(),
                    Console.OpenStandardError()))
            .Build();
    }


    public string GetServiceBusConnectionString()
        => $"Endpoint=sb://{_serviceBusContainer.Hostname}:{_serviceBusContainer.GetMappedPublicPort(SERVICE_BUS_PORT)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _azureSqlEdgeContainer.StartAsync(),
            _serviceBusContainer.StartAsync());

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public Task DisposeAsync()
        => Task.WhenAll(
            _azureSqlEdgeContainer.StopAsync(),
            _serviceBusContainer.StopAsync(),
            _containersNetwork.DeleteAsync());
}

[CollectionDefinition(nameof(CollectionIntegrationTests))]
public sealed class CollectionIntegrationTests : ICollectionFixture<IntegrationTestsFactory> { }
