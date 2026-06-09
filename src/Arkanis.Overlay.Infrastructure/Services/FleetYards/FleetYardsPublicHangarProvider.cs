namespace Arkanis.Overlay.Infrastructure.Services.FleetYards;

using External;
using Microsoft.Extensions.Logging;
using Overlay.External.FleetYards.Abstractions;

public partial class FleetYardsPublicHangarProvider(
    CitizenIdAccountContext citizenIdAccountContext,
    IFleetYardsPublicHangarApi publicHangarApi,
    ILogger<FleetYardsPublicHangarProvider> logger
) : IFleetYardsPublicHangarProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IReadOnlyList<FleetYardsHangarVehicle>? _cache;
    private string? _cachedUsername;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;

    public bool IsAvailable => citizenIdAccountContext.RsiIdentity.IsAuthenticated;

    public async Task<IReadOnlyList<FleetYardsHangarVehicle>> GetHangarAsync(string username, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && _cachedUsername == username && DateTimeOffset.UtcNow < _cacheExpiry)
            {
                return _cache;
            }

            _cache = await FetchAllPagesAsync(username, cancellationToken);
            _cachedUsername = username;
            _cacheExpiry = DateTimeOffset.UtcNow.Add(CacheTtl);
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IReadOnlyList<FleetYardsHangarVehicle>> FetchAllPagesAsync(string username, CancellationToken cancellationToken)
    {
        var result = new List<FleetYardsHangarVehicle>();
        var page = 1;
        const int perPage = 50;

        while (true)
        {
            List<FleetYardsHangarVehicleDto> vehicles;
            try
            {
                vehicles = await publicHangarApi.GetPublicHangarAsync(username, page, perPage, cancellationToken);
            }
            catch (Exception ex)
            {
                LogFetchFailed(logger, username, page, ex);
                break;
            }

            if (vehicles.Count == 0)
            {
                break;
            }

            result.AddRange(vehicles.Select(dto =>
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
            }));

            if (vehicles.Count < perPage)
            {
                break;
            }

            page++;
        }

        LogFetchedVehicles(logger, username, result.Count);
        return result;
    }

    [LoggerMessage(LogLevel.Warning, "Failed to fetch FleetYards public hangar page {page} for {username}")]
    static partial void LogFetchFailed(ILogger logger, string username, int page, Exception ex);

    [LoggerMessage(LogLevel.Debug, "Fetched {count} vehicles from FleetYards public hangar for {username}")]
    static partial void LogFetchedVehicles(ILogger logger, string username, int count);
}
