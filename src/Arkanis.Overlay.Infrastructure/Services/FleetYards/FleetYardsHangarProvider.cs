namespace Arkanis.Overlay.Infrastructure.Services.FleetYards;

using External;
using Microsoft.Extensions.Logging;
using Overlay.External.FleetYards.Abstractions;

public partial class FleetYardsHangarProvider(
    FleetYardsAccountContext accountContext,
    IFleetYardsHangarApi hangarApi,
    ILogger<FleetYardsHangarProvider> logger
) : IFleetYardsHangarProvider
{
    private IReadOnlyList<FleetYardsHangarVehicle>? _cache;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public bool IsAvailable => accountContext.IsAuthenticated;

    public async Task<IReadOnlyList<FleetYardsHangarVehicle>> GetHangarAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return [];
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && DateTimeOffset.UtcNow < _cacheExpiry)
            {
                return _cache;
            }

            _cache = await FetchAllPagesAsync(cancellationToken);
            _cacheExpiry = DateTimeOffset.UtcNow.Add(CacheTtl);
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void InvalidateCache()
    {
        _cache = null;
        _cacheExpiry = DateTimeOffset.MinValue;
    }

    private async Task<IReadOnlyList<FleetYardsHangarVehicle>> FetchAllPagesAsync(CancellationToken cancellationToken)
    {
        var result = new List<FleetYardsHangarVehicle>();
        var page = 1;
        const int perPage = 50;

        while (true)
        {
            List<FleetYardsHangarVehicleDto> vehicles;
            try
            {
                vehicles = await hangarApi.GetHangarAsync(page, perPage, cancellationToken);
            }
            catch (Exception ex)
            {
                LogFetchFailed(logger, page, ex);
                break;
            }

            if (vehicles.Count == 0)
            {
                break;
            }

            result.AddRange(vehicles.Select(MapVehicle));

            if (vehicles.Count < perPage)
            {
                break;
            }

            page++;
        }

        LogFetchedVehicles(logger, result.Count);
        return result;
    }

    private static FleetYardsHangarVehicle MapVehicle(FleetYardsHangarVehicleDto dto)
    {
        var imageUrl = dto.Model?.Media?
            .FirstOrDefault(m => m.Source?.Kind == "image")?.Source?.Url;

        return new FleetYardsHangarVehicle
        {
            Id = dto.Id ?? string.Empty,
            CustomName = dto.Name,
            ModelName = dto.Model?.Name ?? string.Empty,
            ModelSlug = dto.Model?.Slug ?? string.Empty,
            ManufacturerName = dto.Model?.Manufacturer?.Name,
            ManufacturerSlug = dto.Model?.Manufacturer?.Slug,
            Flagship = dto.Flagship,
            Purchased = dto.Purchased,
            Loaner = dto.Loaner,
            ImageUrl = imageUrl,
        };
    }

    [LoggerMessage(LogLevel.Warning, "Failed to fetch FleetYards hangar page {page}")]
    static partial void LogFetchFailed(ILogger logger, int page, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Fetched {count} vehicles from FleetYards hangar")]
    static partial void LogFetchedVehicles(ILogger logger, int count);
}
