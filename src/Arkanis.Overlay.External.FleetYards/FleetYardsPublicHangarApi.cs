namespace Arkanis.Overlay.External.FleetYards;

using System.Text.Json;
using Abstractions;
using Microsoft.Extensions.Options;

internal class FleetYardsPublicHangarApi(IHttpClientFactory httpClientFactory, IOptionsMonitor<FleetYardsApiOptions> options)
    : FleetYardsApiClientBase(httpClientFactory, options), IFleetYardsPublicHangarApi
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<FleetYardsHangarVehicleDto>> GetPublicHangarAsync(
        string username,
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default
    )
    {
        var client = httpClientFactory.CreateClient(GetType().Name);
        client.Timeout = CurrentOptions.Timeout;
        using var response = await client.GetAsync(
            $"{BaseUrl}/public/hangars/{Uri.EscapeDataString(username)}?page={page}&per_page={perPage}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<FleetYardsHangarVehicleDto>>(json, JsonOptions) ?? [];
    }
}
