namespace Arkanis.Overlay.External.FleetYards;

using Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Options;
using Overlay.Common.Extensions;
using Overlay.Common.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddFleetYardsApiClients(
        this IServiceCollection services,
        Func<IServiceProvider, IConfigureOptions<FleetYardsApiOptions>>? createOptions = null
    )
        => services
            .AddSingleton(createOptions ?? (_ => new ConfigureOptions<FleetYardsApiOptions>(_ => { })))
            .AddSingleton<IFleetYardsHangarApi, FleetYardsHangarApi>()
            .AddSingleton<IFleetYardsFleetsApi, FleetYardsFleetsApi>();

    public static IServiceCollection AddFleetYardsAuthenticatorServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
        => services
            .AddConfiguration<FleetYardsOptions>(configuration)
            .AddSingleton<FleetYardsAuthenticator>()
            .Alias<ExternalAuthenticator, FleetYardsAuthenticator>()
            .AddSingleton(FleetYardsAuthenticator.CreateRefreshJobScheduleProvider());
}
