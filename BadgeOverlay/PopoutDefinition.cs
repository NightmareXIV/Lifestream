using Dalamud.Interface.ManagedFontAtlas;
using System;

namespace BadgeOverlay;
#nullable enable

public sealed class PopoutDefinition
{
    public required Action<PopoutContext> Draw { get; init; }

    public float MinWidth { get; set; } = 0f;

    public float? HoverOutDelay { get; set; }

    public bool CloseOnHoverOut { get; set; } = true;
    public Func<IFontHandle?>? GetFont { get; set; } = null;

    public Action? OnOpened { get; set; }
    public Action? OnClosed { get; set; }
}
