using Robust.Shared.Serialization;

namespace Content.Shared._Fish.Research;

[Serializable, NetSerializable]
public enum DestructiveAnalyzerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DestructiveAnalyzerBoundUserInterfaceState : BoundUserInterfaceState
{
    public int ServerPoints;
    public bool ConnectedToServer;
    public bool HasItem;
    public string? ItemName;
    public int ResearchValue;
    public bool CanAnalyze;
    public bool IsAnalyzing;

    public DestructiveAnalyzerBoundUserInterfaceState(
        int serverPoints,
        bool connectedToServer,
        bool hasItem,
        string? itemName,
        int researchValue,
        bool canAnalyze,
        bool isAnalyzing)
    {
        ServerPoints = serverPoints;
        ConnectedToServer = connectedToServer;
        HasItem = hasItem;
        ItemName = itemName;
        ResearchValue = researchValue;
        CanAnalyze = canAnalyze;
        IsAnalyzing = isAnalyzing;
    }
}

[Serializable, NetSerializable]
public sealed class DestructiveAnalyzerAnalyzeMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class DestructiveAnalyzerEjectMessage : BoundUserInterfaceMessage;
