namespace Hosepipe.RabbitMQ;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration for the Hosepipe RabbitMQ integration.
/// The AMQP connection string is the single source of truth — the host and credentials
/// it contains are reused to reach the Management HTTP API when
/// <see cref="ManagementPort"/> is set.
/// </summary>
public sealed record RabbitMqOptions
{
    /// <summary>The configuration section name used to bind <see cref="RabbitMqOptions"/> from <c>appsettings.json</c>.</summary>
    public const string SectionName = "Hosepipe:RabbitMQ";

    /// <summary>
    /// The AMQP connection string used to connect to RabbitMQ,
    /// e.g. <c>amqp://user:password@host:5672/vhost</c>.
    /// The host and credentials are also used to reach the Management HTTP API
    /// when <see cref="ManagementPort"/> is configured.
    /// </summary>
    [Required]
    public required string ConnectionString { get; set; }

    /// <summary>
    /// The port of the RabbitMQ Management HTTP API (typically <c>15672</c>).
    /// Used to enable <see cref="Abstractions.IBroker.ListQueuesAsync"/>.
    /// </summary>
    [Range(1, 65535)]
    public int ManagementPort { get; set; } = 15672;

    /// <summary>
    /// Derives the Management HTTP API base URI and credentials from this options instance.
    /// </summary>
    internal RabbitMqManagementSettings GetManagementSettings()
    {
        var uri = new Uri(ConnectionString);
        var parts = uri.UserInfo.Split(':', 2);
        var scheme = uri.Scheme == "amqps" ? "https" : "http";
        return new RabbitMqManagementSettings(
            BaseUri: new Uri($"{scheme}://{uri.Host}:{ManagementPort}"),
            Username: Uri.UnescapeDataString(parts[0]),
            Password: parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : ""
        );
    }
}

/// <summary>Resolved Management HTTP API connection settings derived from <see cref="RabbitMqOptions"/>.</summary>
internal sealed record RabbitMqManagementSettings(Uri BaseUri, string Username, string Password);
