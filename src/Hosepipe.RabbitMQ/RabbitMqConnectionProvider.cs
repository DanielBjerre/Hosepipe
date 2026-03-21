namespace Hosepipe.RabbitMQ;

using global::RabbitMQ.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>
/// Manages the lifecycle of the RabbitMQ <see cref="IConnection"/> for Hosepipe.
/// The connection is created asynchronously when the host starts and disposed when the host stops.
/// </summary>
internal sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options) : IHostedService, IAsyncDisposable
{
    private IConnection? _connection;

    /// <summary>Gets the active RabbitMQ connection.</summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before the host has started.</exception>
    public IConnection Connection => _connection
        ?? throw new InvalidOperationException(
            "The RabbitMQ connection has not been initialized. Ensure the application host has fully started.");

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(options.Value.ConnectionString) };
        _connection = await factory.CreateConnectionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
