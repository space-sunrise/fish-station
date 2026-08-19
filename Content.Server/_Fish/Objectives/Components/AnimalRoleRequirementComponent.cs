using Content.Server._Fish.Objectives.Systems;

namespace Content.Server._Fish.Objectives.Components;

[RegisterComponent, Access(typeof(AnimalRoleRequirementSystem))]
public sealed partial class AnimalRoleRequirementComponent : Component;
