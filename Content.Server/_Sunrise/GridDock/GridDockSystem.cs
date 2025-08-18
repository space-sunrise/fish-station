using System.Linq;
using System.Numerics;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;

namespace Content.Server._Sunrise.GridDock;

public sealed class GridDockSystem : EntitySystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly DockingSystem _dockSystem = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;

    // Fish-start
    private Vector2 _nextSpawnOffset;
    private const float GridSeparation = 300f;
    // Fish-end

    public override void Initialize()
    {
        _nextSpawnOffset = new Vector2(500, 500); // Fish-edit
        SubscribeLocalEvent<SpawnGridAndDockToStationComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(EntityUid uid, SpawnGridAndDockToStationComponent component, StationPostInitEvent args)
    {
        if (component.Grids.Count == 0)
            return;

        var ftlMap = _shuttles.EnsureFTLMap();
        var xformMap = Transform(ftlMap);

        if (!TryComp<StationDataComponent>(uid, out var stationData))
            return;

        var target = _station.GetLargestGrid((uid, stationData));

        if (target == null)
        {
            Log.Error($"GridDockSystem: No target grid found for {ToPrettyString(uid)}");
            return;
        }

        var usedGridDocks = new HashSet<EntityUid>();
        foreach (var entry in component.Grids)
        {
            // Fish-start
            var spawnPosition = _nextSpawnOffset;
            _nextSpawnOffset.X += GridSeparation;
            // Fish-end

            if (!_loader.TryLoadGrid(xformMap.MapID,
                    entry.GridPath,
                    out var rootUid,
                    offset: spawnPosition)) // Fish-edit
                continue;

            if (!TryComp<ShuttleComponent>(rootUid.Value.Owner, out var shuttleComp))
                continue;

            var gridDocks = _dockSystem.GetDocks(target.Value);
            var shuttleDocks = _dockSystem.GetDocks(rootUid.Value.Owner);
            var configs = _dockSystem.GetDockingConfigs(rootUid.Value.Owner, target.Value, shuttleDocks, gridDocks, entry.PriorityTag, ignored: false);

            DockingConfig? chosenConfig = null;
            int maxNewDocks = 0;
            foreach (var cfg in configs)
            {
                var newDocks = cfg.Docks.Count(pair => !usedGridDocks.Contains(pair.DockBUid));
                if (newDocks > maxNewDocks)
                {
                    maxNewDocks = newDocks;
                    chosenConfig = cfg;
                }
            }

            if (chosenConfig != null && chosenConfig.Docks.All(pair => !usedGridDocks.Contains(pair.DockBUid)))
            {
                foreach (var pair in chosenConfig.Docks)
                {
                    usedGridDocks.Add(pair.DockBUid);
                }

                _shuttles.FTLToDockСonfig(
                    rootUid.Value.Owner,
                    shuttleComp,
                    chosenConfig,
                    5f,
                    30f,
                    priorityTag: entry.PriorityTag,
                    ignored: false);
            }
            else
            {
                _shuttles.FTLToDock(
                    rootUid.Value.Owner,
                    shuttleComp,
                    target.Value,
                    5f,
                    30f,
                    priorityTag: entry.PriorityTag,
                    ignored: false);
            }
        }
    }
}