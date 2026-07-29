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
    public string? SupplementalDirective;
    public bool MedicalUnlocked;
    public bool CanInjectOwner;

    // Ручной резервуар
    public string? CurrentReagent;
    public float CurrentVolume;
    public float MaxVolume;
    public List<SyndicatePaiReagentEntry> Reagents = [];
    public int CurrentReagentIndex;

    // Автодозатор
    public bool AutoDispenserUnlocked;
    public bool AutoDispenserEnabled;
    public float AutoHealthThreshold;
    public string? AutoReagent;
    public float AutoVolume;
    public float AutoMaxVolume;
    public List<SyndicatePaiReagentEntry> AutoReagents = [];
    public int AutoReagentIndex;
    public float AutoCooldownRemaining;
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
public sealed class SyndicatePaiSelectReagentMessage : BoundUserInterfaceMessage
{
    public int Index;
    public bool AutoReservoir;

    public SyndicatePaiSelectReagentMessage(int index, bool autoReservoir = false)
    {
        Index = index;
        AutoReservoir = autoReservoir;
    }
}

[Serializable, NetSerializable]
public sealed class SyndicatePaiSetAutoEnabledMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public SyndicatePaiSetAutoEnabledMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class SyndicatePaiSetAutoThresholdMessage : BoundUserInterfaceMessage
{
    public float Threshold;

    public SyndicatePaiSetAutoThresholdMessage(float threshold)
    {
        Threshold = threshold;
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
