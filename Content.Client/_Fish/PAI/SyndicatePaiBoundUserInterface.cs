using Content.Shared._Fish.PAI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
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
        _window.OnCycle += () => SendMessage(new SyndicatePaiCycleReagentMessage());
        _window.OnSelectReagent += index => SendMessage(new SyndicatePaiSelectReagentMessage(index));
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
    public event Action? OnCycle;
    public event Action? OnImprint;
    public event Action<int>? OnSelectReagent;
    public event Action<string>? OnSetDirective;

    private readonly Label _carrierLabel;
    private readonly Label _masterLabel;
    private readonly Label _reagentLabel;
    private readonly Label _volumeLabel;
    private readonly Label _directiveLabel;
    private readonly LineEdit _directiveEdit;
    private readonly BoxContainer _reagentList;

    public SyndicatePaiWindow()
    {
        Title = Loc.GetString("syndicate-pai-ui-title");
        MinSize = new Vector2(420, 460);
        SetSize = new Vector2(420, 460);

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

        var injectButton = new Button { Text = Loc.GetString("syndicate-pai-ui-inject") };
        injectButton.OnPressed += _ => OnInject?.Invoke();

        var cycleButton = new Button { Text = Loc.GetString("syndicate-pai-ui-cycle") };
        cycleButton.OnPressed += _ => OnCycle?.Invoke();

        var imprintButton = new Button { Text = Loc.GetString("syndicate-pai-ui-imprint") };
        imprintButton.OnPressed += _ => OnImprint?.Invoke();

        var setDirectiveButton = new Button { Text = Loc.GetString("syndicate-pai-ui-set-directive") };
        setDirectiveButton.OnPressed += _ => OnSetDirective?.Invoke(_directiveEdit.Text);

        var actionRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };
        actionRow.AddChild(injectButton);
        actionRow.AddChild(cycleButton);
        actionRow.AddChild(imprintButton);

        _reagentList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };

        root.AddChild(_carrierLabel);
        root.AddChild(_masterLabel);
        root.AddChild(_reagentLabel);
        root.AddChild(_volumeLabel);
        root.AddChild(actionRow);
        root.AddChild(new Label { Text = Loc.GetString("syndicate-pai-ui-reagents") });
        root.AddChild(_reagentList);
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
            };
            var index = reagent.Index;
            button.OnPressed += _ => OnSelectReagent?.Invoke(index);
            _reagentList.AddChild(button);
        }
    }
}
