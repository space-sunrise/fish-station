using Content.Shared._Fish.Mechs.Components;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Robust.Shared.Network;

namespace Content.Shared._Fish.Mechs;

/// <summary>
/// Cabin internals + встроенное радио меха.
/// </summary>
public abstract class SharedMechCabinRadioSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechCabinAtmosComponent, MechToggleInternalsEvent>(OnToggleInternals);
        SubscribeLocalEvent<MechRadioComponent, MechToggleRadioMicEvent>(OnToggleMic);
        SubscribeLocalEvent<MechRadioComponent, MechToggleRadioSpeakerEvent>(OnToggleSpeaker);
        SubscribeLocalEvent<MechRadioComponent, MapInitEvent>(OnRadioMapInit);
    }

    // Actions выдаёт SharedMechChassisAbilitySystem.OnPilotReady (directed uniqueness).

    private void OnToggleInternals(Entity<MechCabinAtmosComponent> ent, ref MechToggleInternalsEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.UseInternalTank = !ent.Comp.UseInternalTank;
        Dirty(ent);
        _actions.SetToggled(ent.Comp.ToggleActionEntity, ent.Comp.UseInternalTank);
        _popup.PopupClient(
            Loc.GetString(ent.Comp.UseInternalTank ? "mech-internals-on" : "mech-internals-off"),
            ent,
            args.Performer);
    }

    private void OnRadioMapInit(Entity<MechRadioComponent> ent, ref MapInitEvent args)
    {
        ApplyRadioState(ent);
    }

    private void OnToggleMic(Entity<MechRadioComponent> ent, ref MechToggleRadioMicEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.MicrophoneEnabled = !ent.Comp.MicrophoneEnabled;
        Dirty(ent);
        _actions.SetToggled(ent.Comp.ToggleMicActionEntity, ent.Comp.MicrophoneEnabled);
        ApplyRadioState(ent);
        _popup.PopupClient(
            Loc.GetString(ent.Comp.MicrophoneEnabled ? "mech-radio-mic-on" : "mech-radio-mic-off"),
            ent,
            args.Performer);
    }

    private void OnToggleSpeaker(Entity<MechRadioComponent> ent, ref MechToggleRadioSpeakerEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.SpeakerEnabled = !ent.Comp.SpeakerEnabled;
        Dirty(ent);
        _actions.SetToggled(ent.Comp.ToggleSpeakerActionEntity, ent.Comp.SpeakerEnabled);
        ApplyRadioState(ent);
        _popup.PopupClient(
            Loc.GetString(ent.Comp.SpeakerEnabled ? "mech-radio-speaker-on" : "mech-radio-speaker-off"),
            ent,
            args.Performer);
    }

    private void ApplyRadioState(Entity<MechRadioComponent> ent)
    {
        if (ent.Comp.MicrophoneEnabled)
        {
            var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(ent);
            transmitter.Channels = new() { SharedChatSystem.CommonChannel };
            Dirty(ent.Owner, transmitter);
        }
        else
        {
            RemCompDeferred<IntrinsicRadioTransmitterComponent>(ent);
        }

        if (ent.Comp.SpeakerEnabled)
        {
            EnsureComp<IntrinsicRadioReceiverComponent>(ent);
            var active = EnsureComp<ActiveRadioComponent>(ent);
            active.Channels = new() { SharedChatSystem.CommonChannel };
            Dirty(ent.Owner, active);
        }
        else
        {
            RemCompDeferred<ActiveRadioComponent>(ent);
        }
    }
}
