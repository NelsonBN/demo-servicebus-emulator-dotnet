using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Demo.Tests;

public class Tests(IntegrationTestsFixture factory)
{
    private readonly IntegrationTestsFixture _factory = factory;


    [Fact]
    public async Task Demo_Test()
    {
        // Arrange
        var queueName = $"demo-queue-{Guid.NewGuid()}";

        var client = new ServiceBusClient(_factory.GetServiceBusConnectionString());
        var adminClient = new ServiceBusAdministrationClient(_factory.GetAdminServiceBusConnectionString());

        var sender = client.CreateSender(queueName);
        var receiver = client.CreateReceiver(queueName);

        var message = $"My new message {DateTime.UtcNow}";


        // Act
        await adminClient.CreateQueueAsync(queueName, cancellationToken: TestContext.Current.CancellationToken);

        await sender.SendMessageAsync(new ServiceBusMessage(message), TestContext.Current.CancellationToken);
        var act = await receiver.ReceiveMessageAsync(cancellationToken: TestContext.Current.CancellationToken);
        var messageReceived = act.Body.ToString();


        // Assert
        Assert.Equal(message, messageReceived);
    }
}
