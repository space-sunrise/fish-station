using System.Net;
using Content.Server.Administration.Systems;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Managers;

// Fish-start - отложенные баны
public sealed partial class BanManager
{
    private readonly List<DeferredBan> _deferredBans = new();

    public void CreateDeferredBan(CreateBanInfo banInfo, bool erase)
    {
        var deferredBan = new DeferredBan(banInfo, erase);

        lock (_deferredBans)
        {
            _deferredBans.Add(deferredBan);
        }

        string targetName = "Unknown";
        foreach (var user in banInfo.Users)
        {
            targetName = user.UserName;
            break;
        }
        _sawmill.Info($"Deferred ban queued for player {targetName}. Will be applied at the end of the round or upon disconnect.");
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
                bool matched = false;

                foreach (var user in ban.BanInfo.Users)
                {
                    if (user.UserId == session.UserId)
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched && ban.BanInfo.AddressRanges.Count > 0)
                {
                    var playerAddress = session.Channel.RemoteEndPoint.Address;
                    foreach (var range in ban.BanInfo.AddressRanges)
                    {
                        if (playerAddress.Equals(range.Address))
                        {
                            matched = true;
                            break;
                        }
                    }
                }

                if (!matched && ban.BanInfo.HWIds.Count > 0 && session.Channel.UserData.HWId.Length > 0)
                {
                    foreach (var hwid in ban.BanInfo.HWIds)
                    {
                        if (hwid.Equals(session.Channel.UserData.HWId))
                        {
                            matched = true;
                            break;
                        }
                    }
                }

                if (matched)
                {
                    bansToApply.Add(ban);
                    _deferredBans.RemoveAt(i);
                }
            }
        }

        foreach (var ban in bansToApply)
        {
            _sawmill.Info($"Applying deferred ban early because player {session.Name} ({session.UserId}) disconnected.");
            ExecuteDeferredBan(ban);
        }
    }

    private void ExecuteDeferredBan(DeferredBan ban)
    {
        if (ban.BanInfo is CreateRoleBanInfo roleBanInfo)
        {
            CreateRoleBan(roleBanInfo);
        }
        else if (ban.BanInfo is CreateServerBanInfo serverBanInfo)
        {
            if (ban.Erase)
            {
                foreach (var user in serverBanInfo.Users)
                {
                    try
                    {
                        if (_systems.TryGetEntitySystem(out AdminSystem? adminSystem))
                            adminSystem.Erase(user.UserId);
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error($"Error while erasing banned player:\n{e}");
                    }
                }
            }

            CreateServerBan(serverBanInfo);
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
    public CreateBanInfo BanInfo { get; }
    public bool Erase { get; }

    public DeferredBan(CreateBanInfo banInfo, bool erase)
    {
        BanInfo = banInfo;
        Erase = erase;
    }
}
// Fish-end
