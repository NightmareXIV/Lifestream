using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using TerraFX.Interop.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BadgeOverlay;

public sealed class PopoutContext
{
    private readonly OverlayManager _manager;
    private readonly OpenPopout _popout;

    internal PopoutContext(OverlayManager manager, int level, OpenPopout popout)
    {
        _manager = manager;
        Level = level;
        _popout = popout;
    }

    public int Level { get; }

    public void Close()
    {
        _manager.CloseFrom(Level);
    }

    public void CloseAll()
    {
        _manager.CloseAll();
    }

    public void KeepOpen()
    {
        _manager.Hold();
    }

    public bool BeginPopup(string id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        var open = ImGui.BeginPopup(id, flags);
        if(open)
        {
            KeepOpen();
        }

        return open;
    }

    public bool BeginCombo(string label, string preview)
    {
        var open = ImGui.BeginCombo(label, preview);
        if(open)
        {
            KeepOpen();
        }

        return open;
    }

    public bool Selectable(string label, PopoutDefinition child, string? id = null, TriggerMode? trigger = null)
    {
        return Selectable(label, () => child, id, trigger);
    }

    public bool Selectable(string label, Func<PopoutDefinition> childFactory, string? id = null, TriggerMode? trigger = null)
    {
        var key = _popout.Key + "/" + (id ?? label);
        var isOpen = _manager.IsOpen(Level + 1, key);

        var clicked = ImGui.Selectable($"{label}##{key}", isOpen, ImGuiSelectableFlags.None, Vector2.Zero);
        var hovered = ImGui.IsItemHovered();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();

        var dl = ImGui.GetWindowDrawList();
        var h = ImGui.GetTextLineHeight();
        var a = h * 0.28f;
        var c = new Vector2(max.X - (a * 1.6f), (min.Y + max.Y) * 0.5f);
        dl.AddTriangleFilled(new Vector2(c.X - (a * 0.6f), c.Y - a), new Vector2(c.X - (a * 0.6f), c.Y + a),
                             new Vector2(c.X + (a * 0.8f), c.Y), ImGui.GetColorU32(ImGuiCol.Text));

        ImGui.SameLine(0f, 0f);
        ImGui.Dummy(new Vector2(h * 1.4f, 0f));

        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        var anchor = new Rect(new Vector2(winPos.X, min.Y), new Vector2(winPos.X + winSize.X, max.Y));

        _manager.NotifySelectable(Level, key, childFactory,
            trigger ?? _manager.Options.DefaultSelectableTrigger,
            hovered, clicked, new Rect(min, max), anchor);

        return clicked;
    }
}
