using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Fish.PerformanceGuardian;

/// <summary>
/// Fish-specific CVars. Separate [CVarDefs] class so Robust picks them up automatically.
/// </summary>
[CVarDefs]
public sealed partial class FishCCVars : CVars
{
}
