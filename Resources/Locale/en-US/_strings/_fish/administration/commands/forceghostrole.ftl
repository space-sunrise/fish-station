cmd-forceghostrole-desc = Forcefully turns any entity into a ghost role by UID, including NPCs with an empty MindContainer.
cmd-forceghostrole-help = Usage: forceghostrole <entityUid> <name> <description> [<rules> | <rafflePrototype> [<rules>] | <initial> <extends> <max> [<rules>]]
cmd-forceghostrole-success = Made entity {$name} ({$uid}) a ghost role.
cmd-forceghostrole-failed = Failed to make entity {$uid} a ghost role.
cmd-forceghostrole-invalid-raffle = {$proto} is not a valid ghost role raffle settings prototype.
cmd-forceghostrole-invalid-duration = Raffle initial/extends/max must be positive numbers (seconds).
cmd-forceghostrole-initial-gt-max = Initial duration must be less than or equal to max duration.
