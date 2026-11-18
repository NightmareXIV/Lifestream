using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BadgeOverlay;

public sealed class BadgeBar
{
    private readonly List<BadgeDefinition> _visible = [];
    private float[] _textW = new float[16];
    private float[] _natural = new float[16];
    private float[] _min = new float[16];
    private float[] _len = new float[16];

    public BadgeBar(string id, BadgeBarStyle? style = null)
    {
        Id = id;
        Style = style ?? new BadgeBarStyle();
    }

    public string Id { get; }
    public BadgeBarStyle Style { get; set; }
    public List<BadgeDefinition> Badges { get; } = [];
    public bool Visible { get; set; } = true;
    public Action? OnShow { get; set; }
    public Action? OnHide { get; set; }

    internal void ComputeLayout(Rect screen, List<PlacedBadge> output)
    {
        output.Clear();
        _visible.Clear();
        foreach(var b in Badges)
        {
            if(b.Visible)
            {
                _visible.Add(b);
            }
        }

        var n = _visible.Count;
        if(n == 0)
        {
            return;
        }

        var s = Style;
        var k = s.ScaleWithGlobalScale ? ImGuiHelpers.GlobalScale : 1f;
        var vertical = s.Dock is BadgeDock.Left or BadgeDock.Right;
        var axisLen = vertical ? screen.Size.Y : screen.Size.X;

        var padStart = s.PaddingStart * k;
        var padEnd = s.PaddingEnd * k;
        var spacing = MathF.Max(0f, s.Spacing * k);
        var inner = MathF.Max(0f, s.InnerPadding * k);
        var avail = axisLen - padStart - padEnd;
        if(avail < 1f)
        {
            return;
        }

        if(_textW.Length < n)
        {
            var cap = Math.Max(n, _textW.Length * 2);
            _textW = new float[cap];
            _natural = new float[cap];
            _min = new float[cap];
            _len = new float[cap];
        }

        var maxText = 0f;
        for(var i = 0; i < n; i++)
        {
            _textW[i] = ImGui.CalcTextSize(_visible[i].Text).X;
            maxText = MathF.Max(maxText, _textW[i]);
        }

        var textH = ImGui.GetTextLineHeight();
        var thickness = s.Thickness > 0f ? s.Thickness * k : textH + (2f * s.CrossPadding * k);

        var uniform = s.Sizing == BadgeSizing.Uniform;
        var uniformNatural = s.UniformLength > 0f
            ? MathF.Max(s.UniformLength * k, maxText)
            : maxText + (2f * inner);

        float sumNatural = 0f, sumMin = 0f, free = 0f;
        for(var i = 0; i < n; i++)
        {
            _natural[i] = uniform ? uniformNatural : _textW[i] + (2f * inner);
            _min[i] = uniform ? maxText : _textW[i];
            sumNatural += _natural[i];
            sumMin += _min[i];
            free += _natural[i] - _min[i];
        }

        var gaps = (n - 1) * spacing;
        var total = sumNatural + gaps;
        var shrinkable = free + gaps;

        var keep = 1f;
        var textScale = 1f;
        if(total > avail)
        {
            var over = total - avail;
            if(over < shrinkable)
            {
                keep = (shrinkable - over) / shrinkable;
            }
            else
            {
                keep = 0f;
                textScale = sumMin > avail ? avail / sumMin : 1f;
            }
        }

        var gap = spacing * keep * textScale;
        var totalFinal = (n - 1) * gap;
        for(var i = 0; i < n; i++)
        {
            _len[i] = (_min[i] + ((_natural[i] - _min[i]) * keep)) * textScale;
            totalFinal += _len[i];
        }

        var margin = s.ScreenMargin * k;
        var cross = s.Dock switch
        {
            BadgeDock.Left => screen.Min.X + margin,
            BadgeDock.Right => screen.Max.X - margin - thickness,
            BadgeDock.Top => screen.Min.Y + margin,
            _ => screen.Max.Y - margin - thickness,
        };

        var axisOrigin = vertical ? screen.Min.Y : screen.Min.X;

        var backward = s.Anchor == BadgeAnchor.End && !s.KeepOrderWhenAnchoredToEnd;
        var cursor = s.Anchor switch
        {
            BadgeAnchor.Start => padStart,
            BadgeAnchor.Center => padStart + ((avail - totalFinal) * 0.5f),
            _ => backward ? axisLen - padEnd : axisLen - padEnd - totalFinal,
        };

        for(var i = 0; i < n; i++)
        {
            float pos;
            if(backward)
            {
                pos = cursor - _len[i];
                cursor = pos - gap;
            }
            else
            {
                pos = cursor;
                cursor += _len[i] + gap;
            }

            var a0 = MathF.Round(axisOrigin + pos);
            var a1 = MathF.Round(axisOrigin + pos + _len[i]);
            var c0 = MathF.Round(cross);
            var c1 = MathF.Round(cross + thickness);

            var rect = vertical ? new Rect(new Vector2(c0, a0), new Vector2(c1, a1)) : new Rect(new Vector2(a0, c0), new Vector2(a1, c1));

            output.Add(new PlacedBadge { Badge = _visible[i], Rect = rect, TextScale = textScale });
        }
    }
}
