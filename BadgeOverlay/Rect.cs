using System.Numerics;

namespace BadgeOverlay;

internal readonly record struct Rect(Vector2 Min, Vector2 Max)
{
    public static readonly Rect Empty = new(Vector2.Zero, Vector2.Zero);
    public Vector2 Size => Max - Min;
    public static Rect FromPosSize(Vector2 pos, Vector2 size)
    {
        return new(pos, pos + size);
    }

    public bool Contains(Vector2 p)
    {
        return p.X >= Min.X && p.X < Max.X && p.Y >= Min.Y && p.Y < Max.Y;
    }
}
