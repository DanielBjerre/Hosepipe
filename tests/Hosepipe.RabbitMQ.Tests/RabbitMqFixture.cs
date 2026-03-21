namespace Hosepipe.RabbitMQ.Tests;

using global::RabbitMQ.Client;
using System.Text;
using Testcontainers.RabbitMq;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management").Build();

    public IConnection Connection { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        var factory = new ConnectionFactory { Uri = new Uri(_container.GetConnectionString()) };
        Connection = await factory.CreateConnectionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>Declares a new, uniquely named queue and returns its name.</summary>
    public async Task<string> DeclareQueueAsync()
    {
        var name = Guid.NewGuid().ToString("N");
        await using var channel = await Connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(name, durable: false, exclusive: false, autoDelete: false, arguments: null);
        return name;
    }

    /// <summary>Publishes a single UTF-8 encoded message to the specified queue.</summary>
    public async Task PublishAsync(string queueName, string body)
    {
        await using var channel = await Connection.CreateChannelAsync();
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            body: Encoding.UTF8.GetBytes(body));
    }

    /// <summary>Returns the number of messages currently waiting in the queue.</summary>
    public async Task<uint> GetMessageCountAsync(string queueName)
    {
        await using var channel = await Connection.CreateChannelAsync();
        var result = await channel.QueueDeclarePassiveAsync(queueName);
        return result.MessageCount;
    }
}
