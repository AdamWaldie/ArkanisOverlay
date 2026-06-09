namespace Arkanis.Overlay.External.FleetYards;

using System.Text.Json;
using Abstractions;
using Microsoft.Extensions.Options;

internal class FleetYardsFleetsApi(IHttpClientFactory httpClientFactory, IOptionsMonitor<FleetYardsApiOptions> options)
    : FleetYardsApiClientBase(httpClientFactory, options), IFleetYardsFleetsApi
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<FleetYardsMyFleetDto>> GetMyFleetsAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateHttpClientAsync(cancellationToken);
        using var response = await client.GetAsync($"{BaseUrl}/fleets/my", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<FleetYardsMyFleetDto>>(json, JsonOptions) ?? [];
    }

    public async Task<List<FleetYardsHangarVehicleDto>> GetFleetVehiclesAsync(
        string fleetSlug,
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default
    )
    {
        var client = await CreateHttpClientAsync(cancellationToken);
        using var response = await client.GetAsync(
            $"{BaseUrl}/fleets/{Uri.EscapeDataString(fleetSlug)}/vehicles?page={page}&per_page={perPage}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<FleetYardsHangarVehicleDto>>(json, JsonOptions) ?? [];
    }
}
