using Content.Shared.Mind.Components;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Mind;

/// <summary>
/// Fish: очистка устаревших ссылок Mind в MindContainer.
/// </summary>
public abstract partial class SharedMindSystem
{
    /// <summary>
    /// Очищает <see cref="MindContainerComponent.Mind"/>, если UID больше не указывает на сущность с <see cref="MindComponent"/>.
    /// Без этого <see cref="MindContainerComponent.HasMind"/> остаётся true и блокирует ghost role / takeover.
    /// </summary>
    public bool ClearStaleMind(EntityUid uid, MindContainerComponent? container = null)
    {
        if (!Resolve(uid, ref container, false) || container.Mind == null)
            return false;

        if (TryComp(container.Mind.Value, out MindComponent? _))
            return false;

        container.Mind = null;
        Dirty(uid, container);
        return true;
    }
}
