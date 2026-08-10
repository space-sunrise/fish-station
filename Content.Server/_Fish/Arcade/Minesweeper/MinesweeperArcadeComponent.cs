using Content.Shared._Fish.Arcade.Minesweeper;

namespace Content.Server._Fish.Arcade.Minesweeper;

/// <summary>
///     Консоль с сапёром: хранит текущую партию, активного игрока и отсечки таймера.
/// </summary>
[RegisterComponent, Access(typeof(MinesweeperArcadeSystem))]
public sealed partial class MinesweeperArcadeComponent : Component
{
    /// <summary>
    ///     Сложность, с которой создаётся первая партия после спавна консоли.
    /// </summary>
    [DataField]
    public MinesweeperDifficulty DefaultDifficulty = MinesweeperDifficulty.Easy;

    /// <summary>
    ///     Текущая партия.
    /// </summary>
    [ViewVariables]
    public MinesweeperGame? Game;

    /// <summary>
    ///     Кто сейчас играет.
    /// </summary>
    [ViewVariables]
    public EntityUid? Player;

    /// <summary>
    ///     Момент первого хода по серверному времени, либо <c>null</c>, если партия ещё не начата.
    /// </summary>
    [ViewVariables]
    public TimeSpan? StartTime;

    /// <summary>
    ///     Момент завершения партии по серверному времени, либо <c>null</c>, если партия ещё идёт.
    /// </summary>
    [ViewVariables]
    public TimeSpan? EndTime;
}
