using Content.Server._Fish.Objectives.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Objectives;
using Content.Shared._Fish.Objectives.Components;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Fish.Objectives.Systems;

public sealed class AnimalObjectivesSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
    }

    public bool IsEligible(EntityUid uid, AnimalObjectivesRuleComponent? rule = null)
    {
        if (!TryComp<MetaDataComponent>(uid, out var meta) || meta.EntityPrototype is not { } proto)
            return false;

        rule ??= TryGetActiveRule(out var activeRule) ? activeRule : null;
        return rule != null && rule.EligiblePrototypes.Contains(proto.ID);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        EnsureRuleStarted();
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        EnsureRuleStarted();
        AssignObjectivesToExistingMinds();
    }

    private void OnMindAdded(EntityUid uid, MindContainerComponent comp, MindAddedMessage args)
    {
        TryAssignObjectives(uid, args.Mind.Owner, args.Mind.Comp);
    }

    private void AssignObjectivesToExistingMinds()
    {
        var query = EntityQueryEnumerator<MindContainerComponent>();
        while (query.MoveNext(out var uid, out var mindContainer))
        {
            if (mindContainer.Mind is not { } mindId || !TryComp<MindComponent>(mindId, out var mind))
                continue;

            TryAssignObjectives(uid, mindId, mind);
        }
    }

    private void TryAssignObjectives(EntityUid uid, EntityUid mindId, MindComponent mind)
    {
        if (!IsEligible(uid))
            return;

        EnsureRuleStarted();

        if (HasAnimalObjectives(mind))
        {
            RegisterMind(mindId);
            return;
        }

        EnsureComp<AnimalObjectiveTrackerComponent>(uid);
        var assigned = AssignObjectives(mindId, mind);
        RegisterMind(mindId);

        Log.Debug($"Assigned {assigned} animal objective(s) to {ToPrettyString(uid)}");
    }

    private int AssignObjectives(EntityUid mindId, MindComponent mind)
    {
        if (!TryGetActiveRule(out var rule))
            return 0;

        var difficulty = 0f;
        var assignedCount = 0;

        foreach (var set in rule.Sets)
        {
            if (!_random.Prob(set.Prob))
                continue;

            for (var pick = 0; pick < set.MaxPicks && rule.MaxDifficulty > difficulty; pick++)
            {
                var remaining = rule.MaxDifficulty - difficulty;
                if (_objectives.GetRandomObjective(mindId, mind, set.Groups, remaining) is not { } objective)
                    continue;

                _mind.AddObjective(mindId, mind, objective);
                difficulty += Comp<ObjectiveComponent>(objective).Difficulty;
                assignedCount++;
            }
        }

        return assignedCount;
    }

    private void RegisterMind(EntityUid mindId)
    {
        if (!TryGetActiveRule(out var rule) || rule.Minds.Contains(mindId))
            return;

        rule.Minds.Add(mindId);
    }

    private bool TryGetActiveRule(out AnimalObjectivesRuleComponent rule)
    {
        rule = default!;

        var query = EntityQueryEnumerator<AnimalObjectivesRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            rule = comp;
            return true;
        }

        return false;
    }

    private bool HasAnimalObjectives(MindComponent mind)
    {
        foreach (var objective in mind.Objectives)
        {
            if (HasComp<AnimalRoleRequirementComponent>(objective))
                return true;
        }

        return false;
    }

    private void EnsureRuleStarted()
    {
        if (TryGetActiveRule(out _))
            return;

        var rule = _gameTicker.AddGameRule("AnimalObjectives");
        _gameTicker.StartGameRule(rule);
    }
}
