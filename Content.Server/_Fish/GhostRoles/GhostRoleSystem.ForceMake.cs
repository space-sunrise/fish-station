using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Raffles;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// Fish: принудительное превращение сущности в доступную ghost role.
/// </summary>
public sealed partial class GhostRoleSystem
{
    /// <summary>
    /// Принудительно делает сущность доступной ghost role.
    /// Работает без MindContainer, с пустым Mind и на NPC (в т.ч. экспедиции).
    /// </summary>
    /// <param name="uid">Целевая сущность.</param>
    /// <param name="name">Имя роли.</param>
    /// <param name="description">Описание роли.</param>
    /// <param name="rules">Правила роли; null — дефолтные.</param>
    /// <param name="makeSentient">Вызвать <see cref="Content.Shared.Mind.SharedMindSystem.MakeSentient"/>.</param>
    /// <param name="allowMovement">Разрешить движение при MakeSentient / takeover.</param>
    /// <param name="allowSpeech">Разрешить речь при MakeSentient / takeover.</param>
    /// <param name="ejectExistingMind">Если есть живой разум — отсоединить его.</param>
    /// <param name="raffleConfig">Опциональная конфигурация лотереи.</param>
    /// <returns>false, если сущность недоступна или разум занят и eject запрещён.</returns>
    public bool TryForceMakeGhostRole(
        EntityUid uid,
        string name,
        string description,
        string? rules = null,
        bool makeSentient = true,
        bool allowMovement = true,
        bool allowSpeech = true,
        bool ejectExistingMind = true,
        GhostRoleRaffleConfig? raffleConfig = null)
    {
        if (TerminatingOrDeleted(uid))
            return false;

        // Сбрасываем устаревшую ссылку Mind (EntityUid без MindComponent), чтобы HasMind не врал.
        _mindSystem.ClearStaleMind(uid);

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            if (!ejectExistingMind)
                return false;

            // Для игрока создаём observer-госта; для «пустого» разума NPC просто отцепляем.
            _mindSystem.TransferTo(mindId, null, createGhost: mind.UserId != null, mind: mind);
        }

        if (makeSentient)
            _mindSystem.MakeSentient(uid, allowMovement, allowSpeech);

        var ghostRole = EnsureComp<GhostRoleComponent>(uid);
        EnsureComp<GhostTakeoverAvailableComponent>(uid);

        ghostRole.RoleName = name;
        ghostRole.RoleDescription = description;
        ghostRole.RoleRules = rules ?? Loc.GetString("ghost-role-component-default-rules");
        ghostRole.MakeSentient = makeSentient;
        ghostRole.AllowMovement = allowMovement;
        ghostRole.AllowSpeech = allowSpeech;
        ghostRole.RaffleConfig = raffleConfig;

        var wasTaken = ghostRole.Taken;
        ghostRole.Taken = false;

        // Taken-роли снимаются с регистрации в OnMindAdded — вернуть в пул, если нужно.
        if (wasTaken || !_ghostRoles.ContainsValue((uid, ghostRole)))
            RegisterGhostRole((uid, ghostRole));

        return true;
    }
}
