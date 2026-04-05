namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ;

using global::EasyNetQ;
using global::EasyNetQ.SystemMessages;
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
public sealed class EasyNetQErrorEnvelopeReader : IErrorEnvelopeReader<Error>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public Error Deserialize(RawMessage message)
    {
        var json = Encoding.UTF8.GetString(message.Body.Span);
        return JsonSerializer.Deserialize<Error>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                "Failed to deserialize EasyNetQ error envelope: the message body produced a null result.");
    }

    /// <inheritdoc />
    public ErrorEnvelopeInfo Read(Error error) =>
        new()
        {
            Payload = error.Message,
            SourceQueue = error.Queue,
            ErrorReason = error.Exception,
            ErrorContext = BuildErrorContext(error)
        };

    private static IReadOnlyDictionary<string, string> BuildErrorContext(Error error)
    {
        var context = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(error.Exchange))
        {
            context["exchange"] = error.Exchange;
        }

        if (!string.IsNullOrEmpty(error.RoutingKey))
        {
            context["routingKey"] = error.RoutingKey;
        }

        if (error.DateTime != default)
        {
            context["dateTime"] = error.DateTime.ToString("O");
        }

        if (!string.IsNullOrEmpty(error.BasicProperties.Type))
        {
            context["messageType"] = error.BasicProperties.Type;
        }

        if (!string.IsNullOrEmpty(error.BasicProperties.CorrelationId))
        {
            context["correlationId"] = error.BasicProperties.CorrelationId;
        }

        if (!string.IsNullOrEmpty(error.BasicProperties.MessageId))
        {
            context["messageId"] = error.BasicProperties.MessageId;
        }

        return context;
    }
}
