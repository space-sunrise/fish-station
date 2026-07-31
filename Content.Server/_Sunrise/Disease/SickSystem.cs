// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._Sunrise.Disease;
using System.Numerics;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Shared.Humanoid;
using Content.Server.Store.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Server.Chat;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Server.Traits.Assorted;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Traits.Assorted;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Item;
using Content.Shared.Medical;
using Content.Shared.Speech.Muting;
using Content.Shared.Store.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Inventory;
using Content.Shared.Zombies;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Server._Sunrise.Misc.ShiftedAsciiTableAccent;

namespace Content.Server._Sunrise.Disease;

public sealed class SickSystem : SharedSickSystem
{
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IServerEntityManager _entityManager = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    private EntityLookupSystem Lookup => _entityManager.System<EntityLookupSystem>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SickComponent, ComponentShutdown>(OnShut);
        SubscribeLocalEvent<SickComponent, EmoteEvent>(OnEmote,
            before:
            new[] { typeof(VocalSystem), typeof(BodyEmotesSystem) });
        SubscribeLocalEvent<InteractionPopupComponent, InteractionSuccessEvent>(OnInteractionSuccess);
    }
    public void OnShut(EntityUid uid, SickComponent component, ComponentShutdown args)
    {
        Log.Info($"Server: SickComponent on {uid} is shutting down (cured/removed). Owner: {component.owner}.");
        if (!Exists(uid))
            return;
        if (TryComp<AutoEmoteComponent>(uid, out var autoEmoteComponent))
        {
            foreach (var emote in autoEmoteComponent.Emotes.ToArray())
            {
                if (emote.Contains("Infected"))
                {
                    _autoEmote.RemoveEmote(uid, emote);
                }
            }
        }

        // Unconditionally remove all possible symptom components to prevent them staying on cure
        if (HasComp<SleepyComponent>(uid))
            RemComp<SleepyComponent>(uid);
        if (HasComp<MutedComponent>(uid))
            RemComp<MutedComponent>(uid);
        if (HasComp<PermanentBlindnessComponent>(uid))
            RemComp<PermanentBlindnessComponent>(uid);
        if (HasComp<BlurryVisionComponent>(uid))
            RemComp<BlurryVisionComponent>(uid);
        if (HasComp<EyeClosingComponent>(uid))
            RemComp<EyeClosingComponent>(uid);
        if (HasComp<SpeedModifierOnComponent>(uid))
            RemComp<SpeedModifierOnComponent>(uid);
        if (HasComp<MinimumBleedComponent>(uid))
            RemComp<MinimumBleedComponent>(uid);
        if (HasComp<AnomalyAccentComponent>(uid))
            RemComp<AnomalyAccentComponent>(uid);

        if (TryComp<BloodstreamComponent>(uid, out var stream))
        {
            if (component.BeforeInfectedBloodReagents.Volume > 0)
            {
                _bloodstream.ChangeBloodReagents(uid, component.BeforeInfectedBloodReagents);
            }
        }

        if (TryComp<DiseaseRoleComponent>(component.owner, out var diseaseComp))
        {
            diseaseComp.Infected.Remove(uid);
            Dirty(component.owner, diseaseComp);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SickComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (Terminating(uid))
                continue;

            if (TryComp<DiseaseRoleComponent>(component.owner, out var diseaseComp))
            {
                UpdateInfection(uid, component, component.owner, diseaseComp);

                // Начиная с 6 стадии наносится урон от ожогов (Heat) (увеличен до 0.02f)
                if (component.Stady >= 6 && diseaseComp.Lethal > 0)
                {
                    if (_prototypeManager.TryIndex<DamageTypePrototype>("Heat", out var heatDamagePrototype))
                    {
                        var dmg = 0.02f * frameTime * diseaseComp.Lethal;
                        _damageableSystem.TryChangeDamage(uid, new(heatDamagePrototype, dmg), true, origin: uid);
                    }
                }
                // Начиная с 10 стадии дополнительно наносится клеточный урон (Cellular) (увеличен до 0.02f)
                if (component.Stady >= 10 && diseaseComp.Lethal > 0)
                {
                    if (_prototypeManager.TryIndex<DamageTypePrototype>("Cellular", out var cellularDamagePrototype))
                    {
                        var dmg = 0.02f * frameTime * diseaseComp.Lethal;
                        _damageableSystem.TryChangeDamage(uid, new(cellularDamagePrototype, dmg), true, origin: uid);
                    }
                }
                if (!component.Inited)
                {
                    Log.Info($"Server: Initializing infection on host {uid} for disease {component.owner}. Setting Inited = true.");
                    if (TryComp<BloodstreamComponent>(uid, out var stream) &&
                        stream.BloodReferenceSolution is { } reagents)
                    {
                        component.BeforeInfectedBloodReagents = reagents.Clone();
                        var solution = new Solution();

                        foreach (var reagentId in diseaseComp.NewBloodReagent)
                        {
                            solution.AddReagent(reagentId, reagents.Volume);
                        }

                        _bloodstream.ChangeBloodReagents(uid, solution);
                    }

                    RaiseNetworkEvent(new ClientInfectEvent(GetNetEntity(uid), GetNetEntity(component.owner)));
                    diseaseComp.SickOfAllTime++;
                    AddMoney(component.owner, 5);
                    _popupSystem.PopupEntity(Loc.GetString("disease-infect-reward", ("points", 5)),
                        component.owner,
                        component.owner,
                        PopupType.Medium);

                    var state = new DiseaseInfoState(
                        diseaseComp.BaseInfectChance,
                        diseaseComp.CoughSneezeInfectChance,
                        diseaseComp.Lethal,
                        diseaseComp.Shield,
                        diseaseComp.Infected.Count,
                        diseaseComp.SickOfAllTime
                    );
                    _ui.SetUiState(component.owner, DiseaseInfoUiKey.Key, state);
                    component.Inited = true;
                }
                else
                {
                    if (_gameTiming.CurTime >= component.NextStadyAt)
                    {
                        component.Stady++;
                        foreach (var emote in EnsureComp<AutoEmoteComponent>(uid).Emotes.ToArray())
                        {
                            if (emote.Contains("Infected"))
                            {
                                _autoEmote.RemoveEmote(uid, emote);
                            }
                        }

                        // Удаляем компоненты симптомов перед очисткой списка, если они больше не активны
                        if (HasComp<SleepyComponent>(uid))
                            RemComp<SleepyComponent>(uid);
                        if (HasComp<MutedComponent>(uid))
                            RemComp<MutedComponent>(uid);
                        if (HasComp<PermanentBlindnessComponent>(uid))
                            RemComp<PermanentBlindnessComponent>(uid);
                        if (HasComp<BlurryVisionComponent>(uid))
                            RemComp<BlurryVisionComponent>(uid);
                        if (HasComp<EyeClosingComponent>(uid))
                            RemComp<EyeClosingComponent>(uid);
                        if (HasComp<SpeedModifierOnComponent>(uid))
                            RemComp<SpeedModifierOnComponent>(uid);
                        if (HasComp<MinimumBleedComponent>(uid))
                            RemComp<MinimumBleedComponent>(uid);
                        if (HasComp<AnomalyAccentComponent>(uid))
                            RemComp<AnomalyAccentComponent>(uid);

                        component.Symptoms.Clear();
                        component.NextStadyAt = _gameTiming.CurTime + component.StadyDelay;
                    }
                }
            }
        }
    }

    void AddMoney(EntityUid diseaseUid, FixedPoint2 value)
    {
        if (TryComp<DiseaseRoleComponent>(diseaseUid, out var diseaseComp))
        {
            if (TryComp<StoreComponent>(diseaseUid, out var store))
            {
                bool f = _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                    {
                        { diseaseComp.CurrencyPrototype, value }
                    },
                    diseaseUid);
                _store.UpdateUserInterface(diseaseUid, diseaseUid, store);
            }
        }
    }

    private void UpdateInfection(EntityUid uid,
        SickComponent component,
        EntityUid disease,
        DiseaseRoleComponent diseaseComponent)
    {
        foreach ((var key, var symptomData) in diseaseComponent.Symptoms)
        {
            if (!component.Symptoms.Contains(key))
            {
                if (component.Stady >= symptomData.MinLevel && component.Stady <= symptomData.MaxLevel)
                {
                    component.Symptoms.Add(key);
                    EnsureComp<AutoEmoteComponent>(uid);
                    switch (key)
                    {
                        case "Headache":
                            _autoEmote.AddEmote(uid, "InfectedHeadache");
                            break;
                        case "Cough":
                            _autoEmote.AddEmote(uid, "InfectedCough");
                            break;
                        case "Sneeze":
                            _autoEmote.AddEmote(uid, "InfectedSneeze");
                            break;
                        case "Vomit":
                            _autoEmote.AddEmote(uid, "InfectedVomit");
                            break;
                        case "Crying":
                            _autoEmote.AddEmote(uid, "InfectedCrying");
                            break;
                        case "Narcolepsy":
                            if (!HasComp<SleepyComponent>(uid))
                            {
                                var c = AddComp<SleepyComponent>(uid);
                                EntityManager.EntitySysManager.GetEntitySystem<SleepySystem>()
                                    .SetNarcolepsy(uid, new Vector2(60, 80), new Vector2(8, 12), c);
                            }

                            break;
                        case "Muted":
                            EnsureComp<MutedComponent>(uid);
                            break;
                        case "Blindness":
                            EnsureComp<PermanentBlindnessComponent>(uid);
                            break;
                        case "Slowness":
                            EnsureComp<SpeedModifierOnComponent>(uid);
                            break;
                        case "Bleed":
                            EnsureComp<MinimumBleedComponent>(uid);
                            break;
                        case "Aphasia":
                            EnsureComp<AnomalyAccentComponent>(uid);
                            break;
                        case "Insult":
                            _autoEmote.AddEmote(uid, "InfectedInsult");
                            break;
                    }
                }
            }
        }
    }

    private void OnEmote(EntityUid uid, SickComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;
        if (!component.Symptoms.Contains(args.Emote.ID))
            return;
        switch (args.Emote.ID)
        {
            case "Headache":
                _popupSystem.PopupEntity(Loc.GetString("disease-symptom-headache"), uid, uid, PopupType.Small);
                break;
            case "Cough":
                if (_robustRandom.Prob(0.9f))
                {
                    if (TryComp<DiseaseRoleComponent>(component.owner, out var disease))
                    {
                        if (_prototypeManager.TryIndex<DamageTypePrototype>("Piercing", out var damagePrototype))
                        {
                            _damageableSystem.TryChangeDamage(uid,
                                new(damagePrototype, 2.5f * disease.Lethal),
                                true,
                                origin: uid);
                        }

                        var infectorEv = new ZombificationResistanceQueryEvent(SlotFlags.HEAD | SlotFlags.MASK |
                                                                               SlotFlags.OUTERCLOTHING);
                        RaiseLocalEvent(uid, infectorEv);

                        foreach (var entity in Lookup.GetEntitiesInRange(uid, 1.0f))
                        {
                            if (HasComp<HumanoidAppearanceComponent>(entity) && !HasComp<SickComponent>(entity) &&
                                !HasComp<DiseaseImmuneComponent>(entity))
                            {
                                var ev = new ZombificationResistanceQueryEvent(SlotFlags.HEAD | SlotFlags.MASK |
                                                                               SlotFlags.OUTERCLOTHING);
                                RaiseLocalEvent(entity, ev);

                                var prob = disease.CoughSneezeInfectChance * ev.TotalCoefficient * infectorEv.TotalCoefficient;
                                OnInfected(entity, component.owner, prob);
                            }
                        }
                    }
                }

                break;
            case "Sneeze":
                if (_robustRandom.Prob(0.9f))
                {
                    if (TryComp<DiseaseRoleComponent>(component.owner, out var disease))
                    {
                        if (_prototypeManager.TryIndex<DamageTypePrototype>("Piercing", out var damagePrototype))
                        {
                            _damageableSystem.TryChangeDamage(uid,
                                new(damagePrototype, 2.5f * disease.Lethal),
                                true,
                                origin: uid);
                        }

                        var infectorEv = new ZombificationResistanceQueryEvent(SlotFlags.HEAD | SlotFlags.MASK |
                                                                               SlotFlags.OUTERCLOTHING);
                        RaiseLocalEvent(uid, infectorEv);

                        foreach (var entity in Lookup.GetEntitiesInRange(uid, 1.5f))
                        {
                            if (HasComp<HumanoidAppearanceComponent>(entity) && !HasComp<SickComponent>(entity) &&
                                !HasComp<DiseaseImmuneComponent>(entity))
                            {
                                var ev = new ZombificationResistanceQueryEvent(SlotFlags.HEAD | SlotFlags.MASK |
                                                                               SlotFlags.OUTERCLOTHING);
                                RaiseLocalEvent(entity, ev);

                                var prob = disease.CoughSneezeInfectChance * ev.TotalCoefficient * infectorEv.TotalCoefficient;
                                OnInfected(entity, component.owner, prob);
                            }
                        }
                    }
                }

                break;
            case "Vomit":
                if (_robustRandom.Prob(0.4f))
                {
                    _vomitSystem.Vomit(uid, -30, -20);
                }

                break;
            case "Insult":
                if (TryComp<DiseaseRoleComponent>(component.owner, out var dis))
                {
                    _stun.TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(5));
                    if (_prototypeManager.TryIndex<DamageTypePrototype>("Shock", out var damagePrototype))
                    {
                        _damageableSystem.TryChangeDamage(uid,
                            new(damagePrototype, 3.5f * dis.Lethal),
                            true,
                            origin: uid);
                    }
                }

                break;
        }
    }

    private void OnInteractionSuccess(EntityUid uid, InteractionPopupComponent popup, ref InteractionSuccessEvent args)
    {
        if (popup.InteractSuccessString != "hugging-success-generic")
            return;

        // uid - цель обнимания (target), args.User - инициатор (initiator)
        
        // 1. Цель (uid) больна, инициатор (args.User) здоров. Цель заражает инициатора.
        if (TryComp<SickComponent>(uid, out var sickTarget))
        {
            InfectOnHug(uid, args.User, sickTarget.owner);
        }

        // 2. Инициатор (args.User) болен, цель (uid) здорова. Инициатор заражает цель.
        if (TryComp<SickComponent>(args.User, out var sickInitiator))
        {
            InfectOnHug(args.User, uid, sickInitiator.owner);
        }
    }

    private void InfectOnHug(EntityUid infector, EntityUid victim, EntityUid diseaseUid)
    {
        if (!HasComp<HumanoidAppearanceComponent>(victim) || HasComp<SickComponent>(victim) || HasComp<DiseaseImmuneComponent>(victim))
            return;

        if (TryComp<DiseaseRoleComponent>(diseaseUid, out var disease))
        {
            var targetEv = new ZombificationResistanceQueryEvent(SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.OUTERCLOTHING);
            RaiseLocalEvent(victim, targetEv);

            var infectorEv = new ZombificationResistanceQueryEvent(SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.OUTERCLOTHING);
            RaiseLocalEvent(infector, infectorEv);

            var prob = disease.BaseInfectChance * targetEv.TotalCoefficient * infectorEv.TotalCoefficient;
            OnInfected(victim, diseaseUid, prob);
        }
    }
}
