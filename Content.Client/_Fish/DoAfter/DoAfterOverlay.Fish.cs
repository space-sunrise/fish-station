using System.Numerics;

#pragma warning disable IDE0130 // Пространство имён совпадает с расширяемым vanilla-классом.
namespace Content.Client.DoAfter;

public sealed partial class DoAfterOverlay
{
    private const float EntranceDuration = 0.2f;
    private const float EntranceDistance = 0.25f;

    private static float GetEntranceProgress(TimeSpan elapsed)
    {
        var elapsedSeconds = elapsed.TotalSeconds;
        if (elapsedSeconds >= EntranceDuration)
            return 1f;

        var entranceRatio = Math.Clamp((float) (elapsedSeconds / EntranceDuration), 0f, 1f);
        var inverseRatio = 1f - entranceRatio;
        return 1f - inverseRatio * inverseRatio * inverseRatio;
    }

    private static Vector2 GetEntrancePosition(Vector2 position, float entranceProgress)
    {
        position.Y -= EntranceDistance * (1f - entranceProgress);
        return position;
    }
}
