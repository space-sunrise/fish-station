using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._Fish.Achievements;

/// <summary>
/// Админ-команда для ручных/особых достижений.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class AchievementGrantCommand : IConsoleCommand
{
    [Dependency] private readonly AchievementManager _achievements = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public string Command => "achgrant";
    public string Description => "Выдать достижение игроку (admin).";
    public string Help => "achgrant <player> <achievementId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteError($"Игрок '{args[0]}' не найден.");
            return;
        }

        _ = GrantAsync(shell, session, args[1]);
    }

    private async Task GrantAsync(IConsoleShell shell, ICommonSession session, string achievementId)
    {
        var ok = await _achievements.TryForceUnlockAsync(session, achievementId);
        shell.WriteLine(ok
            ? $"Выдано {achievementId} → {session.Name}"
            : $"Не удалось выдать {achievementId} (уже есть / нет в кеше / неизвестно).");
    }
}
