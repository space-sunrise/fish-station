using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._Fish.UserInterface.Crt;
using Content.Shared._Fish.Achievements;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client._Fish.Achievements;

/// <summary>
/// Окно списка достижений в CRT-стиле Fish.
/// </summary>
public sealed class AchievementWindow : DefaultWindow
{
    private readonly FishCrtLabel _summary;
    private readonly BoxContainer _categoryRow;
    private readonly BoxContainer _list;
    private readonly Dictionary<string, AchievementEntryControl> _entries = new();
    private readonly Dictionary<FishCrtActionButton, string?> _categoryButtons = new();

    private IPrototypeManager? _prototypes;
    private IReadOnlyDictionary<string, AchievementPlayerState>? _states;
    private string? _selectedCategory;

    public AchievementWindow()
    {
        Title = Loc.GetString("fish-achievements-window-title");
        MinSize = new Vector2(560, 460);
        SetSize = new Vector2(620, 520);

        var theme = new FishCrtThemeScope
        {
            Palette = FishCrtPalettePreset.Green,
            Effects = FishCrtEffects.HorizontalScanlines,
            HorizontalExpand = true,
            VerticalExpand = true,
            BorderThickness = 1,
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
            Margin = new Thickness(8),
        };

        _summary = new FishCrtLabel
        {
            Heading = true,
            Text = Loc.GetString("fish-achievements-summary", ("unlocked", 0), ("total", 0)),
        };

        root.AddChild(_summary);
        root.AddChild(new FishCrtSeparator { MinHeight = 2 });

        root.AddChild(new FishCrtLabel
        {
            Text = Loc.GetString("fish-achievements-categories-label"),
            Tone = FishCrtTone.Muted,
            TextFontSize = 11,
        });

        var categoryScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = false,
            MinHeight = 68,
            HScrollEnabled = true,
            VScrollEnabled = false,
        };

        _categoryRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };
        categoryScroll.AddChild(_categoryRow);
        root.AddChild(categoryScroll);

        var listPanel = new FishCrtPanel
        {
            Variant = FishCrtPanelVariant.Inset,
            Effects = FishCrtEffects.HorizontalScanlines,
            HorizontalExpand = true,
            VerticalExpand = true,
            BackgroundOpacity = 0.85f,
        };

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            Margin = new Thickness(4),
        };

        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };

        scroll.AddChild(_list);
        listPanel.AddChild(scroll);
        root.AddChild(listPanel);
        theme.AddChild(root);
        Contents.AddChild(theme);
    }

    public void Populate(
        IPrototypeManager prototypes,
        IReadOnlyDictionary<string, AchievementPlayerState> states)
    {
        _prototypes = prototypes;
        _states = states;

        RebuildCategories();
        RebuildList();
    }

    public void UpdateEntry(AchievementPlayerState state)
    {
        if (_entries.TryGetValue(state.AchievementId, out var control) && _prototypes != null)
        {
            if (_prototypes.TryIndex<AchievementPrototype>(state.AchievementId, out var proto))
                control.Update(proto, state);
        }

        UpdateSummary();
    }

    private void RebuildCategories()
    {
        _categoryRow.RemoveAllChildren();
        _categoryButtons.Clear();

        if (_prototypes == null)
            return;

        AddCategoryButton(null, Loc.GetString("fish-achievements-category-all"), FishCrtIcons.Users);

        foreach (var category in _prototypes.EnumeratePrototypes<AchievementCategoryPrototype>()
                     .OrderBy(c => c.Order)
                     .ThenBy(c => c.ID))
        {
            AddCategoryButton(category.ID, Loc.GetString(category.Name), FishCrtIcons.Medal);
        }

        RefreshCategorySelection();
    }

    private void AddCategoryButton(string? categoryId, string text, string iconState)
    {
        var button = new FishCrtActionButton
        {
            Text = text,
            IconState = iconState,
            Variant = FishCrtButtonVariant.Outline,
            ContentAlignment = FishCrtContentAlignment.Center,
            MinHeight = 28,
            ToolTip = text,
        };

        button.OnPressed += _ =>
        {
            _selectedCategory = categoryId;
            RefreshCategorySelection();
            RebuildList();
        };

        _categoryButtons[button] = categoryId;
        _categoryRow.AddChild(button);
    }

    private void RefreshCategorySelection()
    {
        foreach (var (button, categoryId) in _categoryButtons)
        {
            var selected = categoryId == _selectedCategory;
            button.Selected = selected;
            button.Variant = selected ? FishCrtButtonVariant.Filled : FishCrtButtonVariant.Outline;
        }
    }

    private void RebuildList()
    {
        _list.RemoveAllChildren();
        _entries.Clear();

        if (_prototypes == null || _states == null)
            return;

        var achievements = _prototypes.EnumeratePrototypes<AchievementPrototype>()
            .Where(a => _selectedCategory == null || a.Category == _selectedCategory)
            .OrderBy(a => a.Order)
            .ThenBy(a => a.ID);

        foreach (var proto in achievements)
        {
            _states.TryGetValue(proto.ID, out var state);
            var control = new AchievementEntryControl();
            control.Update(proto, state);
            _entries[proto.ID] = control;
            _list.AddChild(control);
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (_prototypes == null || _states == null)
            return;

        var total = _prototypes.EnumeratePrototypes<AchievementPrototype>().Count();
        var unlocked = _states.Values.Count(s => s.Unlocked);
        _summary.Text = Loc.GetString("fish-achievements-summary", ("unlocked", unlocked), ("total", total));
        _summary.Tone = unlocked > 0 ? FishCrtTone.Good : FishCrtTone.Default;
    }
}

/// <summary>
/// Карточка одного достижения в CRT-панели.
/// </summary>
public sealed class AchievementEntryControl : BoxContainer
{
    private readonly FishCrtPanel _panel;
    private readonly FishCrtLabel _title;
    private readonly FishCrtLabel _description;
    private readonly ProgressBar _progress;
    private readonly FishCrtLabel _progressLabel;
    private readonly FishCrtIcon _statusIcon;

    public AchievementEntryControl()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        _panel = new FishCrtPanel
        {
            HorizontalExpand = true,
            Variant = FishCrtPanelVariant.Surface,
            Effects = FishCrtEffects.None,
            BackgroundOpacity = 0.72f,
            BorderThickness = 1,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
            Margin = new Thickness(8, 6),
        };

        _statusIcon = new FishCrtIcon
        {
            IconState = FishCrtIcons.Medal,
            SetWidth = 22,
            SetHeight = 22,
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(0, 2, 0, 0),
        };

        var textColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 2,
        };

        _title = new FishCrtLabel { Heading = true };
        _description = new FishCrtLabel { HorizontalExpand = true, Tone = FishCrtTone.Muted };
        _progressLabel = new FishCrtLabel { Tone = FishCrtTone.Muted, TextFontSize = 11 };
        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Visible = false,
            HorizontalExpand = true,
            MinHeight = 10,
        };

        textColumn.AddChild(_title);
        textColumn.AddChild(_description);
        textColumn.AddChild(_progress);
        textColumn.AddChild(_progressLabel);

        row.AddChild(_statusIcon);
        row.AddChild(textColumn);
        _panel.AddChild(row);
        AddChild(_panel);
    }

    public void Update(AchievementPrototype proto, AchievementPlayerState state)
    {
        var unlocked = state.Unlocked;
        _title.Text = Loc.GetString(proto.Name);
        _title.Tone = unlocked ? FishCrtTone.Good : FishCrtTone.Default;

        if (proto.Secret && !unlocked)
        {
            _description.Text = Loc.GetString(proto.SecretDescription ?? "fish-achievements-secret-placeholder");
            _statusIcon.IconState = FishCrtIcons.Warning;
            _statusIcon.Tone = FishCrtTone.Warning;
        }
        else
        {
            _description.Text = Loc.GetString(proto.Description);
            _statusIcon.IconState = FishCrtIcons.Medal;
            _statusIcon.Tone = unlocked ? FishCrtTone.Good : FishCrtTone.Muted;
        }

        var target = System.Math.Max(1, proto.ProgressTarget);
        if (target > 1)
        {
            _progress.Visible = true;
            _progress.MaxValue = target;
            _progress.Value = System.Math.Clamp(state.Progress, 0, target);
            _progressLabel.Text = $"{state.Progress}/{target}";
            _progressLabel.Visible = true;
            _progressLabel.Tone = unlocked ? FishCrtTone.Good : FishCrtTone.Muted;
        }
        else
        {
            _progress.Visible = false;
            _progressLabel.Visible = false;
        }

        _panel.Variant = unlocked ? FishCrtPanelVariant.Surface : FishCrtPanelVariant.Inset;
        _description.Tone = unlocked ? FishCrtTone.Default : FishCrtTone.Muted;
    }
}
