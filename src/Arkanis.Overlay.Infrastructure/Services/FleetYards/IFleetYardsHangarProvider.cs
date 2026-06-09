namespace Arkanis.Overlay.Infrastructure.Services.FleetYards;

public interface IFleetYardsHangarProvider
{
    bool IsAvailable { get; }

    Task<IReadOnlyList<FleetYardsHangarVehicle>> GetHangarAsync(CancellationToken cancellationToken = default);
}
