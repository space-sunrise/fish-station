using Robust.Shared.Configuration;

namespace Content.Server.AntiPidor;

[CVarDefs]
public sealed class AntiPidorCVars
{
    public static readonly CVarDef<string> AntiPidorWords =
        CVarDef.Create("antipidor.words", "ня, мяу", CVar.SERVERONLY);
}
