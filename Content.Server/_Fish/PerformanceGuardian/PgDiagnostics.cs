using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Shared._Fish.PerformanceGuardian;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Тяжёлая диагностика только по запросу / при инциденте.
/// Использует Transform, Grid, EntityLookup — без своих «теплокарт».
/// </summary>
public sealed class PgDiagnostics
{
    private readonly IEntityManager _entities;
    private readonly SharedTransformSystem _xform;
    private readonly SharedPhysicsSystem _physics;
    private readonly EntityLookupSystem _lookup;
    private readonly ISharedPlayerManager _players;

    private readonly Stopwatch _sw = new();
    private readonly List<(EntityUid Uid, float Score)> _scoreScratch = new(64);
    private readonly HashSet<Entity<ActorComponent>> _actorScratch = new();

    public PgDiagnostics(
        IEntityManager entities,
        SharedTransformSystem xform,
        SharedPhysicsSystem physics,
        EntityLookupSystem lookup,
        ISharedPlayerManager players)
    {
        _entities = entities;
        _xform = xform;
        _physics = physics;
        _lookup = lookup;
        _players = players;
    }

    public void Run(
        PgIdleMonitor idle,
        int eventRate,
        PgLoadClassifier classifier,
        float budgetMs,
        float nearbyRange,
        int topLimit,
        out PgLoadSource source,
        out string sourceText,
        out string placeName,
        out string coordinatesText,
        out NetEntity? placeTeleport,
        out List<PgEntityLoadRow> topEntities,
        out List<PgNearbyPlayerRow> nearbyPlayers,
        out string recommendation)
    {
        _sw.Restart();
        topEntities = new List<PgEntityLoadRow>(topLimit);
        nearbyPlayers = new List<PgNearbyPlayerRow>(8);
        placeName = "—";
        coordinatesText = "—";
        placeTeleport = null;
        source = classifier.ClassifyPrimary(idle.AwakeBodies, idle.AtmosActive, eventRate);
        if (idle.EntityCount > 12000 && source is PgLoadSource.Ok or PgLoadSource.Physics)
            source = PgLoadSource.Entities;
        sourceText = PgLoadClassifier.SourceToRu(source);

        EntityUid? hotGrid = null;
        Vector2 hotLocal = Vector2.Zero;
        MapCoordinates? hotMap = null;

        if (!BudgetExceeded(budgetMs))
            FindHotSpot(source, budgetMs, ref hotGrid, ref hotLocal, ref hotMap);

        if (hotGrid != null && _entities.TryGetComponent(hotGrid.Value, out MetaDataComponent? meta))
        {
            placeName = meta.EntityName;
            if (string.IsNullOrWhiteSpace(placeName))
                placeName = hotGrid.Value.ToString();
            placeTeleport = _entities.GetNetEntity(hotGrid.Value);
        }

        if (hotMap != null)
        {
            var p = hotMap.Value.Position;
            coordinatesText = $"карта {hotMap.Value.MapId}, X={p.X:F0} Y={p.Y:F0}";
            if (hotGrid != null)
                coordinatesText += $", локально сетки ({hotLocal.X:F0}, {hotLocal.Y:F0})";
        }

        if (!BudgetExceeded(budgetMs) && hotGrid != null)
            FillTopEntities(hotGrid.Value, topLimit, budgetMs, topEntities);

        if (!BudgetExceeded(budgetMs) && hotMap != null)
            FillNearbyPlayers(hotMap.Value, nearbyRange, budgetMs, nearbyPlayers);

        recommendation = BuildRecommendation(source, placeName, topEntities.Count, nearbyPlayers.Count);
        _sw.Stop();
    }

    private void FindHotSpot(
        PgLoadSource source,
        float budgetMs,
        ref EntityUid? hotGrid,
        ref Vector2 hotLocal,
        ref MapCoordinates? hotMap)
    {
        if (source is PgLoadSource.Atmos or PgLoadSource.Unknown or PgLoadSource.Ok)
        {
            var best = 0;
            var query = _entities.AllEntityQueryEnumerator<GridAtmosphereComponent, TransformComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out var atmos, out var xform, out _))
            {
                if (BudgetExceeded(budgetMs))
                    break;

                var score = atmos.ActiveTilesCount + atmos.HotspotTilesCount * 4;
                if (score <= best)
                    continue;

                best = score;
                hotGrid = uid;
                hotLocal = xform.LocalPosition;
                hotMap = _xform.GetMapCoordinates(uid, xform);
            }

            if (source == PgLoadSource.Atmos && hotGrid != null)
                return;
        }

        if (source is PgLoadSource.Physics or PgLoadSource.Events or PgLoadSource.Entities || hotGrid == null)
        {
            var counts = new Dictionary<EntityUid, int>(32);
            var scanned = 0;
            foreach (var ent in _physics.AwakeBodies)
            {
                if (BudgetExceeded(budgetMs) || scanned++ > 400)
                    break;

                var grid = ent.Comp2.GridUid;
                if (grid == null)
                    continue;

                counts.TryGetValue(grid.Value, out var c);
                counts[grid.Value] = c + 1;
            }

            EntityUid? bestGrid = null;
            var bestCount = 0;
            foreach (var (grid, count) in counts)
            {
                if (count <= bestCount)
                    continue;
                bestCount = count;
                bestGrid = grid;
            }

            if (bestGrid != null && _entities.TryGetComponent(bestGrid.Value, out TransformComponent? gx))
            {
                hotGrid = bestGrid;
                hotLocal = gx.LocalPosition;
                hotMap = _xform.GetMapCoordinates(bestGrid.Value, gx);
            }
        }
    }

    private void FillTopEntities(EntityUid grid, int limit, float budgetMs, List<PgEntityLoadRow> rows)
    {
        _scoreScratch.Clear();
        var scanned = 0;
        foreach (var ent in _physics.AwakeBodies)
        {
            if (BudgetExceeded(budgetMs) || scanned++ > 500)
                break;

            if (ent.Comp2.GridUid != grid)
                continue;

            var score = Math.Max(1f, ent.Comp1.Mass);
            _scoreScratch.Add((ent.Owner, score));
        }

        _scoreScratch.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        var n = Math.Min(limit, _scoreScratch.Count);
        for (var i = 0; i < n; i++)
        {
            var uid = _scoreScratch[i].Uid;
            rows.Add(new PgEntityLoadRow
            {
                Name = _entities.ToPrettyString(uid),
                Detail = $"масса ≈ {_scoreScratch[i].Score:F0}, активная физика",
                TeleportTarget = _entities.GetNetEntity(uid),
            });
        }
    }

    private void FillNearbyPlayers(MapCoordinates mapPos, float range, float budgetMs, List<PgNearbyPlayerRow> rows)
    {
        _actorScratch.Clear();
        _lookup.GetEntitiesInRange(mapPos, range, _actorScratch);

        var added = 0;
        foreach (var ent in _actorScratch)
        {
            if (BudgetExceeded(budgetMs) || added >= 8)
                break;

            if (!_players.TryGetSessionByEntity(ent.Owner, out var session))
                continue;

            var xform = _entities.GetComponent<TransformComponent>(ent.Owner);
            var pos = _xform.GetMapCoordinates(ent.Owner, xform);
            var dist = (pos.Position - mapPos.Position).Length();
            rows.Add(new PgNearbyPlayerRow
            {
                Name = session.Name,
                Detail = $"≈ {dist:F0} м",
                TeleportTarget = _entities.GetNetEntity(ent.Owner),
            });
            added++;
        }
    }

    private bool BudgetExceeded(float budgetMs) => _sw.Elapsed.TotalMilliseconds >= budgetMs;

    private static string BuildRecommendation(PgLoadSource source, string place, int topCount, int nearbyCount)
    {
        return source switch
        {
            PgLoadSource.Atmos =>
                $"Проверьте утечки и пожары на «{place}». При необходимости охладите зону или уберите источник газа.",
            PgLoadSource.Physics =>
                topCount > 0
                    ? $"Осмотрите активные объекты на «{place}» (см. список ниже). Уберите груды обломков / массовые столкновения."
                    : $"Осмотрите зону «{place}» — много активных физических тел.",
            PgLoadSource.Events =>
                nearbyCount > 0
                    ? "Рядом с очагом есть игроки — возможно массовый бой. Понаблюдайте или вмешивайтесь по правилам сервера."
                    : "Высокая частота событий. Проверьте взрывы, стрельбу и массовые взаимодействия.",
            PgLoadSource.Entities =>
                "Слишком много сущностей. Очистите мусор, лут и лишние спавнеры на загруженных сетках.",
            PgLoadSource.Ok =>
                "Сервер в норме. Если лаги всё же есть — смотрите сеть клиента или внешнюю нагрузку машины.",
            _ => "Запустите диагностику ещё раз после следующего всплеска.",
        };
    }
}
