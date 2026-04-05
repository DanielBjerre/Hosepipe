namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ.Tests;

using global::EasyNetQ;
using global::EasyNetQ.SystemMessages;
using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Text;
using System.Text.Json;

public class EasyNetQErrorEnvelopeReaderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly EasyNetQErrorEnvelopeReader _reader = new();

    private static RawMessage ToRawMessage(Error error) =>
        new() { Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(error, JsonOptions)) };

    private static Error BuildError(
        string routingKey = "",
        string exchange = "",
        string queue = "",
        string exception = "",
        string message = "",
        DateTime dateTime = default,
        MessageProperties? basicProperties = null) =>
        new(routingKey, exchange, queue, exception, message, dateTime, basicProperties ?? new MessageProperties());

    [Fact]
    public void Deserialize_WithValidJson_ReturnsPopulatedEnvelope()
    {
        // Arrange – construct the real EasyNetQ error model and serialize it
        var error = new Error(
            routingKey: "orders.created",
            exchange: "orders-exchange",
            queue: "orders-queue",
            exception: "System.Exception: error",
            message: "{\"id\":1}",
            dateTime: new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            basicProperties: new MessageProperties
            {
                ContentType = "application/json",
                CorrelationId = "corr-123",
                MessageId = "msg-456",
                Type = "MyApp.OrderMessage:MyApp"
            });
        var raw = ToRawMessage(error);

        // Act
        var envelope = _reader.Deserialize(raw);

        // Assert
        Assert.Equal("orders.created", envelope.RoutingKey);
        Assert.Equal("orders-exchange", envelope.Exchange);
        Assert.Equal("orders-queue", envelope.Queue);
        Assert.Equal("{\"id\":1}", envelope.Message);
        Assert.Equal("System.Exception: error", envelope.Exception);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), envelope.DateTime);
        Assert.Equal("application/json", envelope.BasicProperties.ContentType);
        Assert.Equal("corr-123", envelope.BasicProperties.CorrelationId);
        Assert.Equal("msg-456", envelope.BasicProperties.MessageId);
        Assert.Equal("MyApp.OrderMessage:MyApp", envelope.BasicProperties.Type);
    }

    [Fact]
    public void Deserialize_WithDefaultEnvelope_UsesDefaultValues()
    {
        // Arrange – a real EasyNetQ error always has a MessageProperties object
        var error = new Error(
            routingKey: "",
            exchange: "",
            queue: "",
            exception: "",
            message: "",
            dateTime: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            basicProperties: new MessageProperties());
        var raw = ToRawMessage(error);

        // Act
        var envelope = _reader.Deserialize(raw);

        // Assert
        Assert.Equal("", envelope.RoutingKey);
        Assert.Equal("", envelope.Queue);
        Assert.Equal("", envelope.Message);
        Assert.Equal("", envelope.Exception);
        Assert.Equal(default, envelope.DateTime);
        Assert.Null(envelope.BasicProperties.Type);
        Assert.Null(envelope.BasicProperties.CorrelationId);
        Assert.Null(envelope.BasicProperties.MessageId);
    }

    [Fact]
    public void Deserialize_WithNullJsonLiteral_ThrowsInvalidOperationException()
    {
        // Arrange – "null" is valid JSON but cannot produce an envelope instance
        var raw = new RawMessage { Body = Encoding.UTF8.GetBytes("null") };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _reader.Deserialize(raw));
    }

    [Fact]
    public void Read_MapsPayloadFromMessage()
    {
        // Arrange
        var error = BuildError(message: "{\"id\":1}");

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("{\"id\":1}", info.Payload);
    }

    [Fact]
    public void Read_MapsSourceQueueFromQueue()
    {
        // Arrange
        var error = BuildError(queue: "orders-queue");

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("orders-queue", info.SourceQueue);
    }

    [Fact]
    public void Read_MapsErrorReasonFromException()
    {
        // Arrange
        var error = BuildError(exception: "System.Exception: something failed");

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("System.Exception: something failed", info.ErrorReason);
    }

    [Fact]
    public void Read_ErrorContext_IncludesExchangeWhenSet()
    {
        // Arrange
        var error = BuildError(exchange: "orders-exchange");

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("orders-exchange", info.ErrorContext["exchange"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesRoutingKeyWhenSet()
    {
        // Arrange
        var error = BuildError(routingKey: "orders.created");

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("orders.created", info.ErrorContext["routingKey"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesDateTimeWhenSet()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var error = BuildError(dateTime: dateTime);

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal(dateTime.ToString("O"), info.ErrorContext["dateTime"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesMessageTypeWhenSet()
    {
        // Arrange
        var error = BuildError(basicProperties: new MessageProperties { Type = "MyApp.OrderMessage:MyApp" });

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("MyApp.OrderMessage:MyApp", info.ErrorContext["messageType"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesCorrelationIdWhenSet()
    {
        // Arrange
        var error = BuildError(basicProperties: new MessageProperties { CorrelationId = "corr-123" });

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("corr-123", info.ErrorContext["correlationId"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesMessageIdWhenSet()
    {
        // Arrange
        var error = BuildError(basicProperties: new MessageProperties { MessageId = "msg-456" });

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("msg-456", info.ErrorContext["messageId"]);
    }

    [Fact]
    public void Read_ErrorContext_ExcludesEmptyExchange()
    {
        // Arrange
        var error = BuildError(exchange: "");

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.DoesNotContain("exchange", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_ErrorContext_ExcludesEmptyRoutingKey()
    {
        // Arrange
        var error = BuildError(routingKey: "");

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.DoesNotContain("routingKey", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_ErrorContext_ExcludesDefaultDateTime()
    {
        // Arrange
        var error = BuildError(dateTime: default);

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.DoesNotContain("dateTime", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_ErrorContext_WithUnsetMessageProperties_ExcludesBasicPropertyEntries()
    {
        // Arrange – MessageProperties with all optional fields left at their defaults (null)
        var error = BuildError(basicProperties: new MessageProperties());

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.DoesNotContain("messageType", info.ErrorContext.Keys);
        Assert.DoesNotContain("correlationId", info.ErrorContext.Keys);
        Assert.DoesNotContain("messageId", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_WithFullError_BuildsCompleteErrorContext()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var error = new Error(
            routingKey: "orders.created",
            exchange: "orders-exchange",
            queue: "orders-queue",
            exception: "System.Exception: error",
            message: "{\"id\":1}",
            dateTime: dateTime,
            basicProperties: new MessageProperties
            {
                ContentType = "application/json",
                CorrelationId = "corr-123",
                MessageId = "msg-456",
                Type = "MyApp.OrderMessage:MyApp"
            });

        // Act
        var info = _reader.Read(error);

        // Assert
        Assert.Equal("{\"id\":1}", info.Payload);
        Assert.Equal("orders-queue", info.SourceQueue);
        Assert.Equal("System.Exception: error", info.ErrorReason);
        Assert.Equal("orders-exchange", info.ErrorContext["exchange"]);
        Assert.Equal("orders.created", info.ErrorContext["routingKey"]);
        Assert.Equal(dateTime.ToString("O"), info.ErrorContext["dateTime"]);
        Assert.Equal("MyApp.OrderMessage:MyApp", info.ErrorContext["messageType"]);
        Assert.Equal("corr-123", info.ErrorContext["correlationId"]);
        Assert.Equal("msg-456", info.ErrorContext["messageId"]);
    }

    [Fact]
    public void Read_ViaInterface_DelegatesToDeserializeThenRead()
    {
        // Arrange
        IErrorEnvelopeReader reader = _reader;
        var error = new Error(
            routingKey: "",
            exchange: "",
            queue: "orders-queue",
            exception: "an error",
            message: "payload",
            dateTime: DateTime.UtcNow,
            basicProperties: new MessageProperties());
        var raw = ToRawMessage(error);

        // Act
        var info = reader.Read(raw);

        // Assert
        Assert.Equal("payload", info.Payload);
        Assert.Equal("orders-queue", info.SourceQueue);
        Assert.Equal("an error", info.ErrorReason);
    }
}