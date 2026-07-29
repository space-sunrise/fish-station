using Content.Shared._Fish.PAI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Globalization;
using System.Numerics;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Fish.PAI;

public sealed class SyndicatePaiBoundUserInterface : BoundUserInterface
{
    private SyndicatePaiWindow? _window;

    public SyndicatePaiBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new SyndicatePaiWindow();
        _window.OnClose += Close;
        _window.OnInject += () => SendMessage(new SyndicatePaiInjectCarrierMessage());
        _window.OnSelectReagent += (index, auto) => SendMessage(new SyndicatePaiSelectReagentMessage(index, auto));
        _window.OnSetAutoEnabled += enabled => SendMessage(new SyndicatePaiSetAutoEnabledMessage(enabled));
        _window.OnSetAutoThreshold += threshold => SendMessage(new SyndicatePaiSetAutoThresholdMessage(threshold));
        _window.OnSetDirective += text => SendMessage(new SyndicatePaiSetDirectiveMessage(text));
        _window.OnImprint += () => SendMessage(new SyndicatePaiImprintMasterMessage());
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SyndicatePaiBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Dispose();
        _window = null;
    }
}

public sealed class SyndicatePaiWindow : DefaultWindow
{
    public event Action? OnInject;
    public event Action? OnImprint;
    public event Action<int, bool>? OnSelectReagent;
    public event Action<bool>? OnSetAutoEnabled;
    public event Action<float>? OnSetAutoThreshold;
    public event Action<string>? OnSetDirective;

    private readonly Label _carrierLabel;
    private readonly Label _masterLabel;
    private readonly Label _reagentLabel;
    private readonly Label _volumeLabel;
    private readonly Label _directiveLabel;
    private readonly LineEdit _directiveEdit;
    private readonly BoxContainer _reagentList;
    private readonly Button _injectButton;

    private readonly Control _autoSection;
    private readonly Button _autoToggleButton;
    private readonly Label _autoReagentLabel;
    private readonly Label _autoVolumeLabel;
    private readonly Label _autoCooldownLabel;
    private readonly Label _autoThresholdLabel;
    private readonly LineEdit _autoThresholdEdit;
    private readonly BoxContainer _autoReagentList;
    private bool _autoEnabled;

    public SyndicatePaiWindow()
    {
        Title = Loc.GetString("syndicate-pai-ui-title");
        MinSize = new Vector2(440, 620);
        SetSize = new Vector2(440, 620);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(8),
            SeparationOverride = 6,
        };

        _carrierLabel = new Label();
        _masterLabel = new Label();
        _reagentLabel = new Label();
        _volumeLabel = new Label();
        _directiveLabel = new Label { Text = Loc.GetString("syndicate-pai-ui-directive") };

        _directiveEdit = new LineEdit
        {
            PlaceHolder = Loc.GetString("syndicate-pai-ui-directive-placeholder"),
        };

        _injectButton = new Button { Text = Loc.GetString("syndicate-pai-ui-inject") };
        _injectButton.OnPressed += _ => OnInject?.Invoke();

        var imprintButton = new Button { Text = Loc.GetString("syndicate-pai-ui-imprint") };
        imprintButton.OnPressed += _ => OnImprint?.Invoke();

        var setDirectiveButton = new Button { Text = Loc.GetString("syndicate-pai-ui-set-directive") };
        setDirectiveButton.OnPressed += _ => OnSetDirective?.Invoke(_directiveEdit.Text);

        var actionRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };
        actionRow.AddChild(_injectButton);
        actionRow.AddChild(imprintButton);

        _reagentList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        root.AddChild(_carrierLabel);
        root.AddChild(_masterLabel);
        root.AddChild(new Label { Text = Loc.GetString("syndicate-pai-ui-manual-section") });
        root.AddChild(_reagentLabel);
        root.AddChild(_volumeLabel);
        root.AddChild(actionRow);
        root.AddChild(new Label { Text = Loc.GetString("syndicate-pai-ui-reagents") });
        root.AddChild(_reagentList);

        _autoToggleButton = new Button();
        _autoToggleButton.OnPressed += _ => OnSetAutoEnabled?.Invoke(!_autoEnabled);

        _autoReagentLabel = new Label();
        _autoVolumeLabel = new Label();
        _autoCooldownLabel = new Label();
        _autoThresholdLabel = new Label { Text = Loc.GetString("syndicate-pai-ui-auto-threshold") };
        _autoThresholdEdit = new LineEdit { PlaceHolder = "40" };
        var applyThreshold = new Button { Text = Loc.GetString("syndicate-pai-ui-auto-threshold-apply") };
        applyThreshold.OnPressed += _ =>
        {
            if (float.TryParse(_autoThresholdEdit.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                OnSetAutoThreshold?.Invoke(value);
        };

        _autoReagentList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        var thresholdRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };
        thresholdRow.AddChild(_autoThresholdEdit);
        thresholdRow.AddChild(applyThreshold);

        _autoSection = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Visible = false,
        };
        _autoSection.AddChild(new Label { Text = Loc.GetString("syndicate-pai-ui-auto-section") });
        _autoSection.AddChild(_autoToggleButton);
        _autoSection.AddChild(_autoReagentLabel);
        _autoSection.AddChild(_autoVolumeLabel);
        _autoSection.AddChild(_autoCooldownLabel);
        _autoSection.AddChild(_autoThresholdLabel);
        _autoSection.AddChild(thresholdRow);
        _autoSection.AddChild(new Label { Text = Loc.GetString("syndicate-pai-ui-auto-reagents") });
        _autoSection.AddChild(_autoReagentList);

        root.AddChild(_autoSection);
        root.AddChild(_directiveLabel);
        root.AddChild(_directiveEdit);
        root.AddChild(setDirectiveButton);

        Contents.AddChild(root);
    }

    public void UpdateState(SyndicatePaiBoundUserInterfaceState state)
    {
        _carrierLabel.Text = Loc.GetString("syndicate-pai-ui-carrier",
            ("name", state.CarrierName ?? Loc.GetString("syndicate-pai-ui-none")));
        _masterLabel.Text = Loc.GetString("syndicate-pai-ui-master",
            ("name", state.MasterName ?? Loc.GetString("syndicate-pai-ui-none")));
        _reagentLabel.Text = Loc.GetString("syndicate-pai-ui-current-reagent",
            ("reagent", state.CurrentReagent ?? Loc.GetString("syndicate-pai-ui-none")));
        _volumeLabel.Text = Loc.GetString("syndicate-pai-ui-volume",
            ("current", state.CurrentVolume.ToString("0.#")),
            ("max", state.MaxVolume.ToString("0.#")));

        _injectButton.Disabled = !state.CanInjectOwner;
        _injectButton.Visible = state.MedicalUnlocked;

        if (state.SupplementalDirective != null)
            _directiveEdit.Text = state.SupplementalDirective;

        _reagentList.RemoveAllChildren();
        foreach (var reagent in state.Reagents)
        {
            var selected = reagent.Index == state.CurrentReagentIndex;
            var button = new Button
            {
                Text = selected
                    ? Loc.GetString("syndicate-pai-ui-reagent-selected", ("name", reagent.Name))
                    : reagent.Name,
                HorizontalExpand = true,
                Visible = state.MedicalUnlocked,
            };
            var index = reagent.Index;
            button.OnPressed += _ => OnSelectReagent?.Invoke(index, false);
            _reagentList.AddChild(button);
        }

        _autoSection.Visible = state.AutoDispenserUnlocked;
        if (!state.AutoDispenserUnlocked)
            return;

        _autoEnabled = state.AutoDispenserEnabled;
        _autoToggleButton.Text = state.AutoDispenserEnabled
            ? Loc.GetString("syndicate-pai-ui-auto-enabled")
            : Loc.GetString("syndicate-pai-ui-auto-disabled");

        _autoReagentLabel.Text = Loc.GetString("syndicate-pai-ui-auto-reagent",
            ("reagent", state.AutoReagent ?? Loc.GetString("syndicate-pai-ui-none")));
        _autoVolumeLabel.Text = Loc.GetString("syndicate-pai-ui-auto-volume",
            ("current", state.AutoVolume.ToString("0.#")),
            ("max", state.AutoMaxVolume.ToString("0.#")));

        _autoCooldownLabel.Text = state.AutoCooldownRemaining > 0
            ? Loc.GetString("syndicate-pai-ui-auto-cooldown", ("seconds", ((int)state.AutoCooldownRemaining).ToString()))
            : Loc.GetString("syndicate-pai-ui-auto-cooldown-ready");

        if (!_autoThresholdEdit.HasKeyboardFocus())
            _autoThresholdEdit.Text = state.AutoHealthThreshold.ToString("0", CultureInfo.InvariantCulture);

        _autoReagentList.RemoveAllChildren();
        foreach (var reagent in state.AutoReagents)
        {
            var selected = reagent.Index == state.AutoReagentIndex;
            var button = new Button
            {
                Text = selected
                    ? Loc.GetString("syndicate-pai-ui-reagent-selected", ("name", reagent.Name))
                    : reagent.Name,
                HorizontalExpand = true,
            };
            var index = reagent.Index;
            button.OnPressed += _ => OnSelectReagent?.Invoke(index, true);
            _autoReagentList.AddChild(button);
        }
    }
}
