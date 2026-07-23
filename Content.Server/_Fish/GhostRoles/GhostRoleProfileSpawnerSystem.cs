using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Station.Systems;
using Content.Shared._Fish.GhostRoles.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NPC.Systems;
using Robust.Server.GameObjects;

namespace Content.Server._Fish.GhostRoles;

/// <summary>
/// Спавнит персонажа игрока из профиля при взятии ghost role через <see cref="GhostRoleProfileSpawnerComponent"/>.
/// </summary>
public sealed class GhostRoleProfileSpawnerSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRoleProfileSpawnerComponent, TakeGhostRoleEvent>(OnTakeRole);
    }

    private void OnTakeRole(Entity<GhostRoleProfileSpawnerComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.TookRole)
            return;

        if (!TryComp(ent, out GhostRoleComponent? ghostRole) || !CanTakeGhost(ent, ghostRole))
        {
            args.TookRole = false;
            return;
        }

        var profile = _gameTicker.GetPlayerProfile(args.Player);
        var coords = Transform(ent).Coordinates;
        var station = _stations.GetOwningStation(ent);

        var mob = _stationSpawning.SpawnPlayerMob(coords, null, profile, station);
        _transform.AttachToGridOrMap(mob);

        if (ent.Comp.Factions.Count > 0)
            _npcFaction.AddFactions((mob, null), ent.Comp.Factions);

        RaiseLocalEvent(mob, new GhostRoleSpawnerUsedEvent(ent, mob));

        if (ghostRole.MakeSentient)
            _mindSystem.MakeSentient(mob, ghostRole.AllowMovement, ghostRole.AllowSpeech);

        EnsureComp<MindContainerComponent>(mob);

        _ghostRole.GhostRoleInternalCreateMindAndTransfer(args.Player, ent, mob, ghostRole);
        _ghostRole.UnregisterGhostRole((ent, ghostRole));
        QueueDel(ent);

        args.TookRole = true;
    }

    private bool CanTakeGhost(EntityUid uid, GhostRoleComponent component)
    {
        return !component.Taken && !MetaData(uid).EntityPaused;
    }
}
