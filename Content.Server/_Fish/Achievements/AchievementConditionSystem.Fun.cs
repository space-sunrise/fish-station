using System.Collections.Generic;
using Content.Shared._Fish.Achievements;
using Content.Shared.Maps;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Achievements;

public sealed partial class AchievementConditionSystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    /// <summary>Tile id до deconstruct — ключ (user, grid, indices).</summary>
    private readonly Dictionary<(NetUserId User, NetEntity Grid, Vector2i Tile), string> _pendingTilePry = new();

    partial void InitializeFun()
    {
        SubscribeLocalEvent<ToolTileCompatibleComponent, TileToolDoAfterEvent>(
            OnTileToolDoAfterPrepare,
            before: [typeof(SharedToolSystem)]);
        SubscribeLocalEvent<ToolTileCompatibleComponent, TileToolDoAfterEvent>(
            OnTileToolDoAfterComplete,
            after: [typeof(SharedToolSystem)]);
    }

    partial void ClearFunRoundState()
    {
        _pendingTilePry.Clear();
    }

    private void OnTileToolDoAfterPrepare(Entity<ToolTileCompatibleComponent> ent, ref TileToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<ToolComponent>(ent, out var tool) || !_tools.HasQuality(ent, PryingQuality, tool))
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var gridUid = GetEntity(args.Grid);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tileRef = _maps.GetTileRef(gridUid, grid, args.GridTile);
        if (tileRef.Tile.IsEmpty)
            return;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tileRef.Tile.TypeId];
        if (!IsIntactFloorTile(tileDef))
            return;

        _pendingTilePry[(actor.PlayerSession.UserId, args.Grid, args.GridTile)] = tileDef.ID;
    }

    private void OnTileToolDoAfterComplete(Entity<ToolTileCompatibleComponent> ent, ref TileToolDoAfterEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var key = (actor.PlayerSession.UserId, args.Grid, args.GridTile);
        if (!_pendingTilePry.Remove(key, out _))
            return;

        if (args.Cancelled || !args.Handled)
            return;

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.TilePry,
            new AchievementTriggerContext(
                EntityPrototypeId: GetPrototypeId(ent),
                VerifiedTag: "IntactFloor",
                EventKey: $"tile-pry:{args.Grid}:{args.GridTile}:{actor.PlayerSession.UserId}"));
    }

    /// <summary>Исключает повреждённые/сгоревшие варианты (см. FTL Misclick).</summary>
    private static bool IsIntactFloorTile(ContentTileDefinition tileDef)
    {
        if (!tileDef.CanCrowbar)
            return false;

        var id = tileDef.ID;
        return !id.Contains("Damaged", StringComparison.OrdinalIgnoreCase)
               && !id.Contains("Burnt", StringComparison.OrdinalIgnoreCase);
    }
}
