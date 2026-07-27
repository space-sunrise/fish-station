using Robust.Shared.Serialization;

namespace Content.Shared._Fish.PAI;

[Serializable, NetSerializable]
public enum SyndicatePaiUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SyndicatePaiBoundUserInterfaceState : BoundUserInterfaceState
{
    public string? CarrierName;
    public string? MasterName;
    public string? CurrentReagent;
    public float CurrentVolume;
    public float MaxVolume;
    public List<SyndicatePaiReagentEntry> Reagents = [];
    public int CurrentReagentIndex;
    public string? SupplementalDirective;
}

[Serializable, NetSerializable]
public sealed class SyndicatePaiReagentEntry
{
    public string Id = string.Empty;
    public string Name = string.Empty;
    public int Index;
}

[Serializable, NetSerializable]
public sealed class SyndicatePaiInjectCarrierMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SyndicatePaiCycleReagentMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SyndicatePaiSelectReagentMessage : BoundUserInterfaceMessage
{
    public int Index;

    public SyndicatePaiSelectReagentMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class SyndicatePaiSetDirectiveMessage : BoundUserInterfaceMessage
{
    public string Directive;

    public SyndicatePaiSetDirectiveMessage(string directive)
    {
        Directive = directive;
    }
}

[Serializable, NetSerializable]
public sealed class SyndicatePaiImprintMasterMessage : BoundUserInterfaceMessage;
