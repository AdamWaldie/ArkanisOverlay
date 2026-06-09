namespace Arkanis.Overlay.External.FleetYards;

using System.ComponentModel.DataAnnotations;

public record FleetYardsApiOptions
{
    [Url]
    public string BaseUrl { get; set; } = FleetYardsConstants.ApiBaseUrl;

    public string? AccessToken { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}
