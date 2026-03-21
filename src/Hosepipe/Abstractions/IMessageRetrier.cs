namespace Hosepipe.Abstractions;

/// <summary>
/// Retries failed messages by republishing them to their original source queues
/// and removing them from the dead-letter queue.
/// </summary>
public interface IMessageRetrier
{
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
