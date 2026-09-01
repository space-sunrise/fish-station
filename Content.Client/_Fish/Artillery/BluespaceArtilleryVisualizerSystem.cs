using Content.Shared._Fish.Artillery;
using Content.Shared.Eye;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.Blinding.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Client._Fish.Artillery;

public sealed class BluespaceArtilleryVisualizerSystem : VisualizerSystem<BluespaceArtilleryComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedEyeSystem _eyeSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float ShakeRadius = 5.0f;
    private const float ShakeDuration = 0.3f;
    private const float ShakeAmplitude = 4.0f;

    protected override void OnAppearanceChange(EntityUid uid, BluespaceArtilleryComponent component, ref AppearanceChangeEvent args)
    {
        if (_appearance.TryGetData(uid, BluespaceArtilleryVisuals.VisualState, out BluespaceArtilleryVisualState state))
        {
            if (TryComp<SpriteComponent>(uid, out var sprite))
            {
                switch (state)
                {
                    case BluespaceArtilleryVisualState.Idle:
                        sprite.LayerSetState(0, "idle");
                        break;
                    case BluespaceArtilleryVisualState.Charging:
                        sprite.LayerSetState(0, "charging");
                        break;
                    case BluespaceArtilleryVisualState.Fire:
                        sprite.LayerSetState(0, "fire");
                        TriggerScreenShake(uid);
                        break;
                }
            }
        }
    }

    private void TriggerScreenShake(EntityUid artillery)
    {
        var player = _playerManager.LocalPlayer?.ControlledEntity;
        if (player == null)
            return;

        if (!TryComp<EyeComponent>(player, out var eye))
            return;

        var artilleryXform = Transform(artillery).MapPosition;
        var playerXform = Transform(player.Value).MapPosition;
        if (artilleryXform.MapId != playerXform.MapId ||
            (artilleryXform.Position - playerXform.Position).Length() > ShakeRadius)
            return;

        var startTime = _timing.CurTime;
        var endTime = startTime + TimeSpan.FromSeconds(ShakeDuration);

        void ShakeStep()
        {
            if (_timing.CurTime >= endTime)
            {
                _eyeSystem.SetOffset(player.Value, Vector2.Zero, eye);
                return;
            }

            var offset = new Vector2(
                _random.NextFloat(-ShakeAmplitude, ShakeAmplitude),
                _random.NextFloat(-ShakeAmplitude, ShakeAmplitude)
            );
            _eyeSystem.SetOffset(player.Value, offset, eye);
            Timer.Spawn(TimeSpan.FromSeconds(0.05), ShakeStep);
        }

        ShakeStep();
    }
}
