namespace Hosepipe.Tests;

using Hosepipe.Abstractions;
using Hosepipe.Models;
using System.Text;

public class IErrorEnvelopeReaderTests
{
    [Fact]
    public void GenericReader_NonGenericRead_DelegatesToTypedImplementation()
    {
        IErrorEnvelopeReader reader = new FakeEnvelopeReader();
        var raw = new RawMessage { Body = Encoding.UTF8.GetBytes("test-payload") };

        var result = reader.Read(raw);

        Assert.Equal("test-payload", result.Payload);
        Assert.Equal("source-queue", result.SourceQueue);
        Assert.Equal("error-reason", result.ErrorReason);
    }

    private sealed class FakeEnvelopeReader : IErrorEnvelopeReader<FakeEnvelope>
    {
        public FakeEnvelope Deserialize(RawMessage message) =>
            new(Encoding.UTF8.GetString(message.Body.Span));

        public ErrorEnvelopeInfo Read(FakeEnvelope envelope) => new()
        {
            Payload = envelope.Body,
            SourceQueue = "source-queue",
            ErrorReason = "error-reason"
        };
    }

    private sealed record FakeEnvelope(string Body);
}
