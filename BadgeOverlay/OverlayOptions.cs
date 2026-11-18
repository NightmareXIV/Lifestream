using System;

namespace BadgeOverlay;

public sealed class OverlayOptions
{
    public float HoverOutDelay = 0.25f;
    public float PopoutGap = 2f;
    public TriggerMode DefaultSelectableTrigger = TriggerMode.Hover;
    public float PopoutRounding = 4f;
    public bool AlwaysOnTop = false;
    public bool HideMisplacedPopouts = true;

    [NonSerialized] public Action? PreDraw = null;
}
