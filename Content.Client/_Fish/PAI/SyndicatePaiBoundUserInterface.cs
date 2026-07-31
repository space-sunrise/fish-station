using Content.Shared._Fish.PAI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Globalization;
using System.Linq;
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
        _window.OnSetTransferAmount += amount => SendMessage(new SyndicatePaiSetTransferAmountMessage(amount));
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

        if (_window != null)
        {
            _window.OnClose -= Close;
            _window.Close();
            _window = null;
        }
    }
}

public sealed class SyndicatePaiWindow : DefaultWindow
{
    public event Action? OnInject;
    public event Action? OnImprint;
    public event Action<int, bool>? OnSelectReagent;
    public event Action<float>? OnSetTransferAmount;
    public event Action<bool>? OnSetAutoEnabled;
    public event Action<float>? OnSetAutoThreshold;
    public event Action<string>? OnSetDirective;

    private readonly Label _carrierLabel;
    private readonly Label _masterLabel;
    private readonly Label _reagentLabel;
    private readonly Label _volumeLabel;
    private readonly Label _doseLabel;
    private readonly Label _directiveLabel;
    private readonly LineEdit _directiveEdit;
    private readonly BoxContainer _reagentList;
    private readonly BoxContainer _doseRow;
    private readonly Button _injectButton;

    private readonly Control _autoSection;
    private readonly Button _autoToggleButton;
    private readonly Label _autoReagentLabel;
    private readonly Label _autoVolumeLabel;
    private readonly Label _autoCooldownLabel;
    private readonly Label _autoThresholdLabel;
    private readonly Label _autoThresholdCurrentLabel;
    private readonly LineEdit _autoThresholdEdit;
    private readonly BoxContainer _autoReagentList;
    private readonly Label _autoReagentsHeader;
    private bool _autoEnabled;

    // Кэш структуры кнопок — не пересоздаём их при тиковом обновлении объёма
    private int _cachedManualReagentIndex = int.MinValue;
    private string _cachedManualReagentKey = string.Empty;
    private bool _cachedManualMedicalUnlocked;
    private int _cachedAutoReagentIndex = int.MinValue;
    private string _cachedAutoReagentKey = string.Empty;
    private float _cachedDose = float.NaN;
    private string _cachedDoseKey = string.Empty;
    private bool _cachedDoseMedicalUnlocked;

    public SyndicatePaiWindow()
    {
        Title = Loc.GetString("syndicate-pai-ui-title");
        MinSize = new Vector2(440, 680);
        SetSize = new Vector2(440, 680);

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
        _doseLabel = new Label();
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

        _doseRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

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
        root.AddChild(_doseLabel);
        root.AddChild(_doseRow);
        root.AddChild(actionRow);
        root.AddChild(new Label { Text = Loc.GetString("syndicate-pai-ui-reagents") });
        root.AddChild(_reagentList);

        _autoToggleButton = new Button();
        _autoToggleButton.OnPressed += _ => OnSetAutoEnabled?.Invoke(!_autoEnabled);

        _autoReagentLabel = new Label();
        _autoVolumeLabel = new Label();
        _autoCooldownLabel = new Label();
        _autoThresholdLabel = new Label { Text = Loc.GetString("syndicate-pai-ui-auto-threshold") };
        _autoThresholdCurrentLabel = new Label();
        _autoThresholdEdit = new LineEdit
        {
            PlaceHolder = "40",
            MinSize = new Vector2(72, 28),
            SetSize = new Vector2(72, 28),
            HorizontalExpand = false,
        };
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
            SeparationOverride = 8,
        };
        thresholdRow.AddChild(_autoThresholdCurrentLabel);
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
        _autoReagentsHeader = new Label { Text = Loc.GetString("syndicate-pai-ui-auto-reagents") };
        _autoSection.AddChild(_autoReagentsHeader);
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
        _doseLabel.Text = Loc.GetString("syndicate-pai-ui-dose-selected",
            ("amount", state.InjectTransferAmount.ToString("0.#")));

        _injectButton.Disabled = !state.CanInjectOwner;
        _injectButton.Visible = state.MedicalUnlocked;
        _doseLabel.Visible = state.MedicalUnlocked;
        _doseRow.Visible = state.MedicalUnlocked;

        RebuildDoseButtonsIfNeeded(state);
        RebuildManualReagentButtonsIfNeeded(state);

        if (state.SupplementalDirective != null && !_directiveEdit.HasKeyboardFocus())
            _directiveEdit.Text = state.SupplementalDirective;

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

        _autoThresholdCurrentLabel.Text = Loc.GetString("syndicate-pai-ui-auto-threshold-current",
            ("value", state.AutoHealthThreshold.ToString("0", CultureInfo.InvariantCulture)));

        if (!_autoThresholdEdit.HasKeyboardFocus())
            _autoThresholdEdit.Text = state.AutoHealthThreshold.ToString("0", CultureInfo.InvariantCulture);

        RebuildAutoReagentButtonsIfNeeded(state);
        var showAutoReagentPicker = state.AutoReagents.Count > 0;
        _autoReagentsHeader.Visible = showAutoReagentPicker;
        _autoReagentList.Visible = showAutoReagentPicker;
    }

    private void RebuildDoseButtonsIfNeeded(SyndicatePaiBoundUserInterfaceState state)
    {
        var doseKey = string.Join('|', state.InjectTransferAmounts.Select(a => a.ToString("0.#", CultureInfo.InvariantCulture)));
        var needsRebuild = _cachedDoseMedicalUnlocked != state.MedicalUnlocked
                           || !string.Equals(_cachedDoseKey, doseKey, StringComparison.Ordinal)
                           || float.IsNaN(_cachedDose)
                           || Math.Abs(_cachedDose - state.InjectTransferAmount) >= 0.01f;

        if (!needsRebuild)
            return;

        _cachedDoseMedicalUnlocked = state.MedicalUnlocked;
        _cachedDoseKey = doseKey;
        _cachedDose = state.InjectTransferAmount;

        _doseRow.RemoveAllChildren();
        foreach (var amount in state.InjectTransferAmounts)
        {
            var selected = Math.Abs(amount - state.InjectTransferAmount) < 0.01f;
            var button = new Button
            {
                Text = selected
                    ? Loc.GetString("syndicate-pai-ui-dose-selected-button", ("amount", amount.ToString("0.#")))
                    : Loc.GetString("syndicate-pai-ui-dose-button", ("amount", amount.ToString("0.#"))),
            };
            var dose = amount;
            button.OnPressed += _ => OnSetTransferAmount?.Invoke(dose);
            _doseRow.AddChild(button);
        }
    }

    private void RebuildManualReagentButtonsIfNeeded(SyndicatePaiBoundUserInterfaceState state)
    {
        var key = BuildReagentKey(state.Reagents);
        if (_cachedManualReagentIndex == state.CurrentReagentIndex
            && string.Equals(_cachedManualReagentKey, key, StringComparison.Ordinal)
            && _cachedManualMedicalUnlocked == state.MedicalUnlocked
            && _reagentList.ChildCount == state.Reagents.Count)
        {
            return;
        }

        _cachedManualReagentIndex = state.CurrentReagentIndex;
        _cachedManualReagentKey = key;
        _cachedManualMedicalUnlocked = state.MedicalUnlocked;

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
    }

    private void RebuildAutoReagentButtonsIfNeeded(SyndicatePaiBoundUserInterfaceState state)
    {
        var key = BuildReagentKey(state.AutoReagents);
        if (_cachedAutoReagentIndex == state.AutoReagentIndex
            && string.Equals(_cachedAutoReagentKey, key, StringComparison.Ordinal)
            && _autoReagentList.ChildCount == state.AutoReagents.Count)
        {
            return;
        }

        _cachedAutoReagentIndex = state.AutoReagentIndex;
        _cachedAutoReagentKey = key;

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

    private static string BuildReagentKey(List<SyndicatePaiReagentEntry> reagents)
    {
        if (reagents.Count == 0)
            return string.Empty;

        return string.Join(';', reagents.Select(r => $"{r.Index}:{r.Id}:{r.Name}"));
    }
}
