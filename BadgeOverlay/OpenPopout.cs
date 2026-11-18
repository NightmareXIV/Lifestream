using System.Numerics;

namespace BadgeOverlay;

internal sealed class OpenPopout
{
    public required string Key;
    public required PopoutDefinition Def;
    public required int Level;
    public required Side Preferred;
    public float CrossOffset;
    public PopoutAlignment Alignment;

    public Rect TriggerRect;
    public Rect Anchor;
    public Rect Bridge = Rect.Empty;

    public Vector2 Pos;
    public Vector2 Size;
    public int Frames;
    public double? HoverLostSince;

    public int StableFrames;
    public bool Ready;
    public int MisplacedStreak;
    public bool Measured => Ready;
    public Rect WindowRect => Rect.FromPosSize(Pos, Size);
    public bool Pinned;
}
