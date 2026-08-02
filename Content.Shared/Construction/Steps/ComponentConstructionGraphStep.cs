using Content.Shared.Examine;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class ComponentConstructionGraphStep : ArbitraryInsertConstructionGraphStep
    {
        [DataField("component")] public string Component { get; private set; } = string.Empty;

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            // Fish-start
            if (string.IsNullOrEmpty(Component))
                return false;

            if (!compFactory.TryGetRegistration(Component, out var registration))
                return false;

            return entityManager.HasComponent(uid, registration.Type);
            // Fish-end
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            examinedEvent.PushMarkup(string.IsNullOrEmpty(Name)
                ? Loc.GetString(
                    "construction-insert-entity-with-component",
                    ("componentName", Component))// Terrible.
                : Loc.GetString(
                    "construction-insert-exact-entity",
                    ("entityName", Loc.GetString(Name))));
        }
    }
}
