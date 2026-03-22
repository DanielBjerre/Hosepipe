namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ.Tests;

using global::EasyNetQ;
using global::RabbitMQ.Client;
using Hosepipe.Abstractions;
using Hosepipe.Brokers.RabbitMQ;
using Hosepipe.Brokers.RabbitMQ.Extensions;
using Hosepipe.ErrorEnvelopeReaders.EasyNetQ.Extensions;
using Hosepipe.Extensions;
using Hosepipe.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.RabbitMq;

/// <summary>
/// Integration test fixture that starts a RabbitMQ container, registers EasyNetQ
/// and the Hosepipe RabbitMQ broker in DI, and exposes helpers to produce error
/// envelopes via EasyNetQ and read them back through Hosepipe abstractions.
/// </summary>
public sealed class EasyNetQFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4-management")
        .WithPortBinding(15672, true)
        .Build();

    private IHost _host = null!;

    /// <summary>The name of the default EasyNetQ error queue.</summary>
    public const string ErrorQueue = "EasyNetQ_Default_Error_Queue";

    /// <summary>The EasyNetQ bus used to publish and subscribe to messages.</summary>
    public IBus Bus => _host.Services.GetRequiredService<IBus>();

    /// <summary>The raw RabbitMQ connection for queue inspection.</summary>
    public IConnection Connection => _host.Services.GetRequiredService<IConnection>();

    /// <summary>The Hosepipe broker backed by RabbitMQ.</summary>
    public IBroker Broker => _host.Services.GetRequiredService<IBroker>();

    /// <summary>The registered envelope reader for EasyNetQ envelopes.</summary>
    public IErrorEnvelopeReader EnvelopeReader => _host.Services.GetRequiredService<IErrorEnvelopeReader>();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var amqpConnectionString = _container.GetConnectionString();
        var easyNetQConnectionString =
            $"host={_container.Hostname};port={_container.GetMappedPublicPort(5672)};username=guest;password=guest";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Services
            .AddHosepipe()
            .WithRabbitMQBroker()
            .WithEasyNetQEnvelopeReader();

        builder.Services.AddEasyNetQ(_container.GetConnectionString());

        builder.Services.Configure<RabbitMqOptions>(options =>
        {
            options.ConnectionString = amqpConnectionString;
            options.ManagementPort = _container.GetMappedPublicPort(15672);
        });


        _host = builder.Build();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        await _container.DisposeAsync();
    }

    /// <summary>Consumes a single raw message from the specified queue, removing it from the queue.</summary>
    public async Task<RawMessage?> ConsumeRawMessageAsync(string queueName, CancellationToken cancellationToken = default)
    {
        await using var channel = await Connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var result = await channel.BasicGetAsync(queueName, autoAck: true, cancellationToken);
        if (result is null)
        {
            return null;
        }

        return new RawMessage { Body = result.Body.ToArray() };
    }

    /// <summary>Returns the number of messages currently in the specified queue, or 0 if the queue does not exist.</summary>
    public async Task<uint> GetMessageCountAsync(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var channel = await Connection.CreateChannelAsync(cancellationToken: cancellationToken);
            var result = await channel.QueueDeclarePassiveAsync(queueName, cancellationToken: cancellationToken);
            return result.MessageCount;
        }
        catch
        {
            return 0;
        }
    }
}
