namespace Hosepipe.RabbitMQ.Tests;

using global::RabbitMQ.Client;
using Hosepipe.Abstractions;
using Hosepipe.RabbitMQ.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using Testcontainers.RabbitMq;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management")
        .WithPortBinding(15672, true)
        .Build();
    private IHost _host = null!;

    public IConnection Connection => _host.Services.GetRequiredService<IConnection>();
    public IQueueReader QueueReader => _host.Services.GetRequiredService<IQueueReader>();
    public IMessageRetrier MessageRetrier => _host.Services.GetRequiredService<IMessageRetrier>();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{RabbitMqOptions.SectionName}:ConnectionString"] = _container.GetConnectionString(),
            [$"{RabbitMqOptions.SectionName}:ManagementPort"] = _container.GetMappedPublicPort(15672).ToString()
        });
        builder.Services.AddHosepipeRabbitMq();
        _host = builder.Build();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await _container.DisposeAsync();
    }

    /// <summary>Declares a new, uniquely named queue and returns its name.</summary>
    public async Task<string> DeclareQueueAsync(CancellationToken cancellationToken = default)
    {
        var name = Guid.NewGuid().ToString("N");
        await using var channel = await Connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(name, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        return name;
    }

    /// <summary>Publishes a single UTF-8 encoded message to the specified queue.</summary>
    public async Task PublishAsync(string queueName, string body, CancellationToken cancellationToken = default)
    {
        await using var channel = await Connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: Encoding.UTF8.GetBytes(body),
            cancellationToken: cancellationToken);
    }

    /// <summary>Returns the number of messages currently waiting in the queue.</summary>
    public async Task<uint> GetMessageCountAsync(string queueName, CancellationToken cancellationToken = default)
    {
        await using var channel = await Connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var result = await channel.QueueDeclarePassiveAsync(queueName, cancellationToken: cancellationToken);
        return result.MessageCount;
    }
}
