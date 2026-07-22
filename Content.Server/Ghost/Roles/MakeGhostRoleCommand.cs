using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Shared.Console;

namespace Content.Server.Ghost.Roles
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class MakeGhostRoleCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "makeghostrole";
        public string Description => "Turns an entity into a ghost role.";
        public string Help => $"Usage: {Command} <entity uid> <name> <description> [<rules>]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3 || args.Length > 4)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine($"{args[0]} is not a valid entity uid.");
                return;
            }

            if (!_entManager.TryGetComponent(uid, out MetaDataComponent? metaData))
            {
                shell.WriteLine($"No entity found with uid {uid}");
                return;
            }

            // Fish edit start - TryGetMind вместо HasMind (устаревший Mind UID), MakeSentient как у cognizine
            var mindSystem = _entManager.System<SharedMindSystem>();
            var ghostRoleSystem = _entManager.System<GhostRoleSystem>();

            if (mindSystem.TryGetMind(uid.Value, out _, out _))
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a mind.");
                return;
            }

            if (_entManager.TryGetComponent(uid, out GhostRoleComponent? _))
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostRoleComponent)}");
                return;
            }

            if (_entManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
            {
                shell.WriteLine($"Entity {metaData.EntityName} with id {uid} already has a {nameof(GhostTakeoverAvailableComponent)}");
                return;
            }

            var name = args[1];
            var description = args[2];
            var rules = args.Length >= 4 ? args[3] : Loc.GetString("ghost-role-component-default-rules");

            if (!ghostRoleSystem.TryForceMakeGhostRole(
                    uid.Value,
                    name,
                    description,
                    rules,
                    makeSentient: true,
                    allowMovement: true,
                    allowSpeech: true,
                    ejectExistingMind: false))
            {
                shell.WriteLine($"Failed to make entity {metaData.EntityName} a ghost role.");
                return;
            }
            // Fish edit end

            shell.WriteLine($"Made entity {metaData.EntityName} a ghost role.");
        }
    }
}
