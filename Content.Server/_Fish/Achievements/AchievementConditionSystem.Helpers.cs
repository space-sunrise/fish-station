using Content.Shared._Fish.Achievements;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._Fish.Achievements;

public sealed partial class AchievementConditionSystem
{
    private static readonly ProtoId<TagPrototype> NpcBossTag = "NpcBoss";

    private string? GetPrototypeId(EntityUid uid)
    {
        return MetaData(uid).EntityPrototype?.ID;
    }

    private AchievementTriggerContext WithPrototype(AchievementTriggerContext ctx, EntityUid entity)
    {
        return ctx with { EntityPrototypeId = GetPrototypeId(entity) };
    }

    private AchievementTriggerContext WithVerifiedTag(AchievementTriggerContext ctx, string tagId)
    {
        return ctx with { VerifiedTag = tagId };
    }
}
