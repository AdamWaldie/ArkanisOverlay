namespace Arkanis.Overlay.Infrastructure.Services.FleetYards;

public interface IFleetYardsPublicHangarProvider
{
    bool IsAvailable { get; }

    Task<IReadOnlyList<FleetYardsHangarVehicle>> GetHangarAsync(string username, CancellationToken cancellationToken = default);
}
