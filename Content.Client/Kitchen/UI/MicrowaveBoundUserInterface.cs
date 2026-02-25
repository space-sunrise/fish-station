using Content.Shared.Kitchen.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Kitchen.UI
{
    [UsedImplicitly]
    public sealed class MicrowaveBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MicrowaveMenu? _menu;

        [ViewVariables]
        private readonly Dictionary<int, EntityUid> _solids = new();

        //Sunrise-Start
        private readonly string _menuTitle;
        private readonly string _leftFlavorText;
        //Sunrise-End
        public MicrowaveBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            //Sunrise-Start
            if ((MicrowaveUiKey)uiKey == MicrowaveUiKey.ElectricRangeKey)
            {
                _menuTitle = "microwave-menu-range-title";
                _leftFlavorText = "microwave-menu-range-footer-flavor-left";
            }
            else
            {
                _menuTitle = "microwave-menu-title";
                _leftFlavorText = "microwave-menu-footer-flavor-left";
            }
            //Sunrise-End
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<MicrowaveMenu>();
            _menu.StartButton.OnPressed += _ => SendPredictedMessage(new MicrowaveStartCookMessage());
            _menu.EjectButton.OnPressed += _ => SendPredictedMessage(new MicrowaveEjectMessage());
            _menu.IngredientsList.OnItemSelected += args =>
            {
                SendPredictedMessage(new MicrowaveEjectSolidIndexedMessage(EntMan.GetNetEntity(_solids[args.ItemIndex])));
            };

            _menu.OnCookTimeSelected += (args, _) =>
            {
                var selectedCookTime = (uint) 0;

                if (args.Button is MicrowaveMenu.MicrowaveCookTimeButton actualButton)
                {
                    selectedCookTime = actualButton.CookTime;
                    SendPredictedMessage(new MicrowaveSelectCookTimeMessage((int) selectedCookTime / 5, actualButton.CookTime));

                    _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                                                                    ("time", selectedCookTime));
                }
                else
                {
                    // args.Button is a normal button aka instant cook button
                    SendPredictedMessage(new MicrowaveSelectCookTimeMessage((int) selectedCookTime, 0));

                    _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                                                         ("time", Loc.GetString("microwave-menu-instant-button")));
                }
            };
            //Sunrise-Start
            _menu.Title = Loc.GetString(_menuTitle);
            _menu.LeftFooter.Text = Loc.GetString(_leftFlavorText);
            //Sunrise-End
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not MicrowaveUpdateUserInterfaceState cState || _menu == null)
            {
                return;
            }

            _menu.IsBusy = cState.IsMicrowaveBusy;
            _menu.CurrentCooktimeEnd = cState.CurrentCookTimeEnd;

            _menu.ToggleBusyDisableOverlayPanel(cState.IsMicrowaveBusy || cState.ContainedSolids.Length == 0);
            // TODO move this to a component state and ensure the net ids.
            RefreshContentsDisplay(EntMan.GetEntityArray(cState.ContainedSolids));

            //Set the cook time info label
            var cookTime = cState.ActiveButtonIndex == 0
                ? Loc.GetString("microwave-menu-instant-button")
                : cState.CurrentCookTime.ToString();


            _menu.CookTimeInfoLabel.Text = Loc.GetString("microwave-bound-user-interface-cook-time-label",
                                                         ("time", cookTime));
            _menu.StartButton.Disabled = cState.IsMicrowaveBusy || cState.ContainedSolids.Length == 0;
            _menu.EjectButton.Disabled = cState.IsMicrowaveBusy || cState.ContainedSolids.Length == 0;


            //Set the correct button active button
            if (cState.ActiveButtonIndex == 0)
            {
                _menu.InstantCookButton.Pressed = true;
            }
            else
            {
                var currentlySelectedTimeButton = (Button) _menu.CookTimeButtonVbox.GetChild(cState.ActiveButtonIndex - 1);
                currentlySelectedTimeButton.Pressed = true;
            }

            //Set the "micowave light" ui color to indicate if the microwave is busy or not
            if (cState.IsMicrowaveBusy && cState.ContainedSolids.Length > 0)
            {
                _menu.IngredientsPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#947300") };
            }
            else
            {
                _menu.IngredientsPanel.PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#1B1B1E") };
            }
        }

        private void RefreshContentsDisplay(EntityUid[] containedSolids)
        {
            if (_menu == null) return;

            _solids.Clear();
            _menu.IngredientsList.Clear();
            foreach (var entity in containedSolids)
            {
                if (EntMan.Deleted(entity))
                {
                    continue;
                }

                if (!TryGetEntityTexture(entity, out var texture))
                    continue;

                var solidItem = _menu.IngredientsList.AddItem(EntMan.GetComponent<MetaDataComponent>(entity).EntityName, texture);
                var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
                _solids.Add(solidIndex, entity);
            }
        }

        private bool TryGetEntityTexture(EntityUid entity, out Texture? texture)
        {
            // TODO just use sprite view
            if (EntMan.TryGetComponent<IconComponent>(entity, out var iconComponent))
            {
                texture = EntMan.System<SpriteSystem>().GetIcon(iconComponent);
                return true;
            }

            if (EntMan.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
            {
                texture = spriteComponent.Icon?.Default;
                return true;
            }

            texture = null;
            return false;
        }
    }
}
