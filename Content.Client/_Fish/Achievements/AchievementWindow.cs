using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._Fish.UserInterface.Crt;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Shared._Fish.Achievements;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Fish.Achievements;

/// <summary>
/// Окно достижений: мягкий скруглённый Slate-стиль ближе к современному Nano.
/// </summary>
public sealed class AchievementWindow : FancyWindow
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
        MinSize = new Vector2(580, 480);
        SetSize = new Vector2(660, 560);

        var theme = new FishCrtThemeScope
        {
            Palette = FishCrtPalettePreset.Slate,
            Effects = FishCrtEffects.None,
            HorizontalExpand = true,
            VerticalExpand = true,
            BorderThickness = 0,
            BackgroundOpacity = 0,
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 10,
            Margin = new Thickness(12, 10, 12, 12),
        };

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
            Margin = new Thickness(2, 0, 2, 2),
        };

        _summary = new FishCrtLabel
        {
            Heading = true,
            TextFontSize = 16,
            Text = Loc.GetString("fish-achievements-summary", ("unlocked", 0), ("total", 0)),
        };
        header.AddChild(_summary);
        header.AddChild(new FishCrtLabel
        {
            Text = Loc.GetString("fish-achievements-categories-label"),
            Tone = FishCrtTone.Muted,
            TextFontSize = 11,
            Margin = new Thickness(0, 6, 0, 0),
        });
        root.AddChild(header);

        var categoryScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = false,
            MinHeight = 44,
            HScrollEnabled = true,
            VScrollEnabled = false,
        };

        _categoryRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 6,
        };
        categoryScroll.AddChild(_categoryRow);
        root.AddChild(categoryScroll);

        var listPanel = new FishCrtPanel
        {
            Variant = FishCrtPanelVariant.Inset,
            Effects = FishCrtEffects.None,
            Rounded = true,
            HorizontalExpand = true,
            VerticalExpand = true,
            BackgroundOpacity = 0.42f,
            BorderThickness = 0,
        };

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            Margin = new Thickness(10, 10, 10, 10),
        };

        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };

        scroll.AddChild(_list);
        listPanel.AddChild(scroll);
        root.AddChild(listPanel);
        theme.AddChild(root);
        ContentsContainer.AddChild(theme);
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

        AddCategoryButton(null, Loc.GetString("fish-achievements-category-all"), null);

        string? firstCategory = null;
        foreach (var category in _prototypes.EnumeratePrototypes<AchievementCategoryPrototype>()
                     .OrderBy(c => c.Order)
                     .ThenBy(c => c.ID))
        {
            firstCategory ??= category.ID;
            AddCategoryButton(category.ID, Loc.GetString(category.Name), null);
        }

        // Не открываем «Все» по умолчанию — иначе ~500 контролов за раз.
        if (_selectedCategory == null && firstCategory != null)
            _selectedCategory = firstCategory;

        RefreshCategorySelection();
    }

    private void AddCategoryButton(string? categoryId, string text, string? iconState)
    {
        var button = new FishCrtActionButton
        {
            Text = text,
            IconState = iconState,
            Variant = FishCrtButtonVariant.Outline,
            ContentAlignment = FishCrtContentAlignment.Center,
            MinHeight = 32,
            ContentMargin = new Thickness(14, 6),
            TextFontSize = 12,
            ToolTip = text,
        };
        button.Background.Rounded = true;
        button.Background.BorderThickness = 0;
        button.Background.Effects = FishCrtEffects.None;

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
            button.Background.BorderThickness = selected ? 0 : 1;
        }
    }

    private void RebuildList()
    {
        _list.RemoveAllChildren();
        _entries.Clear();

        if (_prototypes == null || _states == null)
            return;

        IEnumerable<AchievementPrototype> achievements = _prototypes
            .EnumeratePrototypes<AchievementPrototype>()
            .Where(a => _selectedCategory == null || a.Category == _selectedCategory);

        // «Все»: без сотен manual-заглушек — только живые условия и уже начатый прогресс.
        if (_selectedCategory == null)
        {
            achievements = achievements.Where(a =>
            {
                if (a.Condition != AchievementConditionKeys.Manual)
                    return true;

                return _states.TryGetValue(a.ID, out var st) && (st.Unlocked || st.Progress > 0);
            });
        }

        foreach (var proto in achievements.OrderBy(a => a.Order).ThenBy(a => a.ID))
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
/// Карточка достижения: скруглённая мягкая панель.
/// </summary>
public sealed class AchievementEntryControl : BoxContainer
{
    private static readonly ResPath ProgressBgPath = new("/Textures/Interface/Nano/rounded_button.svg.96dpi.png");

    private readonly FishCrtPanel _panel;
    private readonly FishCrtLabel _title;
    private readonly FishCrtLabel _description;
    private readonly ProgressBar _progress;
    private readonly FishCrtLabel _progressLabel;
    private readonly FishCrtIcon _statusIcon;
    private readonly StyleBoxTexture _progressBg;
    private readonly StyleBoxTexture _progressFg;

    public AchievementEntryControl()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        var cache = IoCManager.Resolve<IResourceCache>();
        _progressBg = new StyleBoxTexture
        {
            Texture = cache.GetTexture(ProgressBgPath),
            Modulate = Color.FromHex("#1A1D24"),
        };
        _progressBg.SetPatchMargin(StyleBox.Margin.All, 10);
        _progressFg = new StyleBoxTexture
        {
            Texture = cache.GetTexture(ProgressBgPath),
            Modulate = Color.FromHex("#6FBE84"),
        };
        _progressFg.SetPatchMargin(StyleBox.Margin.All, 10);

        _panel = new FishCrtPanel
        {
            HorizontalExpand = true,
            Variant = FishCrtPanelVariant.Surface,
            Effects = FishCrtEffects.None,
            Rounded = true,
            BackgroundOpacity = 0.78f,
            BorderThickness = 0,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 12,
            Margin = new Thickness(14, 12),
        };

        _statusIcon = new FishCrtIcon
        {
            IconState = FishCrtIcons.Medal,
            SetWidth = 22,
            SetHeight = 22,
            VerticalAlignment = VAlignment.Top,
            Margin = new Thickness(0, 2, 0, 0),
            Tone = FishCrtTone.Muted,
        };

        var textColumn = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };

        _title = new FishCrtLabel { Heading = true, TextFontSize = 13 };
        _description = new FishCrtLabel { HorizontalExpand = true, Tone = FishCrtTone.Muted, TextFontSize = 11 };
        _progressLabel = new FishCrtLabel { Tone = FishCrtTone.Muted, TextFontSize = 11 };
        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Visible = false,
            HorizontalExpand = true,
            MinHeight = 10,
            Margin = new Thickness(0, 2, 0, 0),
        };
        _progress.BackgroundStyleBoxOverride = _progressBg;
        _progress.ForegroundStyleBoxOverride = _progressFg;

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
        var showProgress = target > 1 && proto.Condition != AchievementConditionKeys.Manual;
        if (showProgress)
        {
            _progress.Visible = true;
            _progress.MaxValue = target;
            _progress.Value = System.Math.Clamp(state.Progress, 0, target);
            _progressLabel.Text = $"{state.Progress}/{target}";
            _progressLabel.Visible = true;
            _progressLabel.Tone = unlocked ? FishCrtTone.Good : FishCrtTone.Muted;
            _progressFg.Modulate = unlocked ? Color.FromHex("#6FBE84") : Color.FromHex("#7A8BB0");
        }
        else
        {
            _progress.Visible = false;
            _progressLabel.Visible = false;
        }

        _panel.BackgroundOpacity = unlocked ? 0.88f : 0.68f;
        _description.Tone = unlocked ? FishCrtTone.Default : FishCrtTone.Muted;
    }
}
