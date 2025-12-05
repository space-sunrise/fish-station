using Robust.Shared.GameStates;

namespace Content.Shared._Fish.DelayedOpen;

[RegisterComponent, NetworkedComponent]
public sealed partial class DelayedOpenComponent : Component
{
    [DataField]
    public float Delay = 5.0f;

    [DataField]
    public bool Enabled = true;
}
