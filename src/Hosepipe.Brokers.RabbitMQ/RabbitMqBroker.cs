namespace Hosepipe.Brokers.RabbitMQ;

using global::RabbitMQ.Client;
using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// RabbitMQ implementation of <see cref="IBroker"/>.
/// Reads messages non-destructively (requeued via BasicNack on channel close)
/// and retries failed messages by republishing them to their source queues.
/// All operations on the same method share a single channel to keep delivery tags valid.
/// </summary>
internal sealed class RabbitMqBroker(
    IConnection connection,
    RabbitMqManagementClient managementClient) : IBroker
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<QueueSummary>> ListQueuesAsync(CancellationToken cancellationToken = default)
    {
        return await managementClient.ListQueuesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RawMessage> ReadMessagesAsync(
        string queueName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: false, cancellationToken);
            if (result is null)
            {
                yield break;
            }

            yield return new RawMessage
            {
                Body = result.Body.ToArray(),
                Headers = ExtractHeaders(result.BasicProperties)
            };

            await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> RetryAllAsync(
        string queueName,
        IErrorEnvelopeReader envelopeReader,
        CancellationToken cancellationToken = default)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var retried = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: false, cancellationToken);
            if (result is null)
            {
                break;
            }

            var raw = new RawMessage { Body = result.Body.ToArray() };
            var envelope = envelopeReader.Read(raw);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: envelope.SourceQueue,
                body: Encoding.UTF8.GetBytes(envelope.Payload),
                cancellationToken: cancellationToken);

            await channel.BasicAckAsync(result.DeliveryTag, multiple: false, cancellationToken);
            retried++;
        }

        return retried;
    }

    private static IReadOnlyDictionary<string, string> ExtractHeaders(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null)
        {
            return new Dictionary<string, string>();
        }

        var headers = new Dictionary<string, string>(properties.Headers.Count);
        foreach (var (key, value) in properties.Headers)
        {
            headers[key] = value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                not null => value.ToString()!,
                _ => string.Empty
            };
        }

        return headers;
    }
}
