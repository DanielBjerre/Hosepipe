namespace Hosepipe.Brokers.RabbitMQ;

using Hosepipe.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Typed HTTP client for the RabbitMQ Management HTTP API.
/// </summary>
internal sealed class RabbitMqManagementClient(HttpClient httpClient)
{
    /// <summary>
    /// Returns a summary of all queues on the broker, ordered by name.
    /// </summary>
    public async Task<IReadOnlyList<QueueSummary>> ListQueuesAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/api/queues", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var dtos = await JsonSerializer.DeserializeAsync<QueueDto[]>(stream, cancellationToken: cancellationToken);

        if (dtos is null or { Length: 0 })
        {
            return [];
        }

        return [..dtos
            .Select(d => new QueueSummary { Name = d.Name, MessageCount = d.Messages })
            .OrderBy(q => q.Name)];
    }

    private sealed record QueueDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("messages")]
        public uint Messages { get; init; }
    }
}
