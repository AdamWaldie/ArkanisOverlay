namespace Arkanis.Overlay.Infrastructure.Services.FleetYards;

public class FleetYardsHangarVehicle
{
    public required string Id { get; init; }

    public string? CustomName { get; init; }

    public required string ModelName { get; init; }

    public required string ModelSlug { get; init; }

    public string? ManufacturerName { get; init; }

    public string? ManufacturerSlug { get; init; }

    public bool Flagship { get; init; }

    public bool Purchased { get; init; }

    public bool Loaner { get; init; }

    public string? ImageUrl { get; init; }
}
