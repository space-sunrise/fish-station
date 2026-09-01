using Content.Client.Camera;
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
    [Dependency] private readonly CameraRecoilSystem _cameraRecoil = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private const float ShakeRadius = 1.0f;
    private const float RecoilStrength = 8.0f;

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
                        TriggerRecoilIfNearby(uid);
                        break;
                }
            }
        }
    }

    private void TriggerRecoilIfNearby(EntityUid artillery)
    {
        var player = _playerManager.LocalPlayer?.ControlledEntity;
        if (player == null)
            return;

        var artilleryXform = Transform(artillery).MapPosition;
        var playerXform = Transform(player.Value).MapPosition;

        if (artilleryXform.MapId != playerXform.MapId)
            return;

        float dist = (artilleryXform.Position - playerXform.Position).Length();
        if (dist <= ShakeRadius)
        {
            _cameraRecoil.KickCamera(player.Value, new Vector2(RecoilStrength, 0f));
        }
    }
}