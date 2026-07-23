using Content.Server.EUI;
using Content.Shared._Fish.SecurityHud;
using Content.Shared.CriminalRecords;
using Content.Shared.Eui;
using Content.Shared.StationRecords;

namespace Content.Server._Fish.SecurityHud;

public sealed class SecurityHudCriminalStatusEui : BaseEui
{
    private readonly SecurityHudCriminalStatusSystem _system;
    private readonly EntityUid _target;
    private StationRecordKey _key;
    private CriminalRecord _criminal;
    private GeneralStationRecord _general;

    public SecurityHudCriminalStatusEui(
        SecurityHudCriminalStatusSystem system,
        EntityUid target,
        StationRecordKey key,
        CriminalRecord criminal,
        GeneralStationRecord general)
    {
        _system = system;
        _target = target;
        _key = key;
        _criminal = criminal;
        _general = general;
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return _system.BuildState(_target, _key, _criminal, _general);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SecurityHudCriminalStatusChangeMessage change)
            return;

        if (Player.AttachedEntity is not { } user)
            return;

        if (!_system.CanUse(user, _target, out var key))
        {
            _system.DenyPermission(user);
            Close();
            return;
        }

        _key = key.Value;

        if (!_system.TryChangeStatus(user, _target, _key, change.Status, change.Reason))
            return;

        if (_system.TryGetRecords(_key, out var criminal, out var general))
        {
            _criminal = criminal;
            _general = general;
            StateDirty();
        }
    }

    public override void Closed()
    {
        base.Closed();
        _system.OnEuiClosed(Player);
    }
}
