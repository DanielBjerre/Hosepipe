namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the error envelope written to the dead-letter queue by EasyNetQ
/// when a message fails processing.
/// </summary>
/// <param name="RoutingKey">The routing key of the original message.</param>
/// <param name="Exchange">The exchange the original message was published to.</param>
/// <param name="Queue">The queue the message was being consumed from when it failed.</param>
/// <param name="Message">The serialized original message payload.</param>
/// <param name="Exception">The exception details from the failed processing attempt.</param>
/// <param name="DateTime">The date and time the error occurred.</param>
/// <param name="BasicProperties">The AMQP basic properties of the original message.</param>
public sealed record EasyNetQEnvelope(
    [property: JsonPropertyName("routingKey")] string RoutingKey = "",
    [property: JsonPropertyName("exchange")] string Exchange = "",
    [property: JsonPropertyName("queue")] string Queue = "",
    [property: JsonPropertyName("message")] string Message = "",
    [property: JsonPropertyName("exception")] string Exception = "",
    [property: JsonPropertyName("dateTime")] DateTimeOffset DateTime = default,
    [property: JsonPropertyName("basicProperties")] EasyNetQBasicProperties? BasicProperties = null);

/// <summary>
/// A subset of AMQP basic properties recorded in the EasyNetQ error envelope.
/// </summary>
/// <param name="ContentType">The MIME content type of the original message body.</param>
/// <param name="CorrelationId">The correlation identifier of the original message.</param>
/// <param name="MessageId">The unique identifier of the original message.</param>
/// <param name="Type">
/// The fully-qualified EasyNetQ type name of the original message
/// (format: <c>Namespace.Type:Assembly</c>).
/// </param>
public sealed record EasyNetQBasicProperties(
    [property: JsonPropertyName("contentType")] string? ContentType = null,
    [property: JsonPropertyName("correlationId")] string? CorrelationId = null,
    [property: JsonPropertyName("messageId")] string? MessageId = null,
    [property: JsonPropertyName("type")] string? Type = null);
