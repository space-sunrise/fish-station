using System.Net;
using Content.Server.Administration.Systems;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers;

// Отложенные баны
public sealed partial class BanManager
{
    private readonly List<DeferredBan> _deferredBans = new();

    public void CreateDeferredBan(
        NetUserId? target,
        string? targetUsername,
        NetUserId? banningAdmin,
        (IPAddress, int)? addressRange,
        ImmutableTypedHwid? hwid,
        uint? minutes,
        NoteSeverity severity,
        string reason,
        ProtoId<JobPrototype>[]? bannedJobs,
        ProtoId<AntagPrototype>[]? bannedAntags,
        bool erase)
    {
        var deferredBan = new DeferredBan(
            target,
            targetUsername,
            banningAdmin,
            addressRange,
            hwid,
            minutes,
            severity,
            reason,
            bannedJobs,
            bannedAntags,
            erase
        );

        lock (_deferredBans)
        {
            _deferredBans.Add(deferredBan);
        }

        var targetName = targetUsername ?? target?.ToString() ?? "Unknown";
        _sawmill.Info(
            $"Deferred ban queued for player {targetName}. Will be applied at the end of the round or upon disconnect.");
    }

    public void ApplyDeferredBans()
    {
        List<DeferredBan> bansToApply;
        lock (_deferredBans)
        {
            bansToApply = new List<DeferredBan>(_deferredBans);
            _deferredBans.Clear();
        }

        foreach (var ban in bansToApply)
        {
            ExecuteDeferredBan(ban);
        }
    }

    private void ApplyDeferredBansForPlayer(ICommonSession session)
    {
        List<DeferredBan> bansToApply = new();
        lock (_deferredBans)
        {
            for (int i = _deferredBans.Count - 1; i >= 0; i--)
            {
                var ban = _deferredBans[i];
                if (ban.Target == session.UserId ||
                    (ban.AddressRange != null &&
                     session.Channel.RemoteEndPoint.Address.Equals(ban.AddressRange.Value.Item1)) ||
                    (ban.HWId != null && ban.HWId.Equals(session.Channel.UserData.HWId)))
                {
                    bansToApply.Add(ban);
                    _deferredBans.RemoveAt(i);
                }
            }
        }

        foreach (var ban in bansToApply)
        {
            _sawmill.Info(
                $"Applying deferred ban early because player {session.Name} ({session.UserId}) disconnected.");
            ExecuteDeferredBan(ban);
        }
    }

    private void ExecuteDeferredBan(DeferredBan ban)
    {
        if (ban.BannedJobs?.Length > 0 || ban.BannedAntags?.Length > 0)
        {
            var now = DateTimeOffset.UtcNow;
            List<string> roles = [];
            foreach (var role in ban.BannedJobs ?? [])
            {
                CreateRoleBan(
                    ban.Target,
                    ban.TargetUsername,
                    ban.BanningAdmin,
                    ban.AddressRange,
                    ban.HWId,
                    role,
                    ban.Minutes,
                    ban.Severity,
                    ban.Reason,
                    now
                );
                roles.Add(role.Id);
            }

            foreach (var role in ban.BannedAntags ?? [])
            {
                CreateRoleBan(
                    ban.Target,
                    ban.TargetUsername,
                    ban.BanningAdmin,
                    ban.AddressRange,
                    ban.HWId,
                    role,
                    ban.Minutes,
                    ban.Severity,
                    ban.Reason,
                    now
                );
                roles.Add(role.Id);
            }

            WebhookUpdateRoleBans(ban.Target,
                ban.TargetUsername,
                ban.BanningAdmin,
                ban.AddressRange,
                ban.HWId,
                roles,
                ban.Minutes,
                ban.Severity,
                ban.Reason,
                now);
        }
        else
        {
            if (ban.Erase && ban.Target is not null)
            {
                try
                {
                    if (_systems.TryGetEntitySystem(out AdminSystem? adminSystem))
                        adminSystem.Erase(ban.Target.Value);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Error while erasing banned player:\n{e}");
                }
            }

            CreateServerBan(
                ban.Target,
                ban.TargetUsername,
                ban.BanningAdmin,
                ban.AddressRange,
                ban.HWId,
                ban.Minutes,
                ban.Severity,
                ban.Reason
            );
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            ApplyDeferredBansForPlayer(e.Session);
        }
    }
}

public sealed class DeferredBan
{
    public NetUserId? Target { get; }
    public string? TargetUsername { get; }
    public NetUserId? BanningAdmin { get; }
    public (IPAddress, int)? AddressRange { get; }
    public ImmutableTypedHwid? HWId { get; }
    public uint? Minutes { get; }
    public NoteSeverity Severity { get; }
    public string Reason { get; }
    public ProtoId<JobPrototype>[]? BannedJobs { get; }
    public ProtoId<AntagPrototype>[]? BannedAntags { get; }
    public bool Erase { get; }

    public DeferredBan(
        NetUserId? target,
        string? targetUsername,
        NetUserId? banningAdmin,
        (IPAddress, int)? addressRange,
        ImmutableTypedHwid? hwid,
        uint? minutes,
        NoteSeverity severity,
        string reason,
        ProtoId<JobPrototype>[]? bannedJobs,
        ProtoId<AntagPrototype>[]? bannedAntags,
        bool erase)
    {
        Target = target;
        TargetUsername = targetUsername;
        BanningAdmin = banningAdmin;
        AddressRange = addressRange;
        HWId = hwid;
        Minutes = minutes;
        Severity = severity;
        Reason = reason;
        BannedJobs = bannedJobs;
        BannedAntags = bannedAntags;
        Erase = erase;
    }
}
