using Content.Shared._Fish.Achievements;
using Content.Shared.Medical;
using Content.Shared.Starlight.Medical.Surgery.Events;
using Content.Shared.Starlight.Medical.Surgery.Steps;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Player;

namespace Content.Server._Fish.Achievements;

public sealed partial class AchievementConditionSystem
{
    partial void InitializeMedical()
    {
        SubscribeLocalEvent<TargetDefibrillatedEvent>(OnTargetDefibrillated);
        SubscribeLocalEvent<SurgeryStepComponent, SurgeryStepCompleteEvent>(OnSurgeryStepComplete);
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);
    }

    private void OnTargetDefibrillated(ref TargetDefibrillatedEvent ev)
    {
        if (!TryComp<ActorComponent>(ev.User, out var actor))
            return;

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.Defibrillate,
            new AchievementTriggerContext(
                EntityPrototypeId: GetPrototypeId(ev.Defibrillator),
                EventKey: $"defib:{GetNetEntity(ev.User)}:{GetNetEntity(ev.Defibrillator.Owner)}:{_timing.CurTick}"));
    }

    private void OnSurgeryStepComplete(Entity<SurgeryStepComponent> ent, ref SurgeryStepCompleteEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.Surgery,
            new AchievementTriggerContext(
                EntityPrototypeId: args.StepProto,
                EventKey: $"surgery:{args.StepProto}:{GetNetEntity(args.Body)}:{_timing.CurTick}"));
    }

    private void OnGunShot(Entity<GunComponent> gun, ref GunShotEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var weaponProto = GetPrototypeId(gun);
        _ = _achievements.ContributeAsync(
            actor.PlayerSession,
            AchievementConditionKeys.GunShot,
            new AchievementTriggerContext(
                WeaponPrototypeId: weaponProto,
                EventKey: $"gun:{weaponProto}:{_timing.CurTick}:{actor.PlayerSession.UserId}"));
    }
}
