using System.Collections.Generic;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Popups;
using Content.Shared._Fish.Achievements;
using Content.Shared.Popups;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Prototypes;

namespace Content.Client._Fish.Achievements;

/// <summary>
/// Единый контроллер окна достижений для Lobby и ESC.
/// </summary>
public sealed class AchievementUIController :
    UIController,
    IOnStateEntered<LobbyState>,
    IOnStateEntered<GameplayState>,
    IOnStateExited<LobbyState>,
    IOnStateExited<GameplayState>
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    private AchievementWindow? _window;
    private readonly Dictionary<string, AchievementPlayerState> _states = new();
    private bool _subscribed;

    public void OnStateEntered(LobbyState state) => EnsureSubscribed();
    public void OnStateEntered(GameplayState state) => EnsureSubscribed();

    public void OnStateExited(LobbyState state) => CloseWindow();
    public void OnStateExited(GameplayState state) => CloseWindow();

    private void EnsureSubscribed()
    {
        if (_subscribed)
        {
            _systems.GetEntitySystem<AchievementClientSystem>().RequestSnapshot();
            return;
        }

        var net = _systems.GetEntitySystem<AchievementClientSystem>();
        net.SnapshotReceived += OnSnapshot;
        net.ProgressReceived += OnProgress;
        _subscribed = true;
        net.RequestSnapshot();
    }

    public void ToggleWindow()
    {
        if (_window != null)
        {
            CloseWindow();
            return;
        }

        OpenWindow();
    }

    private void OpenWindow()
    {
        _window = new AchievementWindow();
        _window.OnClose += () => _window = null;
        _window.Populate(_prototypes, _states);
        _window.OpenCentered();
        _systems.GetEntitySystem<AchievementClientSystem>().RequestSnapshot();
    }

    private void CloseWindow()
    {
        _window?.Close();
        _window = null;
    }

    private void OnSnapshot(List<AchievementPlayerState> entries)
    {
        _states.Clear();
        foreach (var entry in entries)
            _states[entry.AchievementId] = entry;

        _window?.Populate(_prototypes, _states);
    }

    private void OnProgress(AchievementProgressUpdatedEvent ev)
    {
        _states[ev.Entry.AchievementId] = ev.Entry;
        _window?.UpdateEntry(ev.Entry);

        if (!ev.JustUnlocked)
            return;

        var name = _prototypes.TryIndex<AchievementPrototype>(ev.Entry.AchievementId, out var proto)
            ? Loc.GetString(proto.Name)
            : ev.Entry.AchievementId;

        var message = Loc.GetString(
            ev.NotificationLocId ?? "fish-achievements-unlocked",
            ("name", name));

        _systems.GetEntitySystem<PopupSystem>().PopupCursor(message, PopupType.Large);
    }
}
