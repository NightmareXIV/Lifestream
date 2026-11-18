using Dalamud.Interface.ManagedFontAtlas;
using System;

namespace BadgeOverlay;
#nullable enable

public sealed class BadgeBarStyle
{
    public BadgeDock Dock = BadgeDock.Left;
    public BadgeSizing Sizing = BadgeSizing.FitText;
    public float UniformLength = 0f;
    public BadgeAnchor Anchor = BadgeAnchor.Start;
    public PopoutAlignment PopoutAlignment = PopoutAlignment.Center;
    public bool KeepOrderWhenAnchoredToEnd = false;
    public VerticalTextRotation TextRotation = VerticalTextRotation.Degrees270;
    public float Spacing = 4f;
    public float PaddingStart = 8f;
    public float PaddingEnd = 8f;
    public float ScreenMargin = 0f;
    public float InnerPadding = 8f;
    public float CrossPadding = 4f;
    public float Thickness = 0f;
    public float Rounding = 4f;
    public float BorderThickness = 1f;
    public bool UseCustomColors = false;
    public uint BackgroundColor = OverlayColor.Rgba(32, 34, 40, 230);
    public uint HoveredColor = OverlayColor.Rgba(52, 56, 66, 240);
    public uint ActiveColor = OverlayColor.Rgba(70, 100, 170, 245);
    public uint TextColor = OverlayColor.Rgba(235, 235, 240);
    public uint HeaderTextColor = OverlayColor.Rgba(100, 150, 225);
    public uint BorderColor = OverlayColor.Rgba(90, 94, 104);
    [NonSerialized] public bool ScaleWithGlobalScale = true;
    [NonSerialized] public Func<IFontHandle?>? GetFont = null;
}
