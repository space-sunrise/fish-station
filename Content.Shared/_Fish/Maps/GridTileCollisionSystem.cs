using System.Collections.Generic;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Fish.Maps;

/// <summary>
/// Makes tiles with <see cref="ITileDefinition.EnableGridCollision"/> disabled passable for grids/shuttles
/// unless a dense anchored blocker occupies the cell. Regenerates chunk fixtures when blockers change.
/// </summary>
public sealed class GridTileCollisionSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitions = default!;

    /// <summary>
    /// Layers that represent full-tile dense blockers for shuttle/grid collision baking
    /// (walls via Impassable, airlocks via HighImpassable). Excludes table MidImpassable-only props.
    /// </summary>
    private const CollisionGroup DenseBlockerMask =
        CollisionGroup.Impassable | CollisionGroup.HighImpassable;

    /// <summary>
    /// Deferred chunk regenerations so deleted/unanchored blockers leave the lookup first.
    /// </summary>
    private readonly HashSet<(EntityUid Grid, Vector2i Tile)> _pendingRegens = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapGridComponent, GridChunkCollisionFillEvent>(OnChunkCollisionFill);
        SubscribeLocalEvent<AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<ReAnchorEvent>(OnReAnchor);
        SubscribeLocalEvent<CollisionChangeEvent>(OnCollisionChanged);
    }

    public override void Update(float frameTime)
    {
        if (_pendingRegens.Count == 0)
            return;

        foreach (var (grid, tile) in _pendingRegens)
        {
            if (!Exists(grid))
                continue;

            _map.RegenerateChunkCollision(grid, tile);
        }

        _pendingRegens.Clear();
    }

    private void OnChunkCollisionFill(Entity<MapGridComponent> ent, ref GridChunkCollisionFillEvent args)
    {
        var gridUid = ent.Owner;
        var chunkIndices = args.ChunkIndices;
        var chunkSize = args.ChunkSize;

        args.IsFilled = (x, y) =>
        {
            var gridIndices = new Vector2i(
                chunkIndices.X * chunkSize + x,
                chunkIndices.Y * chunkSize + y);
            var tile = _map.GetTileRef(gridUid, ent.Comp, gridIndices).Tile;
            if (tile.IsEmpty)
                return false;

            var def = _tileDefinitions[tile.TypeId];
            if (def.EnableGridCollision)
                return true;

            return _turf.IsTileBlocked(gridUid, gridIndices, DenseBlockerMask, grid: ent.Comp);
        };
    }

    private void OnAnchorStateChanged(ref AnchorStateChangedEvent args)
    {
        TryQueueInvalidateAtEntity(args.Entity);
    }

    private void OnReAnchor(ref ReAnchorEvent args)
    {
        QueueInvalidate(args.OldGrid, args.TilePos);
        QueueInvalidate(args.Grid, args.TilePos);
    }

    private void OnCollisionChanged(ref CollisionChangeEvent args)
    {
        TryQueueInvalidateAtEntity(args.BodyUid);
    }

    private void TryQueueInvalidateAtEntity(EntityUid uid)
    {
        if (!TryComp(uid, out TransformComponent? xform))
            return;

        // При удалении Anchored уже false, но GridUid/координаты ещё валидны.
        if (xform.GridUid is not { } gridUid)
            return;

        if (gridUid == uid)
            return;

        if (!TryComp(gridUid, out MapGridComponent? grid))
            return;

        var tile = _map.GetTileRef(gridUid, grid, xform.Coordinates);
        if (tile.Tile.IsEmpty)
            return;

        if (_tileDefinitions[tile.Tile.TypeId].EnableGridCollision)
            return;

        QueueInvalidate(gridUid, tile.GridIndices);
    }

    private void QueueInvalidate(EntityUid gridUid, Vector2i tileIndices)
    {
        _pendingRegens.Add((gridUid, tileIndices));
    }
}
