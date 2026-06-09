namespace Arkanis.Overlay.External.FleetYards.Extensions;

using Abstractions;

public static class FleetYardsApiClientExtensions
{
    public static TClient WithAccessToken<TClient>(this TClient client, string? accessToken)
        where TClient : class, IFleetYardsApiClient
    {
        if (client is not FleetYardsApiClientBase baseClient)
        {
            return client;
        }

        var cloned = baseClient.CloneAs<TClient>();
        if (cloned is not FleetYardsApiClientBase clonedBase)
        {
            return cloned;
        }

        clonedBase.OverrideOptions = clonedBase.CurrentOptions with { AccessToken = accessToken };
        return cloned;
    }
}
