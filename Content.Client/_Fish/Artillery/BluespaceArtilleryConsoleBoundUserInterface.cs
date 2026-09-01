using Content.Shared._Fish.Artillery;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client._Fish.Artillery;

public sealed class BluespaceArtilleryConsoleBoundUserInterface : BoundUserInterface
{
    private BluespaceArtilleryConsoleWindow? _window;

    public BluespaceArtilleryConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new BluespaceArtilleryConsoleWindow();
        _window.OnFire += () => SendMessage(new BluespaceArtilleryFireMessage());
        _window.OnCoordsChanged += coords => SendMessage(new BluespaceArtillerySetCoordsMessage { Coordinates = coords });
        _window.OnParamsChanged += (type, total, slope, max) =>
            SendMessage(new BluespaceArtillerySetParamsMessage
            {
                ExplosionType = type,
                TotalIntensity = total,
                Slope = slope,
                MaxIntensity = max
            });
        _window.OnPreviewToggled += enabled => SendMessage(new BluespaceArtilleryPreviewMessage { Enabled = enabled });

        var uiManager = IoCManager.Resolve<IUserInterfaceManager>();
        uiManager.WindowRoot.AddChild(_window);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is BluespaceArtilleryConsoleBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _window != null)
        {
            _window.Dispose();
            _window = null;
        }
    }
}