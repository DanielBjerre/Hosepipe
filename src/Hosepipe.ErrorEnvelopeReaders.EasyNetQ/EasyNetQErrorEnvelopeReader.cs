namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ;

using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Text;
using System.Text.Json;

/// <summary>
/// Reads error envelopes produced by EasyNetQ when a message fails processing.
/// </summary>
/// <remarks>
/// EasyNetQ writes a JSON envelope to its error queue (typically <c>EasyNetQ_Default_Error_Queue</c>)
/// containing the original message payload, routing metadata, and exception details.
/// The <see cref="ErrorEnvelopeInfo.SourceQueue"/> is populated from the <c>queue</c> field of the
/// envelope, which is the queue the message was being consumed from when it failed. This matches
/// the default-exchange routing used by <c>IBroker.RetryAllAsync</c>.
/// </remarks>
public sealed class EasyNetQErrorEnvelopeReader : IErrorEnvelopeReader<EasyNetQEnvelope>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public EasyNetQEnvelope Deserialize(RawMessage message)
    {
        var json = Encoding.UTF8.GetString(message.Body.Span);
        return JsonSerializer.Deserialize<EasyNetQEnvelope>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                "Failed to deserialize EasyNetQ error envelope: the message body produced a null result.");
    }

    /// <inheritdoc />
    public ErrorEnvelopeInfo Read(EasyNetQEnvelope envelope) =>
        new()
        {
            Payload = envelope.Message,
            SourceQueue = envelope.Queue,
            ErrorReason = envelope.Exception,
            ErrorContext = BuildErrorContext(envelope)
        };

    private static IReadOnlyDictionary<string, string> BuildErrorContext(EasyNetQEnvelope envelope)
    {
        var context = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(envelope.Exchange))
        {
            context["exchange"] = envelope.Exchange;
        }

        if (!string.IsNullOrEmpty(envelope.RoutingKey))
        {
            context["routingKey"] = envelope.RoutingKey;
        }

        if (envelope.DateTime != default)
        {
            context["dateTime"] = envelope.DateTime.ToString("O");
        }

        if (!string.IsNullOrEmpty(envelope.BasicProperties?.Type))
        {
            context["messageType"] = envelope.BasicProperties.Type;
        }

        if (!string.IsNullOrEmpty(envelope.BasicProperties?.CorrelationId))
        {
            context["correlationId"] = envelope.BasicProperties.CorrelationId;
        }

        if (!string.IsNullOrEmpty(envelope.BasicProperties?.MessageId))
        {
            context["messageId"] = envelope.BasicProperties.MessageId;
        }

        return context;
    }
}
