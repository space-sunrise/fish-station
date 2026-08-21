using Content.Server.Administration.Systems;
using Content.Server.GameTicking;
using Content.Shared._Fish.Achievements;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Shared.Player;

namespace Content.Server._Fish.Achievements;

/// <summary>
/// Единый gate: обычный раунд / ghost / visiting / Admin Arena.
/// </summary>
public sealed class AchievementGameplayGateSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AdminTestArenaSystem _adminArena = default!;

    /// <summary>
    /// Можно ли засчитывать gameplay-достижение этому session.
    /// </summary>
    public bool CanEarnGameplay(
        ICommonSession session,
        AchievementPrototype? proto = null,
        bool requireInRound = true)
    {
        if (requireInRound && _ticker.RunLevel != GameRunLevel.InRound)
            return false;

        if (session.AttachedEntity is not { Valid: true } ent)
            return false;

        if (HasComp<GhostComponent>(ent))
            return false;

        if (!_mind.TryGetMind(ent, out _, out var mind) || mind.UserId != session.UserId)
            return false;

        if (mind.IsVisitingEntity)
            return false;

        if (proto is not { AllowAdminArena: true } && IsInAdminTestArena(ent))
            return false;

        return true;
    }

    public bool IsInAdminTestArena(EntityUid ent)
    {
        var mapUid = Transform(ent).MapUid;
        if (mapUid == null)
            return false;

        foreach (var arenaMap in _adminArena.ArenaMap.Values)
        {
            if (arenaMap == mapUid)
                return true;
        }

        if (TryComp(mapUid.Value, out MetaDataComponent? meta) &&
            meta.EntityName.StartsWith("ATAM-", StringComparison.Ordinal))
            return true;

        return false;
    }
}
