namespace Arkanis.Overlay.External.FleetYards.Options;

using Overlay.Common.Abstractions;

public class FleetYardsOptions : ISelfBindableOptions
{
    public string Authority { get; set; } = FleetYardsConstants.WebBaseUrl;

    public string ClientId { get; set; } = string.Empty;

    public string[] Scopes { get; set; } =
    [
        "openid",
        "hangar:read",
        "fleet:read",
    ];

    public string SectionPath => "FleetYards";
}
