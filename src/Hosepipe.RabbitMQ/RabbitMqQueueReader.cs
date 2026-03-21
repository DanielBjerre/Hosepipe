namespace Hosepipe.RabbitMQ;

using global::RabbitMQ.Client;
using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// Reads messages from a RabbitMQ queue for inspection.
/// Messages are fetched without acknowledgement and are returned to the queue
/// when the channel is closed at the end of enumeration.
/// </summary>
internal sealed class RabbitMqQueueReader : IQueueReader
{
    private readonly IConnection _connection;

    public RabbitMqQueueReader(IConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<RawMessage> ReadMessagesAsync(
        string queueName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: false, cancellationToken);
            if (result is null)
                yield break;

            yield return new RawMessage
            {
                Body = result.Body.ToArray(),
                Headers = ExtractHeaders(result.BasicProperties)
            };

            await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    private static IReadOnlyDictionary<string, string> ExtractHeaders(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null)
            return new Dictionary<string, string>();

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
