namespace Hosepipe.Abstractions;

using Hosepipe.Models;

/// <summary>
/// Represents a message broker that supports queue inspection and message retry.
/// Implementations must not destructively consume messages during reads — unread
/// messages must be returned to the queue when enumeration completes.
/// </summary>
public interface IBroker
{
    /// <summary>
    /// Lists all queues visible to the configured broker, along with the number of messages in each.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of queue summaries, ordered by queue name.</returns>
    Task<IReadOnlyList<QueueSummary>> ListQueuesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all currently available messages from the specified queue.
    /// </summary>
    /// <param name="queueName">The name of the queue to read from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async sequence of raw messages.</returns>
    IAsyncEnumerable<RawMessage> ReadMessagesAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all available messages from the specified queue, republishes each one to
    /// its source queue (as identified by the envelope), and removes it from <paramref name="queueName"/>.
    /// </summary>
    /// <param name="queueName">The dead-letter queue to drain and retry from.</param>
    /// <param name="envelopeReader">The reader used to extract envelope information from each raw message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of messages successfully retried.</returns>
    Task<int> RetryAllAsync(string queueName, IErrorEnvelopeReader envelopeReader, CancellationToken cancellationToken = default);
}
