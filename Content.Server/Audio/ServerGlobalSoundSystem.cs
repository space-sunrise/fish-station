using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.Audio;

public sealed class ServerGlobalSoundSystem : SharedGlobalSoundSystem
{
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // #Fish edit start
    private readonly Dictionary<NetUserId, List<EntityUid>> _playerAdminSounds = new();
    // #Fish edit end

    public override void Shutdown()
    {
        StopAllAdminSounds();
        base.Shutdown();
        _conHost.UnregisterCommand("playglobalsound");
    }

    // #Fish edit start: периодическая очистка мёртвых сущностей
    private void CleanupDeadEntities()
    {
        var deadKeys = new List<NetUserId>();
        foreach (var kvp in _playerAdminSounds)
        {
            kvp.Value.RemoveAll(entity => !Exists(entity));
            if (kvp.Value.Count == 0)
                deadKeys.Add(kvp.Key);
        }
        foreach (var key in deadKeys)
            _playerAdminSounds.Remove(key);
    }
    // #Fish edit end

    public void PlayAdminGlobal(Filter playerFilter, ResolvedSoundSpecifier specifier, AudioParams? audioParams = null, bool replay = true)
    {
        // #Fish edit start: очищаем все списки перед добавлением
        CleanupDeadEntities();
        // #Fish edit end

        var sessions = playerFilter.Recipients;
        if (sessions == null || !sessions.Any())
            return;

        foreach (var session in sessions)
        {
            var userId = session.UserId;
            // Используем перегрузку с ICommonSession
            var result = _audio.PlayGlobal(specifier, session, audioParams);
            if (result != null)
            {
                if (!_playerAdminSounds.ContainsKey(userId))
                    _playerAdminSounds[userId] = new List<EntityUid>();
                _playerAdminSounds[userId].Add(result.Value.Entity);
            }
        }
    }

    public void StopAllAdminSounds()
    {
        // #Fish edit start: удаляем только валидные сущности, затем очищаем словарь
        foreach (var kvp in _playerAdminSounds.ToList())
        {
            foreach (var entity in kvp.Value)
            {
                if (Exists(entity))
                    Del(entity);
            }
        }
        _playerAdminSounds.Clear();
        // #Fish edit end
    }

    public void StopPlayerAdminSounds(NetUserId userId)
    {
        // #Fish edit start: сначала очищаем список от мёртвых, затем удаляем валидные
        if (_playerAdminSounds.TryGetValue(userId, out var list))
        {
            list.RemoveAll(entity => !Exists(entity));
            foreach (var entity in list)
            {
                if (Exists(entity))
                    Del(entity);
            }
            _playerAdminSounds.Remove(userId);
        }
        // #Fish edit end
    }

    // --- Остальные методы без изменений ---

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