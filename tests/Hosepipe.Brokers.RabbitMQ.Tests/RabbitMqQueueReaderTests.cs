namespace Hosepipe.Brokers.RabbitMQ.Tests;

using Hosepipe.Models;
using System.Text;

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqQueueReaderTests(RabbitMqFixture fixture)
{
    [Fact]
    public async Task ReadMessagesAsync_WithEmptyQueue_YieldsNothing()
    {
        var queueName = await fixture.DeclareQueueAsync(TestContext.Current.CancellationToken);
        var reader = fixture.Broker;

        var count = 0;
        await foreach (var _ in reader.ReadMessagesAsync(queueName, TestContext.Current.CancellationToken))
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ReadMessagesAsync_WithMessage_YieldsCorrectPayload()
    {
        var queueName = await fixture.DeclareQueueAsync(TestContext.Current.CancellationToken);
        await fixture.PublishAsync(queueName, "test-payload", TestContext.Current.CancellationToken);
        var reader = fixture.Broker;

        RawMessage? received = null;
        await foreach (var msg in reader.ReadMessagesAsync(queueName, TestContext.Current.CancellationToken).Take(1))
        {
            received = msg;
        }

        Assert.NotNull(received);
        Assert.Equal("test-payload", Encoding.UTF8.GetString(received.Body.Span));
    }

    [Fact]
    public async Task ReadMessagesAsync_WithMessages_YieldsExpectedCount()
    {
        var queueName = await fixture.DeclareQueueAsync(TestContext.Current.CancellationToken);
        const int count = 3;
        for (var i = 0; i < count; i++)
        {
            await fixture.PublishAsync(queueName, $"msg-{i}", TestContext.Current.CancellationToken);
        }
        var reader = fixture.Broker;

        var received = 0;
        await foreach (var _ in reader.ReadMessagesAsync(queueName, TestContext.Current.CancellationToken).Take(count))
        {
            received++;
        }

        Assert.Equal(count, received);
    }

    [Fact]
    public async Task ReadMessagesAsync_AfterEnumeration_MessagesRemainInQueue()
    {
        var queueName = await fixture.DeclareQueueAsync(TestContext.Current.CancellationToken);
        const int count = 3;
        for (var i = 0; i < count; i++)
        {
            await fixture.PublishAsync(queueName, $"msg-{i}", TestContext.Current.CancellationToken);
        }
        var reader = fixture.Broker;

        // Take(count) disposes the channel on completion
        await foreach (var _ in reader.ReadMessagesAsync(queueName, TestContext.Current.CancellationToken).Take(count)) { }

        var remaining = await fixture.GetMessageCountAsync(queueName, TestContext.Current.CancellationToken);
        Assert.Equal((uint)count, remaining);
    }

    [Fact]
    public async Task ListQueuesAsync_IncludesDeclaredQueue()
    {
        var queueName = await fixture.DeclareQueueAsync(TestContext.Current.CancellationToken);
        var reader = fixture.Broker;

        var queues = await reader.ListQueuesAsync(TestContext.Current.CancellationToken);

        Assert.Contains(queues, q => q.Name == queueName);
    }

    [Fact]
    public async Task ListQueuesAsync_ReflectsMessageCount()
    {
        var queueName = await fixture.DeclareQueueAsync(TestContext.Current.CancellationToken);
        await fixture.PublishAsync(queueName, "msg-1", TestContext.Current.CancellationToken);
        await fixture.PublishAsync(queueName, "msg-2", TestContext.Current.CancellationToken);
        var reader = fixture.Broker;

        // The management API collects statistics periodically
        // may not reflect immediately after publish. Poll until the count is visible.
        QueueSummary? summary = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (attempt > 0)
            {
                await Task.Delay(500, TestContext.Current.CancellationToken);
            }
            var queues = await reader.ListQueuesAsync(TestContext.Current.CancellationToken);
            summary = queues.SingleOrDefault(q => q.Name == queueName);
            if (summary?.MessageCount == 2u)
            {
                break;
            }
        }

        Assert.NotNull(summary);
        Assert.Equal(2u, summary.MessageCount);
    }
}
