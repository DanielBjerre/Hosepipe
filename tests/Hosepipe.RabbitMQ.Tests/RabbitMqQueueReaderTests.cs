namespace Hosepipe.RabbitMQ.Tests;

using Hosepipe.Models;
using System.Text;

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqQueueReaderTests(RabbitMqFixture fixture)
{
    [Fact]
    public async Task ReadMessagesAsync_WithEmptyQueue_YieldsNothing()
    {
        var queueName = await fixture.DeclareQueueAsync();
        var reader = new RabbitMqQueueReader(fixture.Connection);

        var count = 0;
        await foreach (var _ in reader.ReadMessagesAsync(queueName))
            count++;

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadMessagesAsync_WithMessage_YieldsCorrectPayload()
    {
        var queueName = await fixture.DeclareQueueAsync();
        await fixture.PublishAsync(queueName, "test-payload");
        var reader = new RabbitMqQueueReader(fixture.Connection);

        RawMessage? received = null;
        await foreach (var msg in reader.ReadMessagesAsync(queueName).Take(1))
            received = msg;

        Assert.NotNull(received);
        Assert.Equal("test-payload", Encoding.UTF8.GetString(received.Body.Span));
    }

    [Fact]
    public async Task ReadMessagesAsync_WithMessages_YieldsExpectedCount()
    {
        var queueName = await fixture.DeclareQueueAsync();
        const int count = 3;
        for (var i = 0; i < count; i++)
            await fixture.PublishAsync(queueName, $"msg-{i}");
        var reader = new RabbitMqQueueReader(fixture.Connection);

        var received = 0;
        await foreach (var _ in reader.ReadMessagesAsync(queueName).Take(count))
            received++;

        Assert.Equal(count, received);
    }

    [Fact]
    public async Task ReadMessagesAsync_AfterEnumeration_MessagesRemainInQueue()
    {
        var queueName = await fixture.DeclareQueueAsync();
        const int count = 3;
        for (var i = 0; i < count; i++)
            await fixture.PublishAsync(queueName, $"msg-{i}");
        var reader = new RabbitMqQueueReader(fixture.Connection);

        // Take(count) disposes the channel on completion; RabbitMQ requeues all unacked messages.
        await foreach (var _ in reader.ReadMessagesAsync(queueName).Take(count)) { }

        var remaining = await fixture.GetMessageCountAsync(queueName);
        Assert.Equal((uint)count, remaining);
    }
}
