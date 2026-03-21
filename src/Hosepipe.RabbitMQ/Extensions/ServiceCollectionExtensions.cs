namespace Hosepipe.RabbitMQ.Extensions;

using global::RabbitMQ.Client;
using Hosepipe.Abstractions;
using Hosepipe.Extensions;
using Hosepipe.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

/// <summary>Extension methods for registering Hosepipe RabbitMQ services in the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Hosepipe services backed by RabbitMQ.
    /// Registers <see cref="IQueueReader"/> and <see cref="IMessageRetrier"/> implementations
    /// and manages the RabbitMQ <see cref="IConnection"/> lifecycle as a hosted service.
    /// Configuration is bound from the <c>appsettings.json</c> section <see cref="RabbitMqOptions.SectionName"/>
    /// and validated with data annotations at startup.
    /// </summary>
    /// <param name="builder">The Hosepipe builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHosepipeBuilder WithRabbitMQBroker(this IHosepipeBuilder builder)
    {
        var services = builder.Services;

        services.AddOptionsWithValidateOnStart<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RabbitMqConnectionProvider>());
        services.AddSingleton(sp => sp.GetRequiredService<RabbitMqConnectionProvider>().Connection);

        services.AddTransient<BasicAuthHandler>();
        services.AddHttpClient<RabbitMqManagementClient>((sp, client) =>
                client.BaseAddress = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value.GetManagementSettings().BaseUri)
            .AddHttpMessageHandler<BasicAuthHandler>();

        services.AddSingleton<IBroker, RabbitMqBroker>();

        return builder;
    }
}
