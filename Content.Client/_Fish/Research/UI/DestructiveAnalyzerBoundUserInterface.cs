using Content.Shared._Fish.Research;
using Content.Shared.Research.Components;
using Robust.Client.UserInterface;

namespace Content.Client._Fish.Research.UI;

public sealed class DestructiveAnalyzerBoundUserInterface : BoundUserInterface
{
    private DestructiveAnalyzerMenu? _menu;

    public DestructiveAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<DestructiveAnalyzerMenu>();
        _menu.OnServerPressed += () => SendMessage(new ConsoleServerSelectionMessage());
        _menu.OnAnalyzePressed += () => SendMessage(new DestructiveAnalyzerAnalyzeMessage());
        _menu.OnEjectPressed += () => SendMessage(new DestructiveAnalyzerEjectMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is DestructiveAnalyzerBoundUserInterfaceState cast)
            _menu?.Update(cast);
    }
}
