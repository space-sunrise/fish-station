using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Console;
// #Fish edit start: added for admin sound management (Networking and LINQ)
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Linq;
// #Fish edit end

namespace Content.Server.Audio;

public sealed class ServerGlobalSoundSystem : SharedGlobalSoundSystem
{
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // #Fish edit start: store per‑player admin sound entities for later stopping
    private readonly Dictionary<NetUserId, List<EntityUid>> _playerAdminSounds = new();
    // #Fish edit end

    public override void Shutdown()
    {
        // #Fish edit start: stop all admin sounds when system shuts down
        StopAllAdminSounds();
        // #Fish edit end
        base.Shutdown();
        _conHost.UnregisterCommand("playglobalsound");
    }

    // #Fish edit start: completely rewritten to create individual sound entities per player
    public void PlayAdminGlobal(Filter playerFilter, ResolvedSoundSpecifier specifier, AudioParams? audioParams = null, bool replay = true)
    {
        var sessions = playerFilter.Recipients;
        if (sessions == null || !sessions.Any())
            return;

        foreach (var session in sessions)
        {
            var userId = session.UserId;
            var result = _audio.PlayGlobal(specifier, Filter.Empty().AddPlayer(session), replay, audioParams);
            if (result != null)
            {
                if (!_playerAdminSounds.ContainsKey(userId))
                    _playerAdminSounds[userId] = new List<EntityUid>();
                _playerAdminSounds[userId].Add(result.Value.Entity);
            }
        }
    }
    // #Fish edit end

    // #Fish edit start: new methods to stop admin sounds (all or for a specific player)
    public void StopAllAdminSounds()
    {
        foreach (var list in _playerAdminSounds.Values)
        {
            foreach (var entity in list)
            {
                if (Exists(entity))
                    Del(entity);
            }
        }
        _playerAdminSounds.Clear();
    }

    public void StopPlayerAdminSounds(NetUserId userId)
    {
        if (_playerAdminSounds.TryGetValue(userId, out var list))
        {
            foreach (var entity in list)
            {
                if (Exists(entity))
                    Del(entity);
            }
            _playerAdminSounds.Remove(userId);
        }
    }
    // #Fish edit end

    // --- Everything below is unchanged from the original file ---

    private Filter GetStationAndPvs(EntityUid source)
    {
        var stationFilter = _stationSystem.GetInOwningStation(source);
        stationFilter.AddPlayersByPvs(source, entityManager: EntityManager);
        return stationFilter;
    }

    public void PlayGlobalOnStation(EntityUid source, ResolvedSoundSpecifier specifier, AudioParams? audioParams = null)
    {
        var msg = new GameGlobalSoundEvent(specifier, audioParams);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    public void StopStationEventMusic(EntityUid source, StationEventMusicType type)
    {
        var msg = new StopStationEventMusic(type);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }

    public void DispatchStationEventMusic(EntityUid source, SoundSpecifier sound, StationEventMusicType type)
    {
        DispatchStationEventMusic(source, _audio.ResolveSound(sound), type);
    }

    public void DispatchStationEventMusic(EntityUid source, ResolvedSoundSpecifier specifier, StationEventMusicType type)
    {
        var audio = AudioParams.Default.WithVolume(-8);
        var msg = new StationEventMusicEvent(specifier, type, audio);
        var filter = GetStationAndPvs(source);
        RaiseNetworkEvent(msg, filter);
    }
}