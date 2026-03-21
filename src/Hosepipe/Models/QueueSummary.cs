namespace Hosepipe.Models;

/// <summary>
/// A snapshot of a queue's name and its current message count.
/// </summary>
public sealed record QueueSummary
{
    /// <summary>The name of the queue.</summary>
    public required string Name { get; init; }

    /// <summary>The total number of messages currently in the queue.</summary>
    public required uint MessageCount { get; init; }
}
