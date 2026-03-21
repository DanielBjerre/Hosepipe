namespace Hosepipe.Endpoints;

using Hosepipe.Abstractions;
using Hosepipe.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
        IQueueReader queueReader,
        IErrorEnvelopeReader envelopeReader,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var message in queueReader.ReadMessagesAsync(queueName, cancellationToken))
        {
            yield return envelopeReader.Read(message);
        }
    }

    private static async Task<IResult> RetryAll(
        string queueName,
        IMessageRetrier retrier,
        IErrorEnvelopeReader envelopeReader,
        CancellationToken cancellationToken)
    {
        var retried = await retrier.RetryAllAsync(queueName, envelopeReader, cancellationToken);
        return Results.Ok(new RetryResult(RetriedCount: retried));
    }
}
