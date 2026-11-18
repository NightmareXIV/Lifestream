using System;

namespace BadgeOverlay;

public sealed class BadgeDefinition
{
    public required string Id { get; init; }
    public required string Text { get; set; }

    public bool Visible { get; set; } = true;
    public TriggerMode Trigger { get; set; } = TriggerMode.Hover;

    public PopoutDefinition? Popout { get; set; }
    public PopoutAlignment? PopoutAlignment { get; set; }

    public Action? OnClicked { get; set; }
    public Action<BadgeClickEventArgs>? OnMouseClick { get; set; }
    public bool OpenByDefault { get; set; }
    public Action? OnShow { get; set; }
    public Action? OnHide { get; set; }

    public uint? BackgroundColor { get; set; }
    public uint? HoveredColor { get; set; }
    public uint? ActiveColor { get; set; }
    public uint? TextColor { get; set; }
}
