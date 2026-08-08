using Content.Shared._Fish.Research.Components;
using Content.Shared.Examine;

namespace Content.Shared._Fish.Research;

/// <summary>
/// Shared helpers for research-analyzable items (examine value).
/// </summary>
public sealed class ResearchAnalyzableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResearchAnalyzableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ResearchAnalyzableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Points <= 0)
            return;

        args.PushMarkup(Loc.GetString("research-analyzable-examine", ("points", ent.Comp.Points)));
    }
}
