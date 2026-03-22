namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ.Tests;

using global::EasyNetQ;
using Hosepipe.Abstractions;

/// <summary>A simple message type used for integration testing with EasyNetQ.</summary>
public record IntegrationTestMessage(string Text);

[Collection(nameof(EasyNetQCollection))]
public sealed class EasyNetQIntegrationTests(EasyNetQFixture fixture)
{
    [Fact]
    public async Task Deserialize_WithRealEasyNetQErrorEnvelope_ReturnsPopulatedEnvelope()
    {
        // Arrange – subscribe with a handler that always throws, then publish
        var subscriptionId = Guid.NewGuid().ToString("N");
        await using var subscription = await fixture.Bus.PubSub.SubscribeAsync<IntegrationTestMessage>(
            subscriptionId,
            _ => Task.FromException(new InvalidOperationException("Intentional deserialization test failure")),
            TestContext.Current.CancellationToken);

        await fixture.Bus.PubSub.PublishAsync(
            new IntegrationTestMessage(Text: "deserialize-test"),
            TestContext.Current.CancellationToken);

        await WaitForMessageInQueueAsync(EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);

        var rawMessage = await fixture.ConsumeRawMessageAsync(
            EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);
        Assert.NotNull(rawMessage);

        // Act
        var reader = new EasyNetQErrorEnvelopeReader();
        var envelope = reader.Deserialize(rawMessage);

        // Assert
        Assert.Contains("deserialize-test", envelope.Message);
        Assert.NotEmpty(envelope.Queue);
        Assert.Contains("Intentional deserialization test failure", envelope.Exception);
        Assert.NotEqual(default, envelope.DateTime);
        Assert.NotNull(envelope.BasicProperties);
        Assert.NotEmpty(envelope.BasicProperties!.Type!);
    }

    [Fact]
    public async Task Read_WithRealEasyNetQErrorEnvelope_ExtractsCorrectEnvelopeInfo()
    {
        // Arrange – subscribe with a handler that always throws, then publish
        var subscriptionId = Guid.NewGuid().ToString("N");
        await using var subscription = await fixture.Bus.PubSub.SubscribeAsync<IntegrationTestMessage>(
            subscriptionId,
            _ => Task.FromException(new InvalidOperationException("Intentional read test failure")),
            TestContext.Current.CancellationToken);

        await fixture.Bus.PubSub.PublishAsync(
            new IntegrationTestMessage(Text: "read-test"),
            TestContext.Current.CancellationToken);

        await WaitForMessageInQueueAsync(EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);

        var rawMessage = await fixture.ConsumeRawMessageAsync(
            EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);
        Assert.NotNull(rawMessage);

        // Act
        var reader = new EasyNetQErrorEnvelopeReader();
        var info = reader.Read(reader.Deserialize(rawMessage));

        // Assert
        Assert.Contains("read-test", info.Payload);
        Assert.NotEmpty(info.SourceQueue);
        Assert.Contains("Intentional read test failure", info.ErrorReason);
        Assert.True(info.ErrorContext.ContainsKey("dateTime"));
        Assert.True(info.ErrorContext.ContainsKey("messageType"));
    }

    [Fact]
    public async Task Read_ViaInterface_WithRealEasyNetQErrorEnvelope_DelegatesToTypedReader()
    {
        // Arrange – subscribe with a handler that always throws, then publish
        var subscriptionId = Guid.NewGuid().ToString("N");
        await using var subscription = await fixture.Bus.PubSub.SubscribeAsync<IntegrationTestMessage>(
            subscriptionId,
            _ => Task.FromException(new InvalidOperationException("Intentional interface test failure")),
            TestContext.Current.CancellationToken);

        await fixture.Bus.PubSub.PublishAsync(
            new IntegrationTestMessage(Text: "interface-test"),
            TestContext.Current.CancellationToken);

        await WaitForMessageInQueueAsync(EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);

        var rawMessage = await fixture.ConsumeRawMessageAsync(
            EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);
        Assert.NotNull(rawMessage);

        // Act – use the non-generic IErrorEnvelopeReader interface
        IErrorEnvelopeReader reader = fixture.EnvelopeReader;
        var info = reader.Read(rawMessage);

        // Assert
        Assert.Contains("interface-test", info.Payload);
        Assert.NotEmpty(info.SourceQueue);
        Assert.Contains("Intentional interface test failure", info.ErrorReason);
    }

    [Fact]
    public async Task Read_WithRealEnvelope_ErrorContextContainsExpectedKeys()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid().ToString("N");
        await using var subscription = await fixture.Bus.PubSub.SubscribeAsync<IntegrationTestMessage>(
            subscriptionId,
            _ => Task.FromException(new InvalidOperationException("Intentional context test failure")),
            TestContext.Current.CancellationToken);

        await fixture.Bus.PubSub.PublishAsync(
            new IntegrationTestMessage(Text: "context-test"),
            TestContext.Current.CancellationToken);

        await WaitForMessageInQueueAsync(EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);

        var rawMessage = await fixture.ConsumeRawMessageAsync(
            EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);
        Assert.NotNull(rawMessage);

        // Act
        var reader = new EasyNetQErrorEnvelopeReader();
        var info = reader.Read(reader.Deserialize(rawMessage));

        // Assert – EasyNetQ populates exchange, dateTime, and messageType for pub/sub messages.
        // routingKey may be empty for basic pub/sub and is excluded by BuildErrorContext.
        Assert.True(info.ErrorContext.ContainsKey("exchange"), "ErrorContext should contain 'exchange'.");
        Assert.True(info.ErrorContext.ContainsKey("dateTime"), "ErrorContext should contain 'dateTime'.");
        Assert.True(info.ErrorContext.ContainsKey("messageType"), "ErrorContext should contain 'messageType'.");
    }

    [Fact]
    public async Task Read_WithTopicPublish_ErrorContextContainsRoutingKey()
    {
        // Arrange – subscribe and publish with an explicit topic so the routing key is populated
        var subscriptionId = Guid.NewGuid().ToString("N");
        const string topic = "orders.created";

        await using var subscription = await fixture.Bus.PubSub.SubscribeAsync<IntegrationTestMessage>(
            subscriptionId,
            (_, _) => Task.FromException(new InvalidOperationException("Intentional topic test failure")),
            cfg => cfg.WithTopic(topic),
            TestContext.Current.CancellationToken);

        await fixture.Bus.PubSub.PublishAsync(
            new IntegrationTestMessage(Text: "topic-test"),
            topic,
            TestContext.Current.CancellationToken);

        await WaitForMessageInQueueAsync(EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);

        var rawMessage = await fixture.ConsumeRawMessageAsync(
            EasyNetQFixture.ErrorQueue, TestContext.Current.CancellationToken);
        Assert.NotNull(rawMessage);

        // Act
        var reader = new EasyNetQErrorEnvelopeReader();
        var info = reader.Read(reader.Deserialize(rawMessage));

        // Assert
        Assert.Contains("topic-test", info.Payload);
        Assert.True(info.ErrorContext.ContainsKey("routingKey"), "ErrorContext should contain 'routingKey'.");
        Assert.Equal(topic, info.ErrorContext["routingKey"]);
    }

    private async Task WaitForMessageInQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        const int maxAttempts = 50;
        const int delayMs = 200;

        for (var i = 0; i < maxAttempts; i++)
        {
            var count = await fixture.GetMessageCountAsync(queueName, cancellationToken);
            if (count > 0)
            {
                return;
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        throw new TimeoutException($"No message appeared in queue '{queueName}' within {maxAttempts * delayMs}ms.");
    }
}
