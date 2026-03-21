namespace Hosepipe.Extensions;

using Hosepipe.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

/// <summary>Extension methods for mapping Hosepipe endpoints into an ASP.NET Core application.</summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps all Hosepipe inspection and retry endpoints under a route prefix.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix for all Hosepipe endpoints. Defaults to <c>/hosepipe</c>.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapHosepipeEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/hosepipe")
    {
        var group = endpoints.MapGroup(prefix);
        HosepipeEndpoints.Map(group);
        return endpoints;
    }
}
