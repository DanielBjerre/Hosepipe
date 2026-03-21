namespace Hosepipe.Abstractions;

using Hosepipe.Models;

/// <summary>
/// Reads raw messages from a message queue for inspection purposes.
/// Implementations must not destructively consume messages — unread messages
/// must be returned to the queue when enumeration completes.
/// </summary>
public interface IQueueReader
{
    /// <summary>
    /// Reads all currently available messages from the specified queue.
    /// </summary>
    /// <param name="queueName">The name of the queue to read from.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async sequence of raw messages.</returns>
    IAsyncEnumerable<RawMessage> ReadMessagesAsync(string queueName, CancellationToken cancellationToken = default);
}
