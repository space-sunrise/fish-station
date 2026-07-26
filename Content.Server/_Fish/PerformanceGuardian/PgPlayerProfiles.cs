using Content.Shared._Fish.PerformanceGuardian;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Fish.PerformanceGuardian;

/// <summary>
/// Session-keyed compact rolling profiles with recycled fixed slots.
/// </summary>
public sealed class PgPlayerProfiles
{
    private sealed class Slot
    {
        public NetUserId UserId;
        public string Name = string.Empty;
        public bool Occupied;
        public int Events10s;
        public int Events1m;
        public int Events5m;
        public int Attack;
        public int Damage;
        public int Throw;
        public int Projectile;
        public float Risk;
        public TimeSpan Window10sStart;
        public TimeSpan Window1mStart;
        public TimeSpan Window5mStart;
    }

    private readonly Slot[] _slots;
    private readonly Dictionary<NetUserId, int> _index = new();

    public PgPlayerProfiles(int capacity)
    {
        _slots = new Slot[Math.Max(8, capacity)];
        for (var i = 0; i < _slots.Length; i++)
            _slots[i] = new Slot();
    }

    public void Resize(int capacity)
    {
        // Fixed at construction for predictability; ignore soft resize requests that shrink mid-round.
        if (capacity <= _slots.Length)
            return;
    }

    public void EnsurePlayer(ICommonSession session, TimeSpan now)
    {
        if (_index.ContainsKey(session.UserId))
        {
            var s = _slots[_index[session.UserId]];
            s.Name = session.Name;
            return;
        }

        var free = -1;
        for (var i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].Occupied)
            {
                free = i;
                break;
            }
        }

        if (free < 0)
        {
            // Recycle lowest-risk slot
            free = 0;
            var best = float.MaxValue;
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Risk < best)
                {
                    best = _slots[i].Risk;
                    free = i;
                }
            }

            _index.Remove(_slots[free].UserId);
        }

        var slot = _slots[free];
        slot.Occupied = true;
        slot.UserId = session.UserId;
        slot.Name = session.Name;
        slot.Events10s = slot.Events1m = slot.Events5m = 0;
        slot.Attack = slot.Damage = slot.Throw = slot.Projectile = 0;
        slot.Risk = 0;
        slot.Window10sStart = slot.Window1mStart = slot.Window5mStart = now;
        _index[session.UserId] = free;
    }

    public void Record(NetUserId userId, PgMetricCategory category, TimeSpan now)
    {
        if (!_index.TryGetValue(userId, out var idx))
            return;

        var s = _slots[idx];
        RollWindows(s, now);
        s.Events10s++;
        s.Events1m++;
        s.Events5m++;

        switch (category)
        {
            case PgMetricCategory.Attack:
                s.Attack++;
                break;
            case PgMetricCategory.Damage:
                s.Damage++;
                break;
            case PgMetricCategory.Throw:
                s.Throw++;
                break;
            case PgMetricCategory.Projectile:
                s.Projectile++;
                break;
        }

        s.Risk = Math.Min(100f, s.Events1m * 0.5f + s.Attack * 0.2f + s.Projectile * 0.15f);
    }

    private static void RollWindows(Slot s, TimeSpan now)
    {
        if (now - s.Window10sStart > TimeSpan.FromSeconds(10))
        {
            s.Events10s = 0;
            s.Window10sStart = now;
        }

        if (now - s.Window1mStart > TimeSpan.FromMinutes(1))
        {
            s.Events1m = 0;
            s.Attack = s.Damage = s.Throw = s.Projectile = 0;
            s.Window1mStart = now;
        }

        if (now - s.Window5mStart > TimeSpan.FromMinutes(5))
        {
            s.Events5m = 0;
            s.Window5mStart = now;
        }
    }

    public void Remove(NetUserId userId)
    {
        if (!_index.Remove(userId, out var idx))
            return;
        _slots[idx].Occupied = false;
    }

    public void CopyRows(List<PgPlayerRow> destination, int max)
    {
        destination.Clear();
        var tmp = new List<Slot>(_slots.Length);
        for (var i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].Occupied)
                tmp.Add(_slots[i]);
        }

        tmp.Sort(static (a, b) => b.Risk.CompareTo(a.Risk));
        var n = Math.Min(max, tmp.Count);
        for (var i = 0; i < n; i++)
        {
            var s = tmp[i];
            destination.Add(new PgPlayerRow
            {
                Name = s.Name,
                UserId = s.UserId.ToString(),
                EventsPerSec10s = s.Events10s / 10f,
                EventsPerSec1m = s.Events1m / 60f,
                EventsPerSec5m = s.Events5m / 300f,
                RiskScore = s.Risk,
                AttackCount = s.Attack,
                DamageCount = s.Damage,
                ThrowCount = s.Throw,
                ProjectileCount = s.Projectile,
            });
        }
    }
}
