using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Audio;

/// <summary>
///     Событие для остановки воспроизведения админских глобальных звуков на клиенте.
/// </summary>
[Serializable, NetSerializable]
public sealed class StopAdminSoundEvent : EntityEventArgs
{
}
