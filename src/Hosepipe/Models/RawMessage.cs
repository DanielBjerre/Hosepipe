namespace Hosepipe.Models;

/// <summary>
/// Represents a raw message read directly from a queue, before any envelope parsing.
/// </summary>
public sealed record RawMessage
{
    /// <summary>The raw message body bytes as received from the broker.</summary>
    public required ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>Transport-level headers associated with the message.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}
