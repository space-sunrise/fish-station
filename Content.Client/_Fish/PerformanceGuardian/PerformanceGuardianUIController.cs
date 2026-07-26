using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Shared._Fish.PerformanceGuardian;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Client._Fish.PerformanceGuardian;

[UsedImplicitly]
public sealed class PerformanceGuardianUIController : UIController,
    IOnStateEntered<GameplayState>,
    IOnStateExited<GameplayState>,
    IOnSystemChanged<PerformanceGuardianSystem>
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _con = default!;

    private PerformanceGuardianWindow? _window;
    private PerformanceGuardianSystem? _system;
    private TimeSpan _nextRequest;
    private float _refreshSeconds = 1.5f;
    private bool _subscribed;

    public override void Initialize()
    {
        base.Initialize();
        _con.RegisterCommand("perfguardian", Loc.GetString("pg-cmd-desc"), Loc.GetString("pg-cmd-help"), OnCommand);
        _con.RegisterCommand("pg", Loc.GetString("pg-cmd-desc"), Loc.GetString("pg-cmd-help"), OnCommand);
        _cfg.OnValueChanged(FishCCVars.PgUiRefreshSeconds, v => _refreshSeconds = Math.Max(0.5f, v), true);
    }

    private void OnCommand(IConsoleShell shell, string argStr, string[] args)
    {
        ToggleWindow();
    }

    public void OnStateEntered(GameplayState state)
    {
    }

    public void OnStateExited(GameplayState state)
    {
        CloseWindow();
    }

    public void OnSystemLoaded(PerformanceGuardianSystem system)
    {
        _system = system;
        system.SnapshotReceived += OnSnapshot;
        system.AlertReceived += OnAlert;
        system.OpenWindowRequested += OpenWindow;
    }

    public void OnSystemUnloaded(PerformanceGuardianSystem system)
    {
        system.SnapshotReceived -= OnSnapshot;
        system.AlertReceived -= OnAlert;
        system.OpenWindowRequested -= OpenWindow;
        CloseWindow();
        _system = null;
    }

    public void ToggleWindow()
    {
        if (_window?.IsOpen == true)
            CloseWindow();
        else
            OpenWindow();
    }

    public void OpenWindow()
    {
        if (!_admin.HasFlag(AdminFlags.Debug))
            return;

        EnsureWindow();
        if (_window == null)
            return;

        if (!_window.IsOpen)
            _window.OpenCentered();
        else
            _window.MoveToFront();

        EnsureSubscribed();
        RequestCurrentTab();
    }

    public void CloseWindow()
    {
        if (_window != null)
        {
            _window.TabNeedsData -= OnTabNeedsData;
            _window.OnClose -= OnWindowClosed;
            if (_window.IsOpen)
                _window.Close();
            _window.Dispose();
            _window = null;
        }

        Unsubscribe();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<PerformanceGuardianWindow>();
        _window.TabNeedsData += OnTabNeedsData;
        _window.OnClose += OnWindowClosed;
    }

    private void OnWindowClosed()
    {
        Unsubscribe();
    }

    private void EnsureSubscribed()
    {
        if (_subscribed || _system == null)
            return;

        _system.Subscribe();
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _system == null)
            return;

        _system.Unsubscribe();
        _subscribed = false;
    }

    private void OnTabNeedsData(PgSnapshotSection section)
    {
        if (_window is not { IsOpen: true })
            return;

        EnsureSubscribed();
        _system?.RequestSnapshot(section);
        _nextRequest = _timing.RealTime + TimeSpan.FromSeconds(_refreshSeconds);
    }

    private void RequestCurrentTab()
    {
        if (_window == null || _system == null)
            return;

        _system.RequestSnapshot(_window.CurrentSection);
        _nextRequest = _timing.RealTime + TimeSpan.FromSeconds(_refreshSeconds);
    }

    private void OnSnapshot(PgSnapshotSection section, PgServerSnapshot snapshot)
    {
        if (_window is not { IsOpen: true, Visible: true })
            return;

        _window.ApplySnapshot(section, snapshot);
    }

    private void OnAlert(PgAlert alert)
    {
        if (_window is not { IsOpen: true })
            return;

        _window.ShowAlert(alert);
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_window is not { IsOpen: true, Visible: true } || _system == null)
            return;

        if (!_admin.HasFlag(AdminFlags.Debug))
        {
            CloseWindow();
            return;
        }

        if (_timing.RealTime < _nextRequest)
            return;

        RequestCurrentTab();
    }
}
