using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.AntiPidor;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Host)]
    public sealed class AntiPidorCommand : LocalizedCommands
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

        public override string Command => "antipidor";
        public override string Description => "Управляет анти-пидор системой";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var _antiPidor = _entityManager.System<AntiPidorSystem>();
            var pidorWords = _antiPidor.pidorWords;

            switch (args.Length)
            {
                case 0:
                    shell.WriteLine("Реестр пидорских слов: " + _antiPidor.GetWords(pidorWords));
                    shell.WriteLine("Дефолтный реестр пидорских слов: " + _antiPidor.GetWords(_antiPidor.GetDefaultPidorWords()));
                    break;
                case 1:
                    switch (args[0])
                    {
                        case "molchanka":
                            shell.WriteLine(_antiPidor.molchanka.ToString());
                            break;
                        case "add":
                            shell.WriteLine("Введи пидорское слово");
                            break;
                        case "remove":
                            shell.WriteLine("Введи слово");
                            break;
                        case "default":
                            _antiPidor.pidorWords = _antiPidor.GetDefaultPidorWords();
                            shell.WriteLine("Реестр пидорских слов был откатан");

                            log(shell,
                                $"{shell.Player?.Name} откатил реестр пидорских слов",
                                $"Консолька откатила реестр пидорских слов");
                            break;
                    }
                    break;
                case 2:
                    if (args[0] == "add")
                    {
                        if (!(pidorWords.Contains(args[1])))
                        {
                            shell.WriteLine("В реестр пидорских слов добавлено слово: ");
                            shell.WriteLine(args[1]);

                            pidorWords.Add(args[1]);

                            log(shell,
                                $"{shell.Player?.Name} добавил в реестр пидорских слов: {args[1]}",
                                $"Консолька добавила в реестр пидорских слов: {args[1]}");
                        }
                    }
                    else if (args[0] == "remove")
                    {
                        if (pidorWords.Contains(args[1]))
                        {
                            shell.WriteLine("Из реестра пидорских слов удалено слово: ");
                            shell.WriteLine(args[1]);

                            pidorWords.Remove(args[1]);

                            log(shell,
                                $"{shell.Player?.Name} убрал из реестра пидорских слов: {args[1]}",
                                $"Консолька убрала из реестра пидорских слов: {args[1]}");
                        }
                    }
                    else if (args[0] == "molchanka")
                    {
                        switch (args[1])
                        {
                            case "true":
                                _antiPidor.molchanka = true;
                                shell.WriteLine("Режим молчанки был включен");

                                log(shell,
                                    $"{shell.Player?.Name} включил режим молчанки",
                                    $"Консолька включила режим молчанки");
                                break;
                            case "false":
                                _antiPidor.molchanka = false;
                                shell.WriteLine("Режим молчанки был выключен");

                                log(shell,
                                    $"{shell.Player?.Name} выключил режим молчанки",
                                    $"Консолька выключила режим молчанки");
                                break;
                        }
                    }
                    break;
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            var _antiPidor = _entityManager.System<AntiPidorSystem>();
            var pidorWords = _antiPidor.pidorWords;

            List<string> options = new List<string> {"add", "remove", "molchanka", "default"};

            switch (args.Length)
            {
                case 1:
                    return CompletionResult.FromHintOptions(options, "Без параметров выводит пидорские слова");
                case 2:
                    options = pidorWords;
                    switch (args[0])
                    {
                        case "remove":
                            return CompletionResult.FromHintOptions(options, "Убери слово");
                        case "add":
                            return CompletionResult.FromHint("Добавь пидорское слово");
                        case "molchanka":
                            options = new List<string> {"true", "false"};
                            return CompletionResult.FromHintOptions(options, "true - гиб за любое слово");
                        case "default":
                            return CompletionResult.FromHint("Откатывает реестр пидорских слов");
                    }
                    break;
            }
            return CompletionResult.Empty;
        }

        private void log(IConsoleShell shell, string client, string server)
        {
            if (shell.Player is null)
            {
                _adminLogger.Add(LogType.AdminCommands, LogImpact.Extreme,
                    $"{server}");
            }
            else
            {
                _adminLogger.Add(LogType.AdminCommands, LogImpact.Extreme,
                    $"{client}");
            }
        }
    }
}
