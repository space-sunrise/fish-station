using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Fish.Kitsune
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class KitsuneFoxLightsComponent : Component
    {
        [DataField]
        public List<EntityUid> Orbs = new();

        [DataField("dieAt", customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan DieAt;
    }

    [RegisterComponent, NetworkedComponent]
    public sealed partial class KitsuneFoxLightsOrbComponent : Component
    {
        [DataField]
        public float Angle;

        [DataField]
        public float Speed = 2f;

        [DataField]
        public float Radius = 1f;

        [DataField]
        public EntityUid Parent;
    }
}
