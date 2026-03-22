namespace Hosepipe.Endpoints;

using Hosepipe.Abstractions;
using Hosepipe.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Runtime.CompilerServices;

internal static class HosepipeEndpoints
{
    internal static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/queues/{queueName}/messages", GetMessages)
            .WithName("GetMessages")
            .Produces<IAsyncEnumerable<ErrorEnvelopeInfo>>();

        group.MapPost("/queues/{queueName}/messages/retry-all", RetryAll)
            .WithName("RetryAll")
            .Produces<RetryResult>();
    }

    private static async IAsyncEnumerable<ErrorEnvelopeInfo> GetMessages(
        string queueName,
        IBroker broker,
        IErrorEnvelopeReader envelopeReader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var message in broker.ReadMessagesAsync(queueName, cancellationToken))
        {
            yield return envelopeReader.Read(message);
        }
    }

    private static async Task<IResult> RetryAll(
        string queueName,
        IBroker broker,
        IErrorEnvelopeReader envelopeReader,
        CancellationToken cancellationToken)
    {
        var retried = await broker.RetryAllAsync(queueName, envelopeReader, cancellationToken);
        return Results.Ok(new RetryResult(RetriedCount: retried));
    }
}
