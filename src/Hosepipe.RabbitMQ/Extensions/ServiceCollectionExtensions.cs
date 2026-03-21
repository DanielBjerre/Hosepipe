namespace Hosepipe.RabbitMQ.Extensions;

using global::RabbitMQ.Client;
using Hosepipe.Abstractions;
using Hosepipe.Extensions;
using Hosepipe.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Hosepipe RabbitMQ services in the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Hosepipe services backed by RabbitMQ using an already-created <see cref="IConnection"/>.
    /// Registers <see cref="IQueueReader"/> and <see cref="IMessageRetrier"/> implementations
    /// that communicate with the provided RabbitMQ connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connection">An active RabbitMQ connection managed by the caller.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHosepipeRabbitMq(
        this IServiceCollection services,
        IConnection connection)
    {
        services.AddSingleton(connection);
        services.AddSingleton<IQueueReader, RabbitMqQueueReader>();
        services.AddSingleton<IMessageRetrier, RabbitMqMessageRetrier>();
        services.AddHosepipe();
        return services;
    }
}
