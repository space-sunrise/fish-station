using Content.Shared._Fish.Audio;
using Robust.Server.Player;
using Robust.Shared.Network;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Audio;

public sealed partial class ServerGlobalSoundSystem
{
    [Dependency] private IPlayerManager _playerManager = default!;

    private void ShutdownFishAdminSounds()
    {
        StopAllAdminSounds();
    }

    public void StopAllAdminSounds()
    {
        RaiseNetworkEvent(new StopAdminSoundEvent());
    }

    public void StopPlayerAdminSounds(NetUserId userId)
    {
        if (_playerManager.TryGetSessionById(userId, out var session))
            RaiseNetworkEvent(new StopAdminSoundEvent(), session);
    }
}
