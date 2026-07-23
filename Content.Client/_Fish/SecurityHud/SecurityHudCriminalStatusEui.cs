using Content.Client.Eui;
using Content.Shared._Fish.SecurityHud;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Fish.SecurityHud;

[UsedImplicitly]
public sealed class SecurityHudCriminalStatusEui : BaseEui
{
    private readonly SecurityHudCriminalStatusWindow _window;

    public SecurityHudCriminalStatusEui()
    {
        _window = new SecurityHudCriminalStatusWindow();

        _window.OnStatusSelected += (status, reason) =>
        {
            SendMessage(new SecurityHudCriminalStatusChangeMessage(status, reason));
        };

        _window.OnClose += () =>
        {
            SendMessage(new CloseEuiMessage());
        };
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not SecurityHudCriminalStatusEuiState cast)
            return;

        _window.UpdateState(cast.TargetName, cast.JobTitle, cast.Status, cast.Reason, cast.MaxStringLength);
    }
}
