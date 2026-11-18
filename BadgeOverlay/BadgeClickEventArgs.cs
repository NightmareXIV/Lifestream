using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Text;

namespace BadgeOverlay;

public sealed class BadgeClickEventArgs
{
    public required BadgeBar Bar { get; init; }
    public required BadgeDefinition Badge { get; init; }
    public required ImGuiMouseButton Button { get; init; }
    public required bool IsPopoutOpen { get; init; }
    public bool PreventDefault { get; set; }
}