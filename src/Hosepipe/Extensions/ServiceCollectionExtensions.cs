namespace Hosepipe.Extensions;

using Hosepipe.Abstractions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering Hosepipe services in the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Hosepipe services to the service collection.
    /// Must be paired with a broker-specific registration (e.g. <c>AddHosepipeRabbitMq</c>)
    /// and an envelope reader registration (e.g. <see cref="AddHosepipeEnvelopeReader{TReader}"/>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHosepipe(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="IErrorEnvelopeReader"/> implementation.
    /// Call this to support a messaging library''s specific envelope schema.
    /// </summary>
    /// <typeparam name="TReader">The envelope reader implementation to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHosepipeEnvelopeReader<TReader>(this IServiceCollection services)
        where TReader : class, IErrorEnvelopeReader
    {
        services.AddSingleton<IErrorEnvelopeReader, TReader>();
        return services;
    }
}
