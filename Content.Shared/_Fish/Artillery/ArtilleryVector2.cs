using System;

namespace Content.Shared._Fish.Artillery;

[Serializable]
public struct ArtilleryVector2
{
    public float X;
    public float Y;

    public ArtilleryVector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static ArtilleryVector2 Zero => new(0, 0);

    public static ArtilleryVector2 operator +(ArtilleryVector2 a, ArtilleryVector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static ArtilleryVector2 operator -(ArtilleryVector2 a, ArtilleryVector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static ArtilleryVector2 operator *(ArtilleryVector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
    public static ArtilleryVector2 operator /(ArtilleryVector2 a, float scalar) => new(a.X / scalar, a.Y / scalar);

    public float Length => MathF.Sqrt(X * X + Y * Y);

    public override string ToString() => $"({X:F1}, {Y:F1})";
}