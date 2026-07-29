using Content.Server.Medical;
using Content.Server.Medical.Components;
using Content.Server.Store.Systems;
using Content.Shared._Fish.PAI;
using Content.Shared._Sunrise.InnateItem;
using Content.Shared.Actions;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.Doors.Components;
using Content.Shared.CriminalRecords;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.MedicalScanner;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Fish.PAI;

public sealed partial class SyndicatePaiSystem : SharedSyndicatePaiSystem
{
    [Dependency] private readonly SharedPopupSystem _serverPopup = default!;
    [Dependency] private readonly SharedActionsSystem _serverActions = default!;
    [Dependency] private readonly SharedContainerSystem _serverContainer = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzer = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _serverUi = default!;

    private const int MaxDirectiveLength = 300;

    private static readonly EntProtoId InnateInstantActionProto = "InnateInstantActionAction";
    private static readonly EntProtoId InnateEntityTargetActionProto = "InnateEntityTargetAction";

    private static readonly HashSet<string> ModuleListingIds =
    [
        "SyndicatePaiMedical",
        "SyndicatePaiAutoDispenser",
        "SyndicatePaiDoorHack",
        "SyndicatePaiSecRecords",
        "SyndicatePaiDisguise",
        "SyndicatePaiAtmosSensor",
        "SyndicatePaiMassScanner",
        "SyndicatePaiMidi",
        "SyndicatePaiStationMap",
    ];

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SyndicatePaiComponent>(SyndicatePaiUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUiOpened);
                subs.Event<SyndicatePaiInjectCarrierMessage>(OnInjectMessage);
                subs.Event<SyndicatePaiSelectReagentMessage>(OnSelectMessage);
                subs.Event<SyndicatePaiSetTransferAmountMessage>(OnSetTransferAmount);
                subs.Event<SyndicatePaiSetAutoEnabledMessage>(OnSetAutoEnabled);
                subs.Event<SyndicatePaiSetAutoThresholdMessage>(OnSetAutoThreshold);
                subs.Event<SyndicatePaiSetDirectiveMessage>(OnSetDirectiveMessage);
                subs.Event<SyndicatePaiImprintMasterMessage>(OnImprintMessage);
            });

        SubscribeLocalEvent<SyndicatePaiComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SyndicatePaiComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<SyndicatePaiComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<SyndicatePaiComponent, SyndicatePaiScanOwnerEvent>(OnScanOwner);
        SubscribeLocalEvent<SyndicatePaiComponent, SyndicatePaiDoorHackEvent>(OnDoorHack);
        SubscribeLocalEvent<SyndicatePaiComponent, SyndicatePaiOpenSecRecordsEvent>(OnOpenSecRecords);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBuyFinished);
    }

    private void OnUiOpened(Entity<SyndicatePaiComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnInjectMessage(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiInjectCarrierMessage args)
    {
        // Только владелец (мастер); без клика по спрайту
        TryInjectOwner(ent, args.Actor);
    }

    private void OnSetTransferAmount(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiSetTransferAmountMessage args)
    {
        TrySetTransferAmount(ent, args.Actor, FixedPoint2.New(args.Amount));
    }

    private void OnSetDirectiveMessage(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiSetDirectiveMessage args)
    {
        if (!CanEditDirectives(ent, args.Actor))
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-directive-denied"), ent.Owner, args.Actor);
            return;
        }

        var text = args.Directive;
        if (text.Length > MaxDirectiveLength)
            text = text[..MaxDirectiveLength];

        SetSupplementalDirective(ent, text);
        _serverPopup.PopupEntity(
            Loc.GetString("syndicate-pai-directive-updated", ("directive", text)),
            args.Actor,
            args.Actor);
    }

    private void OnImprintMessage(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiImprintMasterMessage args)
    {
        if (!TryGetCarrier(ent.Owner, out var carrier) || carrier == null)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-no-carrier"), ent.Owner, args.Actor);
            return;
        }

        // Импринт только носителем или самим пИИ
        if (args.Actor != carrier && args.Actor != ent.Owner)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-imprint-denied"), ent.Owner, args.Actor);
            return;
        }

        TryImprintMaster(ent, carrier.Value, args.Actor);
    }

    private void OnGetVerbs(Entity<SyndicatePaiComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        // Импринт только текущему носителю, не любому прохожему
        if (TryGetCarrier(ent.Owner, out var carrier) && carrier == user && user != ent.Comp.Master)
        {
            AlternativeVerb imprint = new()
            {
                Text = Loc.GetString("syndicate-pai-verb-imprint"),
                Act = () => TryImprintMaster(ent, user, user),
                Priority = 2,
            };
            args.Verbs.Add(imprint);
        }

        if (CanEditDirectives(ent, user))
        {
            AlternativeVerb clear = new()
            {
                Text = Loc.GetString("syndicate-pai-verb-clear-directive"),
                Act = () => SetSupplementalDirective(ent, null),
                Priority = 1,
            };
            args.Verbs.Add(clear);
        }
    }

    private void OnMindRemoved(Entity<SyndicatePaiComponent> ent, ref MindRemovedMessage args)
    {
        ent.Comp.Master = null;
        ent.Comp.SupplementalDirective = null;
        Dirty(ent);
    }

    private void OnUseInHand(Entity<SyndicatePaiComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Master != null)
            return;

        if (args.User == ent.Owner)
            return;

        TryImprintMaster(ent, args.User, args.User);
    }

    private void OnScanOwner(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiScanOwnerEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!ent.Comp.MedicalUnlocked)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-module-locked"), ent.Owner, args.Performer);
            return;
        }

        if (!TryGetOwnerTarget(ent, out var owner) || owner == null)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-no-owner"), ent.Owner, args.Performer);
            return;
        }

        if (!IsHeldByOwner(ent, owner.Value))
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-not-in-owner-inventory"), ent.Owner, args.Performer);
            return;
        }

        if (!TryGetAnalyzer(ent, out var analyzer) || analyzer == null)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-no-analyzer"), ent.Owner, args.Performer);
            return;
        }

        if (!TryComp<HealthAnalyzerComponent>(analyzer.Value, out var analyzerComp))
            return;

        // Моментальный скан владельца без do-after
        analyzerComp.ScannedEntity = owner.Value;
        _itemToggle.TryActivate(analyzer.Value);
        _serverUi.OpenUi(analyzer.Value, HealthAnalyzerUiKey.Key, args.Performer);
        _healthAnalyzer.UpdateScannedUser(analyzer.Value, owner.Value, true);
    }

    private void OnDoorHack(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiDoorHackEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!ent.Comp.DoorHackUnlocked)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-module-locked"), ent.Owner, args.Performer);
            return;
        }

        if (!TryGetAccessBreaker(ent, out var breaker) || breaker == null)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-door-hack-missing"), ent.Owner, args.Performer);
            return;
        }

        if (_gameTiming.CurTime < ent.Comp.NextDoorHackTime)
        {
            var remaining = ent.Comp.NextDoorHackTime - _gameTiming.CurTime;
            _serverPopup.PopupEntity(
                Loc.GetString("syndicate-pai-door-hack-cooldown", ("seconds", (int)remaining.TotalSeconds)),
                ent.Owner,
                args.Performer);
            return;
        }

        if (!TryGetOwnerTarget(ent, out var owner) || owner == null)
        {
            // Fallback: взлом вокруг носителя, если мастер ещё не задан
            if (!TryGetCarrier(ent.Owner, out owner) || owner == null)
            {
                _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-no-owner"), ent.Owner, args.Performer);
                return;
            }
        }

        var origin = _transform.GetMapCoordinates(owner.Value);
        var doors = _lookup.GetEntitiesInRange<DoorComponent>(origin, ent.Comp.DoorHackRadius);
        var hacked = 0;

        foreach (var door in doors)
        {
            if (_emag.TryEmagEffect(breaker.Value, args.Performer, door))
                hacked++;
        }

        if (hacked <= 0)
        {
            // Handled=false — иначе useDelay экшена повесится даже без взлома
            args.Handled = false;
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-door-hack-none"), ent.Owner, args.Performer);
            return;
        }

        ent.Comp.NextDoorHackTime = _gameTiming.CurTime + ent.Comp.DoorHackCooldown;
        Dirty(ent);
        _serverPopup.PopupEntity(
            Loc.GetString("syndicate-pai-door-hack-success", ("count", hacked)),
            ent.Owner,
            args.Performer);
    }

    private void OnOpenSecRecords(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiOpenSecRecordsEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!ent.Comp.SecRecordsUnlocked)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-module-locked"), ent.Owner, args.Performer);
            return;
        }

        EnsureComp<CriminalRecordsConsoleComponent>(ent.Owner);
        _serverUi.TryToggleUi(ent.Owner, CriminalRecordsConsoleKey.Key, args.Performer);
    }

    private void OnStoreBuyFinished(ref StoreBuyFinishedEvent args)
    {
        if (!TryComp<SyndicatePaiComponent>(args.StoreUid, out var pai))
            return;

        var listingId = args.PurchasedItem.ID;
        if (!ModuleListingIds.Contains(listingId))
            return;

        var ent = (args.StoreUid, pai);
        switch (listingId)
        {
            case "SyndicatePaiMedical":
                UnlockMedical(ent);
                break;
            case "SyndicatePaiAutoDispenser":
                UnlockAutoDispenser(ent);
                break;
            case "SyndicatePaiDoorHack":
                UnlockDoorHack(ent);
                break;
            case "SyndicatePaiSecRecords":
                UnlockSecRecords(ent);
                break;
            case "SyndicatePaiDisguise":
                UnlockDisguise(ent);
                break;
            case "SyndicatePaiAtmosSensor":
                GrantInnateTool(ent, "GasAnalyzerPai", entityTarget: false, grantAction: true);
                break;
            // Programs use productAction in catalog — nothing else here
        }
    }

    private void UnlockMedical(Entity<SyndicatePaiComponent> ent)
    {
        if (ent.Comp.MedicalUnlocked)
            return;

        ent.Comp.MedicalUnlocked = true;
        // Инструменты только во внутреннем контейнере — без innate-кликов по чужим
        GrantInnateTool(ent, ent.Comp.HypoPrototype, entityTarget: true, grantAction: false);
        GrantInnateTool(ent, ent.Comp.AnalyzerPrototype, entityTarget: true, grantAction: false);

        if (TryGetManualHypo(ent, out var manualHypo) && manualHypo != null)
            ClearHypoReservoir(manualHypo.Value);

        // OpenMedical выдаётся через productAction листинга; ScanOwner — отдельно
        _serverActions.AddAction(ent.Owner, ref ent.Comp.ScanOwnerActionEntity, ent.Comp.ScanOwnerAction);
        Dirty(ent);
        _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-module-medical-unlocked"), ent.Owner, ent.Owner);
    }

    private void UnlockDoorHack(Entity<SyndicatePaiComponent> ent)
    {
        if (ent.Comp.DoorHackUnlocked)
            return;

        ent.Comp.DoorHackUnlocked = true;
        // Unlimited AccessBreaker — существующая система взлома доступа
        GrantInnateTool(ent, "AccessBreakerUnlimited", entityTarget: true, grantAction: false);
        Dirty(ent);
        _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-module-door-unlocked"), ent.Owner, ent.Owner);
    }

    private void UnlockSecRecords(Entity<SyndicatePaiComponent> ent)
    {
        if (ent.Comp.SecRecordsUnlocked)
            return;

        ent.Comp.SecRecordsUnlocked = true;
        EnsureComp<CriminalRecordsConsoleComponent>(ent.Owner);
        Dirty(ent);
        _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-module-sec-unlocked"), ent.Owner, ent.Owner);
    }

    private void UnlockDisguise(Entity<SyndicatePaiComponent> ent)
    {
        if (ent.Comp.Disguised)
            return;

        // Только спрайт (клиент); имя/описание пИИ не меняем
        ent.Comp.Disguised = true;
        Dirty(ent);

        _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-module-disguise-unlocked"), ent.Owner, ent.Owner);
    }

    private bool TryGetAccessBreaker(Entity<SyndicatePaiComponent> ent, out EntityUid? breaker)
    {
        breaker = null;
        if (!_serverContainer.TryGetContainer(ent.Owner, SyndicatePaiComponent.InnateItemContainerId, out var container))
            return false;

        foreach (var contained in container.ContainedEntities)
        {
            if (!TryComp<EmagComponent>(contained, out var emag))
                continue;

            if (!_emag.CompareFlag(emag.EmagType, EmagType.Access))
                continue;

            breaker = contained;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Выдаёт инструмент в innate_items и создаёт действие (как InnateItemSystem).
    /// </summary>
    private void GrantInnateTool(Entity<SyndicatePaiComponent> ent, EntProtoId proto, bool entityTarget, bool grantAction)
    {
        var innate = EnsureComp<InnateItemComponent>(ent.Owner);
        if (!_serverContainer.TryGetContainer(ent.Owner, SyndicatePaiComponent.InnateItemContainerId, out var container))
        {
            var manager = EnsureComp<ContainerManagerComponent>(ent.Owner);
            container = _serverContainer.EnsureContainer<Container>(ent.Owner, SyndicatePaiComponent.InnateItemContainerId, manager);
        }

        // Не дублируем уже выданный прототип
        foreach (var existing in container.ContainedEntities)
        {
            if (MetaData(existing).EntityPrototype?.ID == proto.Id)
                return;
        }

        var spawned = Spawn(proto);
        if (TryComp<Content.Shared.UserInterface.ActivatableUIComponent>(spawned, out var activatableUi))
        {
            activatableUi.RequiresComplex = false;
            activatableUi.InHandsOnly = false;
            activatableUi.RequireActiveHand = false;
            Dirty(spawned, activatableUi);
        }

        _serverContainer.Insert(spawned, container);

        if (!grantAction)
            return;

        var actionProto = entityTarget ? InnateEntityTargetActionProto : InnateInstantActionProto;
        var action = Spawn(actionProto);

        _serverActions.SetIcon(action, new SpriteSpecifier.EntityPrototype(proto));
        if (entityTarget)
            _serverActions.SetEvent(action, new InnateEntityTargetActionEvent(spawned));
        else
            _serverActions.SetEvent(action, new InnateInstantActionEvent(spawned));

        _metadata.SetEntityName(action, MetaData(spawned).EntityName);
        _metadata.SetEntityDescription(action, MetaData(spawned).EntityDescription);
        _actionContainer.AddAction(ent.Owner, action);
        _serverActions.AddAction(ent.Owner, action, ent.Owner);
        innate.Actions.Add(action);
        Dirty(ent.Owner, innate);
    }

    private bool CanEditDirectives(Entity<SyndicatePaiComponent> ent, EntityUid user)
    {
        if (ent.Comp.Master == user)
            return true;

        return TryGetCarrier(ent.Owner, out var carrier) && carrier == user;
    }
}
