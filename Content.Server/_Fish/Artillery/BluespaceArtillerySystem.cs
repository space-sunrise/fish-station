using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Fish.Artillery;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server._Fish.Artillery;

public sealed class BluespaceArtillerySystem : SharedBluespaceArtillerySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceArtilleryComponent, NewLinkEvent>(OnArtilleryNewLink);
        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, NewLinkEvent>(OnConsoleNewLink);
        SubscribeLocalEvent<BluespaceArtilleryComponent, LinkAttemptEvent>(OnArtilleryLinkAttempt);
        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, LinkAttemptEvent>(OnConsoleLinkAttempt);
        SubscribeLocalEvent<BluespaceArtilleryComponent, PortDisconnectedEvent>(OnArtilleryPortDisconnected);
        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, PortDisconnectedEvent>(OnConsolePortDisconnected);

        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, BluespaceArtilleryFireMessage>(OnFireMessage);
        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, BluespaceArtillerySetCoordsMessage>(OnSetCoordsMessage);
        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, BluespaceArtillerySetParamsMessage>(OnSetParamsMessage);
        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, BluespaceArtilleryPreviewMessage>(OnPreviewMessage);

        SubscribeLocalEvent<BluespaceArtilleryConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<BluespaceArtilleryComponent, ComponentShutdown>(OnArtilleryShutdown);
    }

    private void OnArtilleryNewLink(EntityUid uid, BluespaceArtilleryComponent comp, ref NewLinkEvent args)
    {
        if (args.SinkPort != comp.LinkingPort || !HasComp<BluespaceArtilleryConsoleComponent>(args.Source))
            return;

        comp.LinkedConsole = args.Source;
        if (TryComp<BluespaceArtilleryConsoleComponent>(args.Source, out var console))
        {
            console.LinkedArtillery = uid;
            UpdateUI(args.Source, console);
        }
    }

    private void OnConsoleNewLink(EntityUid uid, BluespaceArtilleryConsoleComponent comp, ref NewLinkEvent args)
    {
        if (args.SourcePort != comp.LinkingPort || !HasComp<BluespaceArtilleryComponent>(args.Sink))
            return;

        comp.LinkedArtillery = args.Sink;
        if (TryComp<BluespaceArtilleryComponent>(args.Sink, out var artillery))
        {
            artillery.LinkedConsole = uid;
            UpdateUI(uid, comp);
        }
    }

    private void OnArtilleryLinkAttempt(EntityUid uid, BluespaceArtilleryComponent comp, ref LinkAttemptEvent args)
    {
        if (comp.LinkedConsole != null)
            args.Cancel();
    }

    private void OnConsoleLinkAttempt(EntityUid uid, BluespaceArtilleryConsoleComponent comp, ref LinkAttemptEvent args)
    {
        if (comp.LinkedArtillery != null)
            args.Cancel();
    }

    private void OnArtilleryPortDisconnected(EntityUid uid, BluespaceArtilleryComponent comp, ref PortDisconnectedEvent args)
    {
        if (args.Port != comp.LinkingPort || comp.LinkedConsole == null)
            return;

        if (TryComp<BluespaceArtilleryConsoleComponent>(comp.LinkedConsole, out var console))
            console.LinkedArtillery = null;
        comp.LinkedConsole = null;
    }

    private void OnConsolePortDisconnected(EntityUid uid, BluespaceArtilleryConsoleComponent comp, ref PortDisconnectedEvent args)
    {
        if (args.Port != comp.LinkingPort || comp.LinkedArtillery == null)
            return;

        if (TryComp<BluespaceArtilleryComponent>(comp.LinkedArtillery, out var artillery))
            artillery.LinkedConsole = null;
        comp.LinkedArtillery = null;
    }

    private void OnFireMessage(EntityUid uid, BluespaceArtilleryConsoleComponent console, BluespaceArtilleryFireMessage args)
    {
        if (console.LinkedArtillery == null)
            return;

        var artillery = Comp<BluespaceArtilleryComponent>(console.LinkedArtillery.Value);
        if (artillery.IsCharging)
            return;

        if (_timing.CurTime < artillery.NextFireTime)
        {
            _popup.PopupEntity(Loc.GetString("bluespace-artillery-on-cooldown"), uid, uid);
            return;
        }

        var consoleMapCoords = _transform.GetMapCoordinates(uid);
        float dx = consoleMapCoords.X - console.TargetCoordinates.X;
        float dy = consoleMapCoords.Y - console.TargetCoordinates.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (dist > 16384f)
        {
            _popup.PopupEntity(Loc.GetString("bluespace-artillery-out-of-range"), uid, uid);
            return;
        }

        artillery.IsCharging = true;
        UpdateUI(uid, console);

        _audio.PlayGlobal(artillery.SectorChargeSound, uid, AudioParams.Default.WithVolume(-5f));

        var station = _stationSystem.GetOwningStation(uid);
        var message = Loc.GetString(
            "bluespace-artillery-station-announcement",
            ("coords", $"{console.TargetCoordinates.X:F1}, {console.TargetCoordinates.Y:F1}"));
        if (station != null)
            _chat.DispatchStationAnnouncement(station.Value, message, sender: Loc.GetString("bluespace-artillery-cc-sender"));

        _audio.PlayPvs(artillery.ChargeSound, console.LinkedArtillery.Value,
            AudioParams.Default.WithVolume(-10f).WithMaxDistance(30f));

        Timer.Spawn(TimeSpan.FromSeconds(artillery.ChargeDuration), () =>
        {
            OnChargeCompleted(uid, console, artillery);
        });
    }

    private void OnChargeCompleted(EntityUid consoleUid, BluespaceArtilleryConsoleComponent console, BluespaceArtilleryComponent artillery)
    {
        if (console.LinkedArtillery == null)
            return;

        _audio.PlayPvs(artillery.FireSound, console.LinkedArtillery.Value, AudioParams.Default.WithVolume(0f));
        _audio.PlayGlobal(artillery.ImpactSound, consoleUid, AudioParams.Default.WithVolume(0f));

        Timer.Spawn(TimeSpan.FromSeconds(artillery.FlightDuration), () =>
        {
            OnImpact(consoleUid, console, artillery);
        });
    }

    private void OnImpact(EntityUid consoleUid, BluespaceArtilleryConsoleComponent console, BluespaceArtilleryComponent artillery)
    {
        if (console.LinkedArtillery == null)
            return;

        var mapCoords = new MapCoordinates(
            new Vector2(console.TargetCoordinates.X, console.TargetCoordinates.Y),
            Transform(consoleUid).MapID);

        var explosionProto = GetExplosionPrototype(console.ExplosionType);

        _explosion.QueueExplosion(
            mapCoords,
            explosionProto,
            console.TotalIntensity,
            console.Slope,
            console.MaxIntensity,
            null);

        artillery.NextFireTime = _timing.CurTime + TimeSpan.FromSeconds(artillery.CooldownDuration);
        artillery.IsCharging = false;
        UpdateUI(consoleUid, console);
    }

    private string GetExplosionPrototype(string type)
    {
        if (_prototypeManager.TryIndex<ExplosionPrototype>(type, out _))
            return type;

        return ExplosionSystem.DefaultExplosionPrototypeId;
    }

    private void OnSetCoordsMessage(EntityUid uid, BluespaceArtilleryConsoleComponent console, BluespaceArtillerySetCoordsMessage args)
    {
        console.TargetCoordinates = args.Coordinates;
        UpdateUI(uid, console);
    }

    private void OnSetParamsMessage(EntityUid uid, BluespaceArtilleryConsoleComponent console, BluespaceArtillerySetParamsMessage args)
    {
        console.ExplosionType = args.ExplosionType;
        console.TotalIntensity = args.TotalIntensity;
        console.Slope = args.Slope;
        console.MaxIntensity = args.MaxIntensity;
        UpdateUI(uid, console);
    }

    private void OnPreviewMessage(EntityUid uid, BluespaceArtilleryConsoleComponent console, BluespaceArtilleryPreviewMessage args)
    {
        console.PreviewEnabled = args.Enabled;
        UpdateUI(uid, console);
    }

    private void OnConsoleShutdown(EntityUid uid, BluespaceArtilleryConsoleComponent console, ComponentShutdown args)
    {
        if (console.LinkedArtillery != null && TryComp<BluespaceArtilleryComponent>(console.LinkedArtillery.Value, out var artillery))
            artillery.LinkedConsole = null;
    }

    private void OnArtilleryShutdown(EntityUid uid, BluespaceArtilleryComponent artillery, ComponentShutdown args)
    {
        if (artillery.LinkedConsole != null && TryComp<BluespaceArtilleryConsoleComponent>(artillery.LinkedConsole.Value, out var console))
            console.LinkedArtillery = null;
    }

    private void UpdateUI(EntityUid consoleUid, BluespaceArtilleryConsoleComponent console)
    {
        if (console.LinkedArtillery == null)
            return;

        var artillery = Comp<BluespaceArtilleryComponent>(console.LinkedArtillery.Value);
        var isOnCooldown = !artillery.IsCharging && _timing.CurTime < artillery.NextFireTime;

        var state = new BluespaceArtilleryConsoleBoundUserInterfaceState(
            console.TargetCoordinates,
            console.ExplosionType,
            console.TotalIntensity,
            console.Slope,
            console.MaxIntensity,
            console.PreviewEnabled,
            true,
            artillery.IsCharging,
            isOnCooldown
        );

        _ui.SetUiState(consoleUid, BluespaceArtilleryConsoleUiKey.Key, state);
    }
}