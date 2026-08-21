using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared._Fish.Achievements;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client._Fish.Achievements;

/// <summary>
/// Окно списка достижений с категориями и фильтрацией.
/// </summary>
public sealed class AchievementWindow : DefaultWindow
{
    private readonly ScrollContainer _scroll;
    private readonly BoxContainer _list;
    private readonly Label _summary;
    private readonly OptionButton _categoryFilter;
    private readonly Dictionary<string, AchievementEntryControl> _entries = new();

    private IPrototypeManager? _prototypes;
    private IReadOnlyDictionary<string, AchievementPlayerState>? _states;
    private string? _selectedCategory;

    public AchievementWindow()
    {
        Title = Loc.GetString("fish-achievements-window-title");
        MinSize = new Vector2(520, 420);
        SetSize = new Vector2(560, 480);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
        };

        _summary = new Label { Text = Loc.GetString("fish-achievements-summary", ("unlocked", 0), ("total", 0)) };
        _categoryFilter = new OptionButton();
        _categoryFilter.OnItemSelected += args =>
        {
            _categoryFilter.SelectId(args.Id);
            _selectedCategory = args.Id == 0 ? null : (string?) _categoryFilter.GetItemMetadata(args.Id);
            RebuildList();
        };

        _scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };
        _scroll.AddChild(_list);

        root.AddChild(_summary);
        root.AddChild(_categoryFilter);
        root.AddChild(_scroll);
        Contents.AddChild(root);
    }

    public void Populate(
        IPrototypeManager prototypes,
        IReadOnlyDictionary<string, AchievementPlayerState> states)
    {
        _prototypes = prototypes;
        _states = states;

        _categoryFilter.Clear();
        _categoryFilter.AddItem(Loc.GetString("fish-achievements-category-all"), 0);
        var id = 1;
        foreach (var category in prototypes.EnumeratePrototypes<AchievementCategoryPrototype>().OrderBy(c => c.Order).ThenBy(c => c.ID))
        {
            _categoryFilter.AddItem(Loc.GetString(category.Name), id);
            _categoryFilter.SetItemMetadata(id, category.ID);
            id++;
        }

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
    }
}

public sealed class AchievementEntryControl : PanelContainer
{
    private readonly Label _title;
    private readonly Label _description;
    private readonly ProgressBar? _progress;
    private readonly Label _progressLabel;

    public AchievementEntryControl()
    {
        HorizontalExpand = true;
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 2,
            Margin = new Thickness(6),
        };

        _title = new Label { StyleClasses = { "LabelHeading" } };
        _description = new Label { HorizontalExpand = true };
        _progressLabel = new Label();
        _progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Visible = false,
            HorizontalExpand = true,
            MinHeight = 12,
        };

        box.AddChild(_title);
        box.AddChild(_description);
        box.AddChild(_progress);
        box.AddChild(_progressLabel);
        AddChild(box);
    }

    public void Update(AchievementPrototype proto, AchievementPlayerState state)
    {
        var unlocked = state.Unlocked;
        _title.Text = Loc.GetString(proto.Name);

        if (proto.Secret && !unlocked)
        {
            _description.Text = Loc.GetString(proto.SecretDescription ?? "fish-achievements-secret-placeholder");
        }
        else
        {
            _description.Text = Loc.GetString(proto.Description);
        }

        var target = System.Math.Max(1, proto.ProgressTarget);
        if (target > 1)
        {
            _progress!.Visible = true;
            _progress.MaxValue = target;
            _progress.Value = System.Math.Clamp(state.Progress, 0, target);
            _progressLabel.Text = $"{state.Progress}/{target}";
            _progressLabel.Visible = true;
        }
        else
        {
            _progress!.Visible = false;
            _progressLabel.Visible = false;
        }

        Modulate = unlocked ? Color.White : Color.FromHex("#888888");
    }
}
