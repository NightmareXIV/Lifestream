using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace BadgeOverlay;

internal static class BadgeText
{
    public static void Draw(ImDrawListPtr dl, string text, Rect rect, uint color, float scale, bool vertical, VerticalTextRotation rotation)
    {
        var font = ImGui.GetFont();
        var fontSize = ImGui.GetFontSize() * scale;
        var size = ImGui.CalcTextSize(text) * scale;

        var center = (rect.Min + rect.Max) * 0.5f;
        center = new Vector2(MathF.Round(center.X), MathF.Round(center.Y));
        var pos = center - (size * 0.5f);
        pos = new Vector2(MathF.Round(pos.X), MathF.Round(pos.Y));

        if(!vertical)
        {
            dl.AddText(font, fontSize, pos, color, text);
            return;
        }

        var r = MathF.Max(size.X, size.Y) + 4f;
        dl.PushClipRect(center - new Vector2(r), center + new Vector2(r), false);

        var start = dl.VtxBuffer.Size;
        dl.AddText(font, fontSize, pos, color, text);

        var vtx = dl.VtxBuffer;
        var end = vtx.Size;
        var cw = rotation == VerticalTextRotation.Degrees90;
        for(var i = start; i < end; i++)
        {
            ref var v = ref vtx.Ref(i);
            var d = v.Pos - center;
            v.Pos = cw ? center + new Vector2(-d.Y, d.X) : center + new Vector2(d.Y, -d.X);
        }

        dl.PopClipRect();
    }
}
