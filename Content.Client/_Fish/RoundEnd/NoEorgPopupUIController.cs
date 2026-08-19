using Content.Client.Lobby;
using Content.Shared.CCVar;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Client._Fish.RoundEnd
{
    public sealed class NoEorgPopupUIController : UIController, IOnStateEntered<LobbyState>
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private NoEorgPopup? _window;

        public void OnStateEntered(LobbyState state)
        {
            if (!_cfg.GetCVar(FishCVars.EorgPopupEnabled))
                return;

            OpenNoEorgPopup();
        }

        private void OpenNoEorgPopup()
        {
            if (_window != null)
                return;

            _window = new NoEorgPopup();
            _window.OpenCentered();
            _window.OnClose += () => _window = null;
        }
    }
}
