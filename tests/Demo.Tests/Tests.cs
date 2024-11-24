using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Demo.Tests.Config;
using Xunit.Abstractions;

namespace Demo.Tests;

[Collection(nameof(CollectionIntegrationTests))]
public class Tests(
    IntegrationTestsFactory factory,
    ITestOutputHelper output)
{
    private readonly IntegrationTestsFactory _factory = factory;
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task Demo_test()
    {
        // Arrange
        var queueName = "demo-queue";
        var client = new ServiceBusClient(_factory.GetServiceBusConnectionString());

        var sender = client.CreateSender(queueName);
        var receiver = client.CreateReceiver(queueName);

        var message = $"My new message {DateTime.UtcNow}";


        // Act
        await sender.SendMessageAsync(new ServiceBusMessage(message));
        var act = await receiver.ReceiveMessageAsync();
        var messageReceived = act.Body.ToString();


        // Assert
        Assert.Equal(message, messageReceived);
    }

    [Fact]
    public void Is_file_Exists()
    {
        // Arrange && Act
        var path = Path.GetFullPath("./Config/ServiceBusEmulator.Config.json");


        // Assert
        _output.WriteLine($"Path: {path} -> Exists: {File.Exists(path)}");
        Assert.True(File.Exists(path));
    }
}
