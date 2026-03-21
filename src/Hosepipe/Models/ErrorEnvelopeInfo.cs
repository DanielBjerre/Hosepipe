namespace Hosepipe.Models;

/// <summary>
/// Normalized information extracted from an error envelope, independent of the envelope schema
/// used by the underlying messaging library.
/// </summary>
public sealed record ErrorEnvelopeInfo
{
    /// <summary>The original message payload that failed processing.</summary>
    public required string Payload { get; init; }

    /// <summary>The name of the queue or exchange the message originally came from.</summary>
    public required string SourceQueue { get; init; }

    /// <summary>The reason the message failed processing.</summary>
    public required string ErrorReason { get; init; }

    /// <summary>Additional error context properties provided by the messaging library.</summary>
    public IReadOnlyDictionary<string, string> ErrorContext { get; init; } = new Dictionary<string, string>();
}
