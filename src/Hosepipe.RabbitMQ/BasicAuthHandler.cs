namespace Hosepipe.RabbitMQ;

using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;

/// <summary>
/// Adds a Basic authentication header to every outgoing HTTP request.
/// Credentials are derived from the configured <see cref="RabbitMqOptions"/>.
/// </summary>
internal sealed class BasicAuthHandler(IOptions<RabbitMqOptions> options) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var settings = options.Value.GetManagementSettings();
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return await base.SendAsync(request, cancellationToken);
    }
}
