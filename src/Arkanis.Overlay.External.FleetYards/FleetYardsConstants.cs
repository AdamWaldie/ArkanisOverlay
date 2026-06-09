namespace Arkanis.Overlay.External.FleetYards;

using Overlay.Common;
using Overlay.Common.Models;

public static class FleetYardsConstants
{
    public const string WebBaseUrl = "https://fleetyards.net";
    public const string ApiBaseUrl = "https://api.fleetyards.net/v1";
    public const string OAuthAuthorizeUrl = "https://fleetyards.net/oauth/authorize";
    public const string OAuthTokenUrl = "https://fleetyards.net/oauth/token";

    public static readonly ExternalAuthenticatorInfo ProviderInfo = new()
    {
        ServiceId = ExternalService.FleetYards,
        DisplayName = "FleetYards",
        Description =
            "FleetYards.net is a community ship database and hangar tracker for Star Citizen."
            + " Connect your FleetYards account to view your owned ships and fleet directly in the overlay.",
    };
}
