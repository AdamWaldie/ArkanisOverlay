namespace Arkanis.Overlay.External.FleetYards.Abstractions;

public interface IFleetYardsHangarApi : IFleetYardsApiClient
{
    Task<List<FleetYardsHangarVehicleDto>> GetHangarAsync(
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default
    );

    Task<FleetYardsHangarStatsDto?> GetHangarStatsAsync(CancellationToken cancellationToken = default);
}

public interface IFleetYardsFleetsApi : IFleetYardsApiClient
{
    Task<List<FleetYardsMyFleetDto>> GetMyFleetsAsync(CancellationToken cancellationToken = default);

    Task<List<FleetYardsHangarVehicleDto>> GetFleetVehiclesAsync(
        string fleetSlug,
        int page = 1,
        int perPage = 50,
        CancellationToken cancellationToken = default
    );
}
