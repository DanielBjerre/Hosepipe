namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ.Extensions;

using Hosepipe.Extensions;

/// <summary>Extension methods for registering the EasyNetQ envelope reader with Hosepipe.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="EasyNetQErrorEnvelopeReader"/> as the <see cref="Hosepipe.Abstractions.IErrorEnvelopeReader"/>
    /// implementation for Hosepipe, enabling support for error envelopes produced by EasyNetQ.
    /// </summary>
    /// <param name="builder">The Hosepipe builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHosepipeBuilder WithEasyNetQEnvelopeReader(this IHosepipeBuilder builder) =>
        builder.AddHosepipeEnvelopeReader<EasyNetQErrorEnvelopeReader>();
}
