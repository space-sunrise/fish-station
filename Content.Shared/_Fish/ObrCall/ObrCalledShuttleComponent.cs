namespace Content.Shared._Fish.ObrCall;

/// <summary>
/// Маркер шаттла, вызванного через систему ОБР. Хранит миссию для членов отряда.
/// </summary>
[RegisterComponent]
public sealed partial class ObrCalledShuttleComponent : Component
{
    [DataField]
    public string TeamId = string.Empty;

    [DataField]
    public string Mission = string.Empty;

    /// <summary>
    /// EntityUid game rule, создавшего этот шаттл.
    /// </summary>
    [DataField]
    public EntityUid? RuleUid;
}
