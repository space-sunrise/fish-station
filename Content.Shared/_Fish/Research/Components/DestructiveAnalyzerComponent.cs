using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Fish.Research.Components;

/// <summary>
/// Стационарный destructive analyzer: принимает предмет в ItemSlot и разрушает его ради research points.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DestructiveAnalyzerComponent : Component
{
    public const string DefaultSlotId = "analyzer_input";

    [DataField]
    public string SlotId = DefaultSlotId;

    /// <summary>
    /// Длительность анализа перед уничтожением предмета.
    /// </summary>
    [DataField]
    public TimeSpan AnalysisDuration = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Момент завершения текущего анализа. null — машина свободна.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? AnalysisFinishTime;

    /// <summary>
    /// EntityUid предмета, который сейчас анализируется (защита от гонок / подмены).
    /// </summary>
    [DataField]
    public EntityUid? AnalyzingItem;

    /// <summary>
    /// Сколько points будет начислено при успешном завершении (зафиксировано при старте).
    /// </summary>
    [DataField]
    public int PendingPoints;

    [DataField]
    public SoundSpecifier AnalyzeSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");
}
