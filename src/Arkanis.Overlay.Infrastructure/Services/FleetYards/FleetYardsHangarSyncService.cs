namespace Arkanis.Overlay.Infrastructure.Services.FleetYards;

using Abstractions;
using Common.Abstractions;
using Common.Options;
using Domain.Abstractions.Services;
using Domain.Models.Game;
using Domain.Models.Inventory;
using External;
using Microsoft.Extensions.Logging;

public partial class FleetYardsHangarSyncService(
    FleetYardsAccountContext fleetYardsAccountContext,
    CitizenIdAccountContext citizenIdAccountContext,
    IFleetYardsHangarProvider hangarProvider,
    IFleetYardsPublicHangarProvider publicHangarProvider,
    IInventoryManager inventoryManager,
    IGameEntityRepository<GameSpaceShip> spaceShipRepository,
    IUserPreferencesManager userPreferencesManager,
    ILogger<FleetYardsHangarSyncService> logger
) : ISelfInitializable
{
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        userPreferencesManager.ApplyPreferences += OnApplyPreferences;
        // Delayed initial sync: account contexts initialize in parallel, give them time to validate credentials
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await SyncAsync(cancellationToken);
        }, cancellationToken);
        return Task.CompletedTask;
    }

    private void OnApplyPreferences(object? _, UserPreferences __) =>
        _ = Task.Run(async () =>
        {
            // Brief delay so account contexts process the same event before we read their state
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            await SyncAsync(CancellationToken.None);
        });

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            await SyncCoreAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogSyncFailed(logger, ex);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task SyncCoreAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<FleetYardsHangarVehicle> remoteVehicles;

        if (fleetYardsAccountContext.IsAuthenticated)
        {
            remoteVehicles = await hangarProvider.GetHangarAsync(cancellationToken);
        }
        else if (citizenIdAccountContext.RsiIdentity.IsAuthenticated
                 && citizenIdAccountContext.RsiIdentity.Name is { Length: > 0 } rsiHandle)
        {
            remoteVehicles = await publicHangarProvider.GetHangarAsync(rsiHandle, cancellationToken);
        }
        else
        {
            await RemoveFleetYardsEntriesAsync(cancellationToken);
            return;
        }

        var vehicleLookup = await BuildVehicleLookupAsync(cancellationToken);

        var allEntries = await inventoryManager.GetAllEntriesAsync(cancellationToken);
        var existingEntries = allEntries
            .OfType<HangarInventoryEntry>()
            .Where(e => e.FleetYardsVehicleId != null)
            .ToDictionary(e => e.FleetYardsVehicleId!);

        var processedIds = new HashSet<string>();

        foreach (var vehicle in remoteVehicles)
        {
            if (vehicle.Id is not { Length: > 0 } vehicleId)
            {
                continue;
            }

            var gameVehicle = ResolveGameVehicle(vehicle, vehicleLookup);
            if (gameVehicle is null)
            {
                LogVehicleNotMatched(logger, vehicle.ModelName, vehicle.ModelSlug);
                continue;
            }

            processedIds.Add(vehicleId);

            if (existingEntries.TryGetValue(vehicleId, out var existing))
            {
                var changed = false;
                if (existing.NameTag != vehicle.CustomName) { existing.NameTag = vehicle.CustomName; changed = true; }
                if (existing.IsPledged != vehicle.Purchased) { existing.IsPledged = vehicle.Purchased; changed = true; }
                if (existing.IsLoaner != vehicle.Loaner) { existing.IsLoaner = vehicle.Loaner; changed = true; }
                if (changed)
                {
                    await inventoryManager.AddOrUpdateEntryAsync(existing, cancellationToken);
                }
            }
            else
            {
                var entry = InventoryEntry.Create(gameVehicle);
                entry.FleetYardsVehicleId = vehicleId;
                entry.NameTag = vehicle.CustomName;
                entry.IsPledged = vehicle.Purchased;
                entry.IsLoaner = vehicle.Loaner;
                await inventoryManager.AddOrUpdateEntryAsync(entry, cancellationToken);
            }
        }

        foreach (var (_, entry) in existingEntries.Where(kv => !processedIds.Contains(kv.Key)))
        {
            await inventoryManager.DeleteEntryAsync(entry.Id, cancellationToken);
        }

        LogSyncCompleted(logger, remoteVehicles.Count, processedIds.Count);
    }

    private async Task RemoveFleetYardsEntriesAsync(CancellationToken cancellationToken)
    {
        var all = await inventoryManager.GetAllEntriesAsync(cancellationToken);
        foreach (var entry in all.OfType<HangarInventoryEntry>().Where(e => e.FleetYardsVehicleId != null))
        {
            await inventoryManager.DeleteEntryAsync(entry.Id, cancellationToken);
        }
    }

    private async Task<Dictionary<string, GameVehicle>> BuildVehicleLookupAsync(CancellationToken cancellationToken)
    {
        var lookup = new Dictionary<string, GameVehicle>(StringComparer.OrdinalIgnoreCase);
        await foreach (var ship in spaceShipRepository.GetAllAsync(cancellationToken))
        {
            var mainName = ship.Name.MainContent;
            RegisterName(lookup, mainName.FullName, ship);
            if (mainName is GameEntityName.NameWithShortVariant nsv)
            {
                RegisterName(lookup, nsv.ShortName, ship);
            }
        }

        return lookup;
    }

    private static void RegisterName(Dictionary<string, GameVehicle> lookup, string name, GameVehicle vehicle)
    {
        var key = NormalizeName(name);
        lookup.TryAdd(key, vehicle);
    }

    private static GameVehicle? ResolveGameVehicle(FleetYardsHangarVehicle vehicle, Dictionary<string, GameVehicle> lookup)
    {
        // Try direct name match
        if (vehicle.ModelName is { Length: > 0 } modelName
            && lookup.TryGetValue(NormalizeName(modelName), out var byName))
        {
            return byName;
        }

        // Try slug: "aurora-mr" → "aurora mr"
        if (vehicle.ModelSlug is { Length: > 0 } slug
            && lookup.TryGetValue(NormalizeName(slug.Replace('-', ' ')), out var bySlug))
        {
            return bySlug;
        }

        return null;
    }

    private static string NormalizeName(string name) =>
        name.Trim().ToLowerInvariant();

    [LoggerMessage(LogLevel.Warning, "FleetYards hangar sync: could not match '{ModelName}' (slug: {ModelSlug}) to any game vehicle")]
    static partial void LogVehicleNotMatched(ILogger logger, string modelName, string modelSlug);

    [LoggerMessage(LogLevel.Information, "FleetYards hangar sync completed: {RemoteCount} remote vehicles, {MatchedCount} matched")]
    static partial void LogSyncCompleted(ILogger logger, int remoteCount, int matchedCount);

    [LoggerMessage(LogLevel.Warning, "FleetYards hangar sync failed")]
    static partial void LogSyncFailed(ILogger logger, Exception ex);
}
