namespace Hosepipe.RabbitMQ;

using global::RabbitMQ.Client;
using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Text;

/// <summary>
/// Retries failed messages by reading them from a RabbitMQ dead-letter queue,
/// republishing each to its source queue, and acknowledging them from the dead-letter queue.
/// All operations run within a single channel to ensure delivery tags remain valid.
/// </summary>
internal sealed class RabbitMqMessageRetrier : IMessageRetrier
{
    private readonly IConnection _connection;

    public RabbitMqMessageRetrier(IConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async Task<int> RetryAllAsync(
        string queueName,
        IErrorEnvelopeReader envelopeReader,
        CancellationToken cancellationToken = default)
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var retried = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: false, cancellationToken);
            if (result is null)
                break;

            var raw = new RawMessage { Body = result.Body.ToArray() };
            var envelope = envelopeReader.Read(raw);

            var body = Encoding.UTF8.GetBytes(envelope.Payload);
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: envelope.SourceQueue,
                body: body,
                cancellationToken: cancellationToken);

            await channel.BasicAckAsync(result.DeliveryTag, multiple: false, cancellationToken);
            retried++;
        }

        return retried;
    }
}
