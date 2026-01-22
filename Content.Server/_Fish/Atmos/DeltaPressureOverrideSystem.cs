using Content.Shared._Fish.FishCCVars;
using Content.Shared.Atmos.Components;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.EntitySystems;

public enum WindowID
{
    // Reinforced plasma windows
    PlasmaReinforcedWindowDirectional,
    ReinforcedPlasmaWindow,
    ReinforcedPlasmaWindowDiagonal,

    // Reinforced windows
    WindowReinforcedDirectional,
    ReinforcedWindow,
    ReinforcedWindowDiagonal
}

// This system allows to override values from `deltapressure.yml` without modifying prototypes
public partial class DeltaPressureSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _deltaPReinforcedPlasma;

    private float _deltaPReinforced;

    partial void AfterInit()
    {
        _cfg.OnValueChanged(FishCCVars.DeltaPReinforcedPlasma, (value) => _deltaPReinforcedPlasma = value, true);
        _cfg.OnValueChanged(FishCCVars.DeltaPReinforced, (value) => _deltaPReinforced = value, true);

        SubscribeLocalEvent<DeltaPressureComponent, ComponentStartup>(OnDeltaPressureComponentStartup);
    }

    private void OnDeltaPressureComponentStartup(Entity<DeltaPressureComponent> ent, ref ComponentStartup args)
    {
        Override(ent, ref args);
    }

    private bool Override(Entity<DeltaPressureComponent> ent, ref ComponentStartup args)
    {
        TryPrototype(ent, out var proto);

        if (proto is null || !Enum.TryParse<WindowID>(proto.ID, out var windowProtoID))
            return false;

        return windowProtoID switch
        {
            WindowID.PlasmaReinforcedWindowDirectional
                or WindowID.ReinforcedPlasmaWindow
                or WindowID.ReinforcedPlasmaWindowDiagonal => SetMinPressure(ent, _deltaPReinforcedPlasma),
            WindowID.WindowReinforcedDirectional
                or WindowID.ReinforcedWindow
                or WindowID.ReinforcedWindowDiagonal => SetMinPressure(ent, _deltaPReinforced),
            _ => false,
        };
    }

    private static bool SetMinPressure(Entity<DeltaPressureComponent> ent, float pressure)
    {
        ent.Comp.MinPressure = pressure;
        ent.Comp.MinPressureDelta = pressure;
        return true;
    }
}
