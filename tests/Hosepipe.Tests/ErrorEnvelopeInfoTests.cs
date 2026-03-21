namespace Hosepipe.Tests;

using Hosepipe.Models;

public class ErrorEnvelopeInfoTests
{
    [Fact]
    public void ErrorEnvelopeInfo_WithRequiredProperties_HasEmptyContextByDefault()
    {
        var info = new ErrorEnvelopeInfo
        {
            Payload = "{}",
            SourceQueue = "orders",
            ErrorReason = "Deserialization failed"
        };

        Assert.Equal("{}", info.Payload);
        Assert.Equal("orders", info.SourceQueue);
        Assert.Equal("Deserialization failed", info.ErrorReason);
        Assert.Empty(info.ErrorContext);
    }

    [Fact]
    public void ErrorEnvelopeInfo_WithContext_ExposesAllProperties()
    {
        var context = new Dictionary<string, string> { ["exception"] = "NullReferenceException" };

        var info = new ErrorEnvelopeInfo
        {
            Payload = "{\"id\":1}",
            SourceQueue = "orders",
            ErrorReason = "Unhandled exception",
            ErrorContext = context
        };

        Assert.Equal("NullReferenceException", info.ErrorContext["exception"]);
    }
}
