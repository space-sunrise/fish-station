using Content.Shared._Fish.Audio;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Audio;

public sealed partial class ClientGlobalSoundSystem
{
    private void InitializeFishAudio()
    {
        SubscribeNetworkEvent<StopAdminSoundEvent>(OnStopAdminSound);
    }

    private void CleanupAdminAudioStreams()
    {
        for (var i = _adminAudio.Count - 1; i >= 0; i--)
        {
            var stream = _adminAudio[i];
            if (stream == null || TerminatingOrDeleted(stream.Value))
            {
                _adminAudio.RemoveAt(i);
            }
        }
    }

    private void OnStopAdminSound(StopAdminSoundEvent ev)
    {
        for (var i = _adminAudio.Count - 1; i >= 0; i--)
        {
            var stream = _adminAudio[i];
            if (stream != null && !TerminatingOrDeleted(stream.Value))
                _audio.Stop(stream);
            _adminAudio.RemoveAt(i);
        }
    }
}
