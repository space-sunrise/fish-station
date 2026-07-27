using Content.Shared._Fish.PAI;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Server._Fish.PAI;

public sealed class SyndicatePaiSystem : SharedSyndicatePaiSystem
{
    [Dependency] private readonly SharedPopupSystem _serverPopup = default!;

    private const int MaxDirectiveLength = 300;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SyndicatePaiComponent>(SyndicatePaiUiKey.Key,
            subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnUiOpened);
                subs.Event<SyndicatePaiInjectCarrierMessage>(OnInjectMessage);
                subs.Event<SyndicatePaiCycleReagentMessage>(OnCycleMessage);
                subs.Event<SyndicatePaiSelectReagentMessage>(OnSelectMessage);
                subs.Event<SyndicatePaiSetDirectiveMessage>(OnSetDirectiveMessage);
                subs.Event<SyndicatePaiImprintMasterMessage>(OnImprintMessage);
            });

        SubscribeLocalEvent<SyndicatePaiComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SyndicatePaiComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<SyndicatePaiComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUiOpened(Entity<SyndicatePaiComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnInjectMessage(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiInjectCarrierMessage args)
    {
        TryInjectCarrier(ent, args.Actor);
    }

    private void OnCycleMessage(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiCycleReagentMessage args)
    {
        TryCycleReagent(ent, args.Actor);
    }

    private void OnSelectMessage(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiSelectReagentMessage args)
    {
        TrySelectReagent(ent, args.Actor, args.Index);
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
            ent.Owner,
            ent.Owner);
    }

    private void OnImprintMessage(Entity<SyndicatePaiComponent> ent, ref SyndicatePaiImprintMasterMessage args)
    {
        if (!TryGetCarrier(ent.Owner, out var carrier) || carrier == null)
        {
            _serverPopup.PopupEntity(Loc.GetString("syndicate-pai-no-carrier"), ent.Owner, args.Actor);
            return;
        }

        // Импринт: носитель себя, либо пИИ запрашивает отпечаток носителя
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

        if (user != ent.Comp.Master)
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
        // Стирание личности сбрасывает привязку мастера и директивы
        ent.Comp.Master = null;
        ent.Comp.SupplementalDirective = null;
        Dirty(ent);
    }

    /// <summary>
    /// Запоминаем активировавшего как мастера, если ещё не задан (аналог set_dna).
    /// </summary>
    private void OnUseInHand(Entity<SyndicatePaiComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Master != null)
            return;

        if (args.User == ent.Owner)
            return;

        TryImprintMaster(ent, args.User, args.User);
    }

    private bool CanEditDirectives(Entity<SyndicatePaiComponent> ent, EntityUid user)
    {
        if (ent.Comp.Master == user)
            return true;

        return TryGetCarrier(ent.Owner, out var carrier) && carrier == user;
    }
}
