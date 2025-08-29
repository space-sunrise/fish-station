using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Bible.Components;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Chat;
using Content.Shared.Prayer;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Server.Prayer;
using Content.Shared.Pulsedemon;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Server.Actions;

namespace Content.Server.Pulsedemon;
/// <summary>
/// System to handle subtle messages and praying
/// </summary>
/// <remarks>
/// Rain is a professional developer and this did not take 2 PRs to fix subtle messages
/// </remarks>
public sealed class Pulsedemon : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<PulsedemonComponent, PulsedemonDemonicWhisper>(DemonicWhisper);
        SubscribeLocalEvent<PulsedemonComponent,UseAttemptEvent >(ActionOnInteract);
    }

    private void ActionOnInteract(EntityUid uid
    { 
                               _quickDialog.OpenDialog(target, "Subtle Message", "Message", "Popup Message", (string message, string popupMessage) =>
                        {
                            _prayerSystem.SendSubtleMessage(string messageString, string popupMessage);
                        });
    }





}