namespace Hosepipe.Abstractions;

using Hosepipe.Models;

/// <summary>
/// Extracts normalized <see cref="ErrorEnvelopeInfo"/> from a raw queue message.
/// Implement this interface to support a specific messaging library''s envelope format.
/// </summary>
public interface IErrorEnvelopeReader
{
    /// <summary>Reads and normalizes the error envelope from a raw queue message.</summary>
    /// <param name="message">The raw message as received from the broker.</param>
    /// <returns>Normalized information extracted from the envelope.</returns>
    ErrorEnvelopeInfo Read(RawMessage message);
}

/// <summary>
/// Typed base for implementing <see cref="IErrorEnvelopeReader"/> against a specific envelope type.
/// Consumers implement this interface when their messaging library uses a strongly-typed envelope model.
/// The non-generic <see cref="IErrorEnvelopeReader.Read"/> is fulfilled automatically via the
/// default interface implementation.
/// </summary>
/// <typeparam name="TEnvelope">The envelope type produced by the consumer''s messaging library.</typeparam>
public interface IErrorEnvelopeReader<TEnvelope> : IErrorEnvelopeReader
{
    /// <summary>Deserializes the raw message body into the typed envelope.</summary>
    /// <param name="message">The raw message as received from the broker.</param>
    /// <returns>The deserialized envelope.</returns>
    TEnvelope Deserialize(RawMessage message);

    /// <summary>Extracts normalized information from the typed envelope.</summary>
    /// <param name="envelope">The deserialized envelope object.</param>
    /// <returns>Normalized information extracted from the envelope.</returns>
    ErrorEnvelopeInfo Read(TEnvelope envelope);

    ErrorEnvelopeInfo IErrorEnvelopeReader.Read(RawMessage message) => Read(Deserialize(message));
}
