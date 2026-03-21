namespace Hosepipe.RabbitMQ.Tests;

using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Text;

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqMessageRetrierTests(RabbitMqFixture fixture)
{
    [Fact]
    public async Task RetryAllAsync_WithEmptyQueue_ReturnsZero()
    {
        var deadLetterQueue = await fixture.DeclareQueueAsync();
        var retrier = new RabbitMqMessageRetrier(fixture.Connection);

        var count = await retrier.RetryAllAsync(deadLetterQueue, new StubEnvelopeReader("irrelevant"));

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RetryAllAsync_WithMessages_ReturnsRetriedCount()
    {
        var sourceQueue = await fixture.DeclareQueueAsync();
        var deadLetterQueue = await fixture.DeclareQueueAsync();
        const int messageCount = 3;
        for (var i = 0; i < messageCount; i++)
            await fixture.PublishAsync(deadLetterQueue, $"error-payload-{i}");
        var retrier = new RabbitMqMessageRetrier(fixture.Connection);

        var count = await retrier.RetryAllAsync(deadLetterQueue, new StubEnvelopeReader(sourceQueue));

        Assert.Equal(messageCount, count);
    }

    [Fact]
    public async Task RetryAllAsync_WithMessages_EmptiesDeadLetterQueue()
    {
        var sourceQueue = await fixture.DeclareQueueAsync();
        var deadLetterQueue = await fixture.DeclareQueueAsync();
        for (var i = 0; i < 3; i++)
            await fixture.PublishAsync(deadLetterQueue, $"error-payload-{i}");
        var retrier = new RabbitMqMessageRetrier(fixture.Connection);

        await retrier.RetryAllAsync(deadLetterQueue, new StubEnvelopeReader(sourceQueue));

        var remaining = await fixture.GetMessageCountAsync(deadLetterQueue);
        Assert.Equal(0u, remaining);
    }

    [Fact]
    public async Task RetryAllAsync_WithMessages_RepublishesToSourceQueue()
    {
        var sourceQueue = await fixture.DeclareQueueAsync();
        var deadLetterQueue = await fixture.DeclareQueueAsync();
        const int messageCount = 2;
        for (var i = 0; i < messageCount; i++)
            await fixture.PublishAsync(deadLetterQueue, $"error-payload-{i}");
        var retrier = new RabbitMqMessageRetrier(fixture.Connection);

        await retrier.RetryAllAsync(deadLetterQueue, new StubEnvelopeReader(sourceQueue));

        var count = await fixture.GetMessageCountAsync(sourceQueue);
        Assert.Equal((uint)messageCount, count);
    }

    /// <summary>
    /// Treats the raw message body as the payload and routes to a fixed source queue.
    /// Used to isolate retrier logic from envelope parsing details.
    /// </summary>
    private sealed class StubEnvelopeReader(string sourceQueue) : IErrorEnvelopeReader
    {
        public ErrorEnvelopeInfo Read(RawMessage message) => new()
        {
            Payload = Encoding.UTF8.GetString(message.Body.Span),
            SourceQueue = sourceQueue,
            ErrorReason = "Test error"
        };
    }
}
