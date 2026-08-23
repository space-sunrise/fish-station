using Content.Server.Mining;
using Content.Server.Singularity.Events;
using Content.Shared._Fish.Achievements;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chasm;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Fish.Achievements;

public sealed partial class AchievementConditionSystem
{
    /// <summary>Игроки, уже получившие progress за reagent в bloodstream за раунд.</summary>
    private readonly Dictionary<NetUserId, HashSet<string>> _reagentMetabolized = new();

    partial void InitializeExploration()
    {
        SubscribeLocalEvent<ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<EventHorizonConsumedEntityEvent>(OnEventHorizonConsumed);
        SubscribeLocalEvent<MobStateActionsComponent, CritSuccumbEvent>(OnSuccumb);
        SubscribeLocalEvent<AchievementTrackedComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<SiliconLawUpdaterComponent, EntInsertedIntoContainerMessage>(OnLawBoardInserted);
        SubscribeLocalEvent<SiliconLawProviderComponent, IonStormLawsEvent>(OnIonStormLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, SiliconEmaggedEvent>(OnSiliconEmagged);
        SubscribeLocalEvent<BloodstreamComponent, SolutionContainerChangedEvent>(OnBloodstreamChanged);
        SubscribeLocalEvent<ChasmFallingComponent, ComponentInit>(OnChasmFalling);
        SubscribeLocalEvent<ActorComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnExamined(ExaminedEvent args)
    {
        if (!TryComp<ActorComponent>(args.Examiner, out var actor))
            return;

        string? verifiedTag = null;
        if (HasComp<MeteorComponent>(args.Examined))
            verifiedTag = "Meteor";

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.Examine,
            new AchievementTriggerContext(
                EntityPrototypeId: GetPrototypeId(args.Examined),
                VerifiedTag: verifiedTag,
                EventKey: $"examine:{GetNetEntity(args.Examined)}:{actor.PlayerSession.UserId}"));
    }

    private void OnEventHorizonConsumed(EventHorizonConsumedEntityEvent args)
    {
        if (!TryComp<ActorComponent>(args.Entity, out var actor))
            return;

        // Только singularity/event horizon, не произвольный consume.
        if (!HasComp<SingularityComponent>(args.EventHorizonUid) &&
            !HasComp<EventHorizonComponent>(args.EventHorizonUid))
            return;

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.SingularityConsumed,
            new AchievementTriggerContext(
                EventKey: $"singulo:{GetNetEntity(args.Entity)}"));
    }

    private void OnSuccumb(EntityUid uid, MobStateActionsComponent component, CritSuccumbEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        if (!TryComp<MobStateComponent>(uid, out var mob) || mob.CurrentState != MobState.Critical)
            return;

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.Succumb,
            new AchievementTriggerContext(
                EventKey: $"succumb:{actor.PlayerSession.UserId}:{_timing.CurTick}"));
    }

    private void OnEmote(EntityUid uid, AchievementTrackedComponent tracked, EmoteEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.Emote,
            new AchievementTriggerContext(
                EmotePrototypeId: args.Emote.ID,
                EventKey: $"emote:{args.Emote.ID}:{_timing.CurTick}:{actor.PlayerSession.UserId}"));
    }

    private void OnLawBoardInserted(EntityUid uid, SiliconLawUpdaterComponent updater, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<SiliconLawProviderComponent>(args.Entity, out _))
            return;

        var query = EntityManager.CompRegistryQueryEnumerator(updater.Components);
        while (query.MoveNext(out var siliconUid))
            ContributeAiLawChange(siliconUid, $"upload:{GetNetEntity(uid)}:{_timing.CurTick}");
    }

    private void OnIonStormLaws(EntityUid uid, SiliconLawProviderComponent component, ref IonStormLawsEvent args)
    {
        ContributeAiLawChange(uid, $"ion:{_timing.CurTick}");
    }

    private void OnSiliconEmagged(EntityUid uid, SiliconLawProviderComponent component, ref SiliconEmaggedEvent args)
    {
        ContributeAiLawChange(uid, $"emag:{_timing.CurTick}");
    }

    private void ContributeAiLawChange(EntityUid siliconUid, string suffix)
    {
        if (!TryComp<ActorComponent>(siliconUid, out var actor))
            return;

        string? jobId = null;
        if (_mind.TryGetMind(siliconUid, out _, out var mind) && mind.UserId != null)
        {
            foreach (var roleEnt in mind.MindRoleContainer.ContainedEntities)
            {
                if (!TryComp<MindRoleComponent>(roleEnt, out var role))
                    continue;

                if (role.JobPrototype is { } job)
                {
                    jobId = job.Id;
                    break;
                }
            }
        }

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.AiLawChanges,
            new AchievementTriggerContext(
                JobId: jobId,
                EventKey: $"law:{GetNetEntity(siliconUid)}:{suffix}"));
    }

    private void OnBloodstreamChanged(EntityUid uid, BloodstreamComponent component, SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != BloodstreamComponent.DefaultBloodSolutionName)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        foreach (var reagentQuantity in args.Solution.Contents)
        {
            if (reagentQuantity.Quantity <= FixedPoint2.Zero)
                continue;

            NotifyReagentMetabolize(actor.PlayerSession, reagentQuantity.Reagent.Prototype, uid);
        }
    }

    private void OnChasmFalling(EntityUid uid, ChasmFallingComponent component, ComponentInit args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.ChasmFall,
            new AchievementTriggerContext(
                EventKey: $"chasm:{GetNetEntity(uid)}"));
    }

    private void OnBeingGibbed(EntityUid uid, ActorComponent actor, ref BeingGibbedEvent args)
    {
        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.Gibbed,
            new AchievementTriggerContext(
                EntityPrototypeId: GetPrototypeId(uid),
                EventKey: $"gib:{GetNetEntity(uid)}"));
    }

    /// <summary>Один progress за reagent id на игрока за раунд.</summary>
    internal void NotifyReagentMetabolize(ICommonSession session, string reagentId, EntityUid body)
    {
        if (!_reagentMetabolized.TryGetValue(session.UserId, out var set))
        {
            set = new HashSet<string>();
            _reagentMetabolized[session.UserId] = set;
        }

        if (!set.Add(reagentId))
            return;

        _ = _achievements.ContributeAsync(
            session,
            AchievementConditionKeys.ReagentMetabolize,
            new AchievementTriggerContext(
                ReagentPrototypeId: reagentId,
                EventKey: $"reagent:{reagentId}:{session.UserId}"));
    }

    partial void ClearExplorationRoundState()
    {
        _reagentMetabolized.Clear();
    }
}
