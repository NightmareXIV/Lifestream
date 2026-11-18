namespace BadgeOverlay;

public static class OverlayColor
{
    public static uint Rgba(byte r, byte g, byte b, byte a = 255)
    {
        return ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;
    }
}
