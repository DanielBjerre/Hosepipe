namespace Hosepipe.ErrorEnvelopeReaders.EasyNetQ.Tests;

using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Text;

public class EasyNetQErrorEnvelopeReaderTests
{
    private readonly EasyNetQErrorEnvelopeReader _reader = new();

    private static RawMessage ToRawMessage(string json) =>
        new() { Body = Encoding.UTF8.GetBytes(json) };

    [Fact]
    public void Deserialize_WithValidJson_ReturnsPopulatedEnvelope()
    {
        // Arrange
        var json = """
            {
                "routingKey": "orders.created",
                "exchange": "orders-exchange",
                "queue": "orders-queue",
                "message": "{\"id\":1}",
                "exception": "System.Exception: error",
                "dateTime": "2024-01-15T10:00:00+00:00",
                "basicProperties": {
                    "contentType": "application/json",
                    "correlationId": "corr-123",
                    "messageId": "msg-456",
                    "type": "MyApp.OrderMessage:MyApp"
                }
            }
            """;
        var raw = ToRawMessage(json);

        // Act
        var envelope = _reader.Deserialize(raw);

        // Assert
        Assert.Equal("orders.created", envelope.RoutingKey);
        Assert.Equal("orders-exchange", envelope.Exchange);
        Assert.Equal("orders-queue", envelope.Queue);
        Assert.Equal("{\"id\":1}", envelope.Message);
        Assert.Equal("System.Exception: error", envelope.Exception);
        Assert.Equal(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero), envelope.DateTime);
        Assert.Equal("application/json", envelope.BasicProperties?.ContentType);
        Assert.Equal("corr-123", envelope.BasicProperties?.CorrelationId);
        Assert.Equal("msg-456", envelope.BasicProperties?.MessageId);
        Assert.Equal("MyApp.OrderMessage:MyApp", envelope.BasicProperties?.Type);
    }

    [Fact]
    public void Deserialize_WithEmptyJsonObject_UsesDefaultValues()
    {
        // Arrange
        var raw = ToRawMessage("{}");

        // Act
        var envelope = _reader.Deserialize(raw);

        // Assert
        Assert.Equal("", envelope.RoutingKey);
        Assert.Equal("", envelope.Queue);
        Assert.Equal("", envelope.Message);
        Assert.Equal("", envelope.Exception);
        Assert.Equal(default, envelope.DateTime);
        Assert.Null(envelope.BasicProperties);
    }

    [Fact]
    public void Deserialize_WithNullJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var raw = ToRawMessage("null");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _reader.Deserialize(raw));
    }

    [Fact]
    public void Read_MapsPayloadFromMessage()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(Message: "{\"id\":1}");

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("{\"id\":1}", info.Payload);
    }

    [Fact]
    public void Read_MapsSourceQueueFromQueue()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(Queue: "orders-queue");

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("orders-queue", info.SourceQueue);
    }

    [Fact]
    public void Read_MapsErrorReasonFromException()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(Exception: "System.Exception: something failed");

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("System.Exception: something failed", info.ErrorReason);
    }

    [Fact]
    public void Read_ErrorContext_IncludesExchangeWhenSet()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(Exchange: "orders-exchange");

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("orders-exchange", info.ErrorContext["exchange"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesRoutingKeyWhenSet()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(RoutingKey: "orders.created");

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("orders.created", info.ErrorContext["routingKey"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesDateTimeWhenSet()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var envelope = new EasyNetQEnvelope(DateTime: dateTime);

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal(dateTime.ToString("O"), info.ErrorContext["dateTime"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesMessageTypeWhenSet()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(BasicProperties: new EasyNetQBasicProperties(Type: "MyApp.OrderMessage:MyApp"));

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("MyApp.OrderMessage:MyApp", info.ErrorContext["messageType"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesCorrelationIdWhenSet()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(BasicProperties: new EasyNetQBasicProperties(CorrelationId: "corr-123"));

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("corr-123", info.ErrorContext["correlationId"]);
    }

    [Fact]
    public void Read_ErrorContext_IncludesMessageIdWhenSet()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(BasicProperties: new EasyNetQBasicProperties(MessageId: "msg-456"));

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.Equal("msg-456", info.ErrorContext["messageId"]);
    }

    [Fact]
    public void Read_ErrorContext_ExcludesEmptyExchange()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(Exchange: "");

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.DoesNotContain("exchange", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_ErrorContext_ExcludesEmptyRoutingKey()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(RoutingKey: "");

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.DoesNotContain("routingKey", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_ErrorContext_ExcludesDefaultDateTime()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(DateTime: default);

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.DoesNotContain("dateTime", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_ErrorContext_WithNullBasicProperties_ExcludesBasicPropertyEntries()
    {
        // Arrange
        var envelope = new EasyNetQEnvelope(BasicProperties: null);

        // Act
        var info = _reader.Read(envelope);

        // Assert
        Assert.DoesNotContain("messageType", info.ErrorContext.Keys);
        Assert.DoesNotContain("correlationId", info.ErrorContext.Keys);
        Assert.DoesNotContain("messageId", info.ErrorContext.Keys);
    }

    [Fact]
    public void Read_WithFullEnvelope_BuildsCompleteErrorContext()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var envelope = new EasyNetQEnvelope(
            RoutingKey: "orders.created",
            Exchange: "orders-exchange",
            Queue: "orders-queue",
            Message: "{\"id\":1}",
            Exception: "System.Exception: error",
            DateTime: dateTime,
            BasicProperties: new EasyNetQBasicProperties(
                ContentType: "application/json",
                CorrelationId: "corr-123",
                MessageId: "msg-456",
                Type: "MyApp.OrderMessage:MyApp"));

        // Act
        var info = _reader.Read(envelope);

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
        var json = """{"queue":"orders-queue","message":"payload","exception":"an error"}""";
        var raw = ToRawMessage(json);

        // Act
        var info = reader.Read(raw);

        // Assert
        Assert.Equal("payload", info.Payload);
        Assert.Equal("orders-queue", info.SourceQueue);
        Assert.Equal("an error", info.ErrorReason);
    }
}
