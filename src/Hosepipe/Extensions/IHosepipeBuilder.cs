namespace Hosepipe.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A builder for configuring Hosepipe services.
/// </summary>
public interface IHosepipeBuilder
{
    /// <summary>Gets the service collection being configured.</summary>
    IServiceCollection Services { get; }
}

internal sealed class HosepipeBuilder(IServiceCollection services) : IHosepipeBuilder
{
    public IServiceCollection Services => services;
}
