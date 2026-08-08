using Robust.Shared.GameStates;

namespace Content.Shared._Fish.Research.Components;

/// <summary>
/// Помечает предмет как пригодный для деструктивного анализа и задаёт research value.
/// Награда валидируется только на сервере; клиент видит Points лишь для examine/UI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResearchAnalyzableComponent : Component
{
    /// <summary>
    /// Сколько research points даёт успешный анализ одного экземпляра (или одной единицы стека).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Points = 1000;
}
