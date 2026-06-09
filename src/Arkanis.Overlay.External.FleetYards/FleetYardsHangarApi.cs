namespace Arkanis.Overlay.External.FleetYards;

using System.Text.Json;
using Abstractions;
using Microsoft.Extensions.Options;

internal class FleetYardsHangarApi(IHttpClientFactory httpClientFactory, IOptionsMonitor<FleetYardsApiOptions> options)
    : FleetYardsApiClientBase(httpClientFactory, options), IFleetYardsHangarApi
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<FleetYardsHangarVehicleDto>> GetHangarAsync(
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default
    )
    {
        var client = await CreateHttpClientAsync(cancellationToken);
        using var response = await client.GetAsync(
            $"{BaseUrl}/hangar?page={page}&per_page={perPage}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<FleetYardsHangarVehicleDto>>(json, JsonOptions) ?? [];
    }

    public async Task<FleetYardsHangarStatsDto?> GetHangarStatsAsync(CancellationToken cancellationToken = default)
    {
        var client = await CreateHttpClientAsync(cancellationToken);
        using var response = await client.GetAsync($"{BaseUrl}/hangar/stats", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<FleetYardsHangarStatsDto>(json, JsonOptions);
    }
}
