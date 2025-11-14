using System.Linq;
using System.Threading;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.AntiPidor;

/// <summary>
/// This handles my penis...
/// </summary>
[UsedImplicitly]
public sealed class AntiPidorSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public bool molchanka = false; // если да, то гибает всех, кто говорит
    public List<string> pidorWords = new List<string>();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntitySpokeEvent>(OnSpeak);
        pidorWords = GetDefaultPidorWords();
    }

    private void OnSpeak(EntitySpokeEvent args)
    {
        var pidor = args.Source;
        var pidorPos = _transform.GetMapCoordinates(pidor);
        int comedyDelay = 1000;

        if (!molchanka)
        {
            // проверка на пидора
            foreach (var pidorWord in pidorWords)
            {
                var pidorMsg = args.Message.ToLower();

                if (pidorMsg.Contains(pidorWord.ToLower()))
                {
                    Robust.Shared.Timing.Timer.Delay(comedyDelay);

                    // взрыв пидора
                    Robust.Shared.Timing.Timer.Spawn(_gameTiming.TickPeriod,
                        () => _explosion.QueueExplosion(pidorPos,
                            ExplosionSystem.DefaultExplosionPrototypeId, 4, 1, 2, pidor, maxTileBreak: 0),
                        CancellationToken.None);
                    // гиб пидора
                    _bodySystem.GibBody(pidor);
                }
            }
        }
        else
        {
            Robust.Shared.Timing.Timer.Delay(comedyDelay);

            // взрыв бедняжки
            Robust.Shared.Timing.Timer.Spawn(_gameTiming.TickPeriod,
                () => _explosion.QueueExplosion(pidorPos,
                    ExplosionSystem.DefaultExplosionPrototypeId, 4, 1, 2, pidor, maxTileBreak: 0),
                CancellationToken.None);
            // гиб бедняжки
            _bodySystem.GibBody(pidor);
        }
    }
    public string GetWords(List<string> list)
    {
        string words = "";

        foreach (var pidorWord in list)
        {
            words += pidorWord + " ";
        }
        return words;
    }
    public List<string> GetDefaultPidorWords()
    {
        var rawPidorWords = _cfg.GetCVar(AntiPidorCVars.AntiPidorWords);
        //делаем из строки лист
        var defaultPidorWords = rawPidorWords
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();
        return defaultPidorWords;
    }
}
