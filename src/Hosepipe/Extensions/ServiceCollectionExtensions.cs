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
    /// <returns>An <see cref="IHosepipeBuilder"/> for further configuration.</returns>
    public static IHosepipeBuilder AddHosepipe(this IServiceCollection services)
    {
        return new HosepipeBuilder(services);
    }

    /// <summary>
    /// Registers a custom <see cref="IErrorEnvelopeReader"/> implementation.
    /// Call this to support a messaging library''s specific envelope schema.
    /// </summary>
    /// <typeparam name="TReader">The envelope reader implementation to register.</typeparam>
    /// <param name="builder">The Hosepipe builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHosepipeBuilder AddHosepipeEnvelopeReader<TReader>(this IHosepipeBuilder builder)
        where TReader : class, IErrorEnvelopeReader
    {
        builder.Services.AddSingleton<IErrorEnvelopeReader, TReader>();
        return builder;
    }
}
