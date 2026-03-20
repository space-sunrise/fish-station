using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Fish.Kitsune
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class KitsuneFireComponent : Component
    {
        [DataField]
        public DamageSpecifier Healing = new()
        {
            DamageDict = new()
            {
                { "Heat", -2 },
                { "Cold", -2 },
                { "Shock", -2 }
            }
        };

        [DataField("duration", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan Duration;

        [DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan NextTick;
    }
}
