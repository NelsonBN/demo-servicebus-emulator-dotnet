using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Demo.Tests.Config;

namespace Demo.Tests;

[Collection(nameof(CollectionIntegrationTests))]
public class Tests(IntegrationTestsFactory factory)
{
    private readonly IntegrationTestsFactory _factory = factory;

    [Fact]
    public async Task Test()
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
}
