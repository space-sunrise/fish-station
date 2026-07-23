using Content.Client.UserInterface.Controls;
using Content.Shared._Fish.SecurityCrimeReport;
using Content.Shared._Sunrise.Laws;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Fish.SecurityCrimeReport;

/// <summary>
/// Opens SimpleRadialMenu from the mask InstantAction, then sends the selected article to the server.
/// FIsh edit
/// </summary>
public sealed class SecurityCrimeReportSystem : SharedSecurityCrimeReportSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private SimpleRadialMenu? _menu;

    private static readonly SpriteSpecifier.Rsi ArticleIcon =
        new(new ResPath("Clothing/Mask/gassecurity.rsi"), "icon");

    public override void Shutdown()
    {
        CloseMenu();
        base.Shutdown();
    }

    protected override void HandleOpenAction(Entity<SecurityCrimeReportComponent> ent, ref OpenSecurityCrimeReportEvent args)
    {
        base.HandleOpenAction(ent, ref args);

        var local = _player.LocalEntity;
        if (local == null || args.Performer != local.Value)
            return;

        OpenRadial(ent);
    }

    private void OpenRadial(Entity<SecurityCrimeReportComponent> ent)
    {
        CloseMenu();

        var buttons = new List<RadialMenuOptionBase>();
        foreach (var lawId in ent.Comp.Articles)
        {
            if (!_prototype.TryIndex(lawId, out var law) || string.IsNullOrEmpty(law.LawIdentifier))
                continue;

            var title = Loc.GetString(law.Title);
            var tooltip = Loc.GetString("security-crime-report-menu-article",
                ("code", law.LawIdentifier),
                ("title", title));

            buttons.Add(new RadialMenuActionOption<ProtoId<CorporateLawPrototype>>(id => OnSelected(ent.Owner, id), lawId)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(ArticleIcon),
                ToolTip = tooltip,
            });
        }

        if (buttons.Count == 0)
            return;

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(buttons);
        _menu.OpenOverMouseScreenPosition();
        _menu.OnClose += CloseMenu;
    }

    private void OnSelected(EntityUid device, ProtoId<CorporateLawPrototype> law)
    {
        RaiseNetworkEvent(new SecurityCrimeReportSelectedEvent(GetNetEntity(device), law));
        CloseMenu();
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.OnClose -= CloseMenu;
        _menu.Dispose();
        _menu = null;
    }
}
