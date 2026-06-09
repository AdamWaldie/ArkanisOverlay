namespace Arkanis.Overlay.External.FleetYards;

using Microsoft.Extensions.Options;
using Overlay.Common.Options;

public class FleetYardsLinkHelper(IOptionsMonitor<ArkanisRestBackendOptions> backendOptions)
{
    public string GetLinkAccountUrl()
        => new Uri(backendOptions.CurrentValue.BaseAddress, "/api/v1/overlay/connect/link-account/fleetyards").ToString();
}
