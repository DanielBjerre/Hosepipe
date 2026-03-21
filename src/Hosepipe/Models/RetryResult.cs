namespace Hosepipe.Models;

/// <summary>The result returned after a retry-all operation.</summary>
/// <param name="RetriedCount">The number of messages successfully retried.</param>
public sealed record RetryResult(int RetriedCount);
