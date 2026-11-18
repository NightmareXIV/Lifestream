using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using ECommons;
using ECommons.Automation;
using ECommons.ImGuiMethods;
using ECommons.Interop;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace BadgeOverlay;
#nullable enable

public sealed class OverlayManager
{
    private const int WarmFrames = 2;
    private const int MaxMisplacedHideFrames = 4;
    private const ImGuiWindowFlags BadgeFlags =
        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav |
        ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    private const ImGuiWindowFlags PopoutFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav |
        ImGuiWindowFlags.NoCollapse;

    private readonly List<BadgeBar> _bars = [];
    private readonly List<OpenPopout> _stack = [];
    private readonly List<PlacedBadge> _placed = [];
    private readonly List<Rect> _badgeRects = [];

    private bool _hold, _holdPrev;
    private BadgeBar? _rootBar;
    private BadgeDefinition? _rootBadge;
    private bool _rootPlaced;

    private bool RootBadgeAlive => _rootBar is { } bar && _rootBadge is { } badge && bar.Visible && badge.Visible && badge.Popout is not null && _bars.Contains(bar) && bar.Badges.Contains(badge);
    private bool Held => _hold || _holdPrev;
    internal void Hold()
    {
        _hold = true;
    }
    private string? _suppressKey;
    private bool _suppressSeen;

    private bool _defaultOpenedThisFrame;

    private HashSet<BadgeDefinition> _badgesShown = [];
    private HashSet<BadgeDefinition> _badgesNow = [];
    private HashSet<BadgeBar> _barsShown = [];
    private HashSet<BadgeBar> _barsNow = [];

    public OverlayManager(OverlayOptions? options = null)
    {
        Options = options ?? new OverlayOptions();
    }

    public OverlayOptions Options { get; }
    public bool IsAnyPopoutOpen => _stack.Count > 0;

    public void AddBar(BadgeBar bar)
    {
        _bars.Add(bar);
    }

    public void RemoveBar(BadgeBar bar)
    {
        if(_bars.Remove(bar))
        {
            CloseAll();
        }
    }

    public void CloseAll()
    {
        CloseFrom(0);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
    public void Draw()
    {
        Options.PreDraw?.Invoke();
        var vp = ImGui.GetMainViewport();
        var screen = new Rect(vp.Pos, vp.Pos + vp.Size);
        var mouse = ImGui.GetMousePos();
        _holdPrev = _hold;
        _hold = false;
        _rootPlaced = false;
        _suppressSeen = false;
        _defaultOpenedThisFrame = false;
        _badgesNow.Clear();
        _barsNow.Clear();

        _badgeRects.Clear();
        foreach(var bar in _bars)
        {
            if(!bar.Visible)
            {
                continue;
            }

            using(var font = bar.Style.GetFont?.Invoke()?.Push())
            {
                bar.ComputeLayout(screen, _placed);
                if(_placed.Count > 0)
                {
                    _barsNow.Add(bar);
                }
                for(var i = 0; i < _placed.Count; i++)
                {
                    DrawBadge(bar, _placed[i]);
                }
            }
        }

        if(_stack.Count > 0 && !_rootPlaced)
        {
            CloseAll();
        }

        if(_suppressKey != null && !_suppressSeen)
        {
            _suppressKey = null;
        }

        NotifyVisibility();

        if(_stack.Count > 0 && !_rootPlaced)
        {
            CloseAll();
        }

        for(var i = 0; i < _stack.Count; i++)
        {
            if(!RootBadgeAlive)
            {
                CloseAll();
                break;
            }
            DrawPopout(i, _stack[i], screen);
        }

        UpdateHoverOut(mouse);

        if(_stack.Count > 0 && !Held)
        {
            var pressed = GenericHelpers.IsKeyPressed([LimitedKeys.LeftMouseButton, LimitedKeys.RightMouseButton, LimitedKeys.MiddleMouseButton]);
            if(!_outPressed && pressed && !IsInsideOverlay(mouse))
            {
                if(_stack[0].Pinned)
                {
                    CloseFrom(1);
                }
                else
                {
                    CloseAll();
                }
            }
            _outPressed = pressed;
        }
        else
        {
            if(_outPressed)
            {
                _outPressed = false;
            }
        }
    }

    private void NotifyVisibility()
    {
        try
        {
            foreach(var bar in _barsNow)
            {
                if(!_barsShown.Contains(bar)) { bar.OnShow?.Invoke(); }
            }
            foreach(var badge in _badgesNow)
            {
                if(!_badgesShown.Contains(badge)) { badge.OnShow?.Invoke(); }
            }
            foreach(var badge in _badgesShown)
            {
                if(!_badgesNow.Contains(badge)) { badge.OnHide?.Invoke(); }
            }
            foreach(var bar in _barsShown)
            {
                if(!_barsNow.Contains(bar)) { bar.OnHide?.Invoke(); }
            }
        }
        finally
        {
            (_badgesShown, _badgesNow) = (_badgesNow, _badgesShown);
            (_barsShown, _barsNow) = (_barsNow, _barsShown);
        }
    }

    private bool _outPressed = false;

    private bool IsInsideOverlay(Vector2 mouse)
    {
        foreach(var r in _badgeRects)
        {
            if(r.Contains(mouse))
            {
                return true;
            }
        }

        foreach(var p in _stack)
        {
            if(p.WindowRect.Contains(mouse) || p.Bridge.Contains(mouse))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawBadge(BadgeBar bar, in PlacedBadge pb)
    {
        var badge = pb.Badge;
        var st = bar.Style;
        var rect = pb.Rect;
        var key = bar.Id + "/" + badge.Id;
        _badgeRects.Add(rect);
        _badgesNow.Add(badge);
        if(_suppressKey == key)
        {
            _suppressSeen = true;
        }
        if(_stack.Count > 0 && _stack[0].Key == key)
        {
            _rootPlaced = true;
        }

        var open = _stack.Count > 0 && _stack[0].Key == key;
        bool hovered = false, clickedL = false, clickedR = false, clickedM = false;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(1f, 1f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.SetNextWindowPos(rect.Min, ImGuiCond.Always);
        ImGui.SetNextWindowSize(rect.Size, ImGuiCond.Always);

        var begun = ImGui.Begin($"##BadgeOverlay_{key}", BadgeFlags);
        try
        {
            if(begun)
            {

                if(Options.AlwaysOnTop && !Held)
                {
                    CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
                }
                ImGui.SetCursorPos(Vector2.Zero);
                ImGui.InvisibleButton("##hit", rect.Size);
                hovered = ImGui.IsItemHovered();
                clickedL = ImGui.IsItemClicked(ImGuiMouseButton.Left);
                clickedR = ImGui.IsItemClicked(ImGuiMouseButton.Right);
                clickedM = ImGui.IsItemClicked(ImGuiMouseButton.Middle);

                var bg = open ? (badge.ActiveColor ?? (st.UseCustomColors ? st.ActiveColor : ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive].ToUint()))
                        : hovered ? (badge.HoveredColor ?? (st.UseCustomColors ? st.HoveredColor : ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered].ToUint()))
                        : (badge.BackgroundColor ?? (st.UseCustomColors ? st.BackgroundColor : ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg].ToUint()));

                var k = st.ScaleWithGlobalScale ? ImGuiHelpers.GlobalScale : 1f;
                var rounding = st.Rounding * k;
                var dl = ImGui.GetWindowDrawList();
                dl.AddRectFilled(rect.Min, rect.Max, bg, rounding);
                if(st.BorderThickness > 0f)
                {
                    dl.AddRect(rect.Min, rect.Max, st.UseCustomColors ? st.BorderColor : ImGui.GetStyle().Colors[(int)ImGuiCol.Border].ToUint(), rounding, ImDrawFlags.None, st.BorderThickness * k);
                }

                var vertical = st.Dock is BadgeDock.Left or BadgeDock.Right;
                BadgeText.Draw(dl, badge.Text, rect, badge.TextColor ?? (st.UseCustomColors ? st.TextColor : ImGui.GetStyle().Colors[(int)ImGuiCol.Text].ToUint()), pb.TextScale, vertical, st.TextRotation);
            }
        }
        finally
        {
            ImGui.End();
            ImGui.PopStyleVar(4);
        }

        var leftPrevented = false;
        if(clickedL)
        {
            badge.OnClicked?.Invoke();
        }
        if(badge.OnMouseClick != null)
        {
            if(clickedL)
            {
                leftPrevented = RaiseClick(bar, badge, open, ImGuiMouseButton.Left);
            }
            if(clickedR)
            {
                RaiseClick(bar, badge, open, ImGuiMouseButton.Right);
            }
            if(clickedM)
            {
                RaiseClick(bar, badge, open, ImGuiMouseButton.Middle);
            }
        }

        if(Held && _stack.Count > 0)
        {
            return;
        }

        if(badge.OpenByDefault)
        {
            badge.OpenByDefault = false;
            if(!_defaultOpenedThisFrame && badge.Popout is { } defaultDef)
            {
                _defaultOpenedThisFrame = true;
                OpenRoot(bar, badge, key, defaultDef, rect, st.Dock, pinned: true);
            }
        }

        if(hovered && _stack.Count > 0 && _stack[0].Key != key)
        {
            CloseFrom(0);
        }

        open = _stack.Count > 0 && _stack[0].Key == key;
        if(open)
        {
            _stack[0].TriggerRect = rect;
            _stack[0].Anchor = rect;
            _stack[0].Alignment = badge.PopoutAlignment ?? st.PopoutAlignment;
        }

        if(badge.Popout is { } def)
        {
            if(clickedL && !leftPrevented)
            {
                if(open)
                {
                    CloseFrom(0);
                    if(badge.Trigger == TriggerMode.Hover)
                    {
                        _suppressKey = key;
                        _suppressSeen = true;
                    }
                }
                else
                {
                    if(_suppressKey == key)
                    {
                        _suppressKey = null;
                    }
                    OpenRoot(bar, badge, key, def, rect, st.Dock);
                }
            }
            else if(badge.Trigger == TriggerMode.Hover && hovered && !open && _suppressKey != key)
            {
                OpenRoot(bar, badge, key, def, rect, st.Dock);
            }
        }

        if(!hovered && _suppressKey == key)
        {
            _suppressKey = null;
        }
    }

    private static bool RaiseClick(BadgeBar bar, BadgeDefinition badge, bool popoutOpen, ImGuiMouseButton button)
    {
        var args = new BadgeClickEventArgs
        {
            Bar = bar,
            Badge = badge,
            Button = button,
            IsPopoutOpen = popoutOpen,
        };
        badge.OnMouseClick!(args);
        return args.PreventDefault;
    }

    private void OpenRoot(BadgeBar bar, BadgeDefinition badge, string key, PopoutDefinition def, Rect badgeRect, BadgeDock dock, bool pinned = false)
    {
        CloseFrom(0);
        var preferred = dock switch
        {
            BadgeDock.Left => Side.Right,
            BadgeDock.Right => Side.Left,
            BadgeDock.Top => Side.Bottom,
            _ => Side.Top,
        };
        _stack.Add(new OpenPopout
        {
            Key = key,
            Def = def,
            Level = 0,
            Preferred = preferred,
            TriggerRect = badgeRect,
            Anchor = badgeRect,
            Pinned = pinned,
            Alignment = badge.PopoutAlignment ?? bar.Style.PopoutAlignment,
        });
        _rootBar = bar;
        _rootBadge = badge;
        _rootPlaced = true;
        def.OnOpened?.Invoke();
    }

    internal bool IsOpen(int index, string key)
    {
        return _stack.Count > index && _stack[index].Key == key;
    }

    internal void NotifySelectable(int level, string key, Func<PopoutDefinition> factory, TriggerMode mode,
                                   bool hovered, bool clicked, Rect trigger, Rect anchor)
    {
        var child = level + 1;
        var open = IsOpen(child, key);
        if(open)
        {
            _stack[child].TriggerRect = trigger;
            _stack[child].Anchor = anchor;
        }

        if(Held)
        {
            return;
        }

        if(hovered && !open && _stack.Count > child)
        {
            CloseFrom(child);
        }

        open = IsOpen(child, key);

        if(mode == TriggerMode.Hover)
        {
            if(hovered && !open)
            {
                OpenChild(child, key, factory(), trigger, anchor);
            }
        }
        else if(clicked)
        {
            if(open)
            {
                CloseFrom(child);
            }
            else
            {
                OpenChild(child, key, factory(), trigger, anchor);
            }
        }
    }

    private void OpenChild(int index, string key, PopoutDefinition def, Rect trigger, Rect anchor)
    {
        CloseFrom(index);
        if(_stack.Count != index)
        {
            return;
        }

        _stack.Add(new OpenPopout
        {
            Key = key,
            Def = def,
            Level = index,
            Preferred = Side.Right,
            CrossOffset = -ImGui.GetStyle().WindowPadding.Y,
            TriggerRect = trigger,
            Anchor = anchor,
        });
        def.OnOpened?.Invoke();
    }

    internal void CloseFrom(int index)
    {
        for(var i = _stack.Count - 1; i >= index && i >= 0; i--)
        {
            var p = _stack[i];
            _stack.RemoveAt(i);
            p.Def.OnClosed?.Invoke();
        }

        if(_stack.Count == 0)
        {
            _rootBar = null;
            _rootBadge = null;
        }
    }

    private void DrawPopout(int index, OpenPopout p, Rect screen)
    {
        var gap = Options.PopoutGap * ImGuiHelpers.GlobalScale;
        var warm = !p.Ready;
        p.Pos = Place(p, screen, gap, out var bridge);
        p.Bridge = bridge;

        ImGui.SetNextWindowPos(p.Pos, ImGuiCond.Always);
        if(p.Def.MinWidth > 0f)
        {
            ImGui.SetNextWindowSizeConstraints(new Vector2(p.Def.MinWidth * ImGuiHelpers.GlobalScale, 0f), new Vector2(float.MaxValue, float.MaxValue));
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, Options.PopoutRounding * ImGuiHelpers.GlobalScale);
        var pushed = 1;
        if(warm)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0f);
            pushed++;
        }
        using(var font = p.Def.GetFont?.Invoke()?.Push())
        {
            var begun = ImGui.Begin($"##BadgeOverlayPopout_{p.Key}", warm ? PopoutFlags | ImGuiWindowFlags.NoInputs : PopoutFlags);
            try
            {
                if(begun)
                {
                    if(Options.AlwaysOnTop && !Held)
                    {
                        CImGui.igBringWindowToDisplayFront(CImGui.igGetCurrentWindow());
                    }
                    var newSize = ImGui.GetWindowSize();
                    if(p.Frames > 0 && MathF.Abs(newSize.X - p.Size.X) < 0.5f && MathF.Abs(newSize.Y - p.Size.Y) < 0.5f)
                    {
                        p.StableFrames++;
                    }
                    else
                    {
                        p.StableFrames = 0;
                    }
                    var drawnPos = ImGui.GetWindowPos();
                    p.Pos = drawnPos;
                    p.Size = newSize;
                    var wantPos = Place(p, screen, gap, out var wantBridge);
                    p.Bridge = wantBridge;
                    var misplaced = MathF.Abs(MathF.Truncate(wantPos.X) - drawnPos.X) > 0.5f
                                 || MathF.Abs(MathF.Truncate(wantPos.Y) - drawnPos.Y) > 0.5f;

                    bool hide;
                    if(warm)
                    {
                        hide = true;
                        p.MisplacedStreak = 0;
                    }
                    else if(misplaced && Options.HideMisplacedPopouts)
                    {
                        p.MisplacedStreak++;
                        hide = p.MisplacedStreak <= MaxMisplacedHideFrames;
                    }
                    else
                    {
                        p.MisplacedStreak = 0;
                        hide = false;
                    }

                    var dl = ImGui.GetWindowDrawList();
                    try
                    {
                        p.Def.Draw(new PopoutContext(this, index, p));
                    }
                    catch(Exception e)
                    {
                        e.Log();
                    }
                    if(hide)
                    {
                        var vtx = dl.VtxBuffer;
                        for(var i = 0; i < vtx.Size; i++)
                        {
                            ref var v = ref vtx.Ref(i);
                            v.Col &= 0x00FFFFFFu;
                        }
                    }
                    if(ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopupId | ImGuiPopupFlags.AnyPopupLevel))
                    {
                        Hold();
                    }
                }
            }
            finally
            {
                ImGui.End();
                ImGui.PopStyleVar(pushed);
            }
        }

        p.Frames++;
        if(!p.Ready && p.Frames >= WarmFrames)
        {
            p.Ready = true;
        }
    }

    private static Vector2 Place(OpenPopout p, Rect screen, float gap, out Rect bridge)
    {
        var size = new Vector2(MathF.Max(p.Size.X, 1f), MathF.Max(p.Size.Y, 1f));
        var a = p.Anchor;
        Vector2 pos;
        Side used;

        if(p.Preferred is Side.Right or Side.Left)
        {
            var wantY = p.Alignment switch
            {
                PopoutAlignment.Center => (a.Min.Y + a.Max.Y - size.Y) * 0.5f,
                PopoutAlignment.End => a.Max.Y - size.Y,
                _ => a.Min.Y + p.CrossOffset,
            }
        ;
            var y = Math.Clamp(MathF.Round(wantY), screen.Min.Y, MathF.Max(screen.Min.Y, screen.Max.Y - size.Y));
            var xr = a.Max.X + gap;
            var xl = a.Min.X - gap - size.X;
            var fitsR = xr + size.X <= screen.Max.X;
            var fitsL = xl >= screen.Min.X;
            var useRight = p.Preferred == Side.Right ? (fitsR || !fitsL) : (!fitsL && fitsR);
            var x = useRight ? xr : xl;
            x = Math.Clamp(x, screen.Min.X, MathF.Max(screen.Min.X, screen.Max.X - size.X));
            pos = new Vector2(x, y);
            used = useRight ? Side.Right : Side.Left;
        }
        else
        {
            var wantX = p.Alignment switch
            {
                PopoutAlignment.Center => (a.Min.X + a.Max.X - size.X) * 0.5f,
                PopoutAlignment.End => a.Max.X - size.X,
                _ => a.Min.X + p.CrossOffset,
            };
            var x = Math.Clamp(MathF.Round(wantX), screen.Min.X, MathF.Max(screen.Min.X, screen.Max.X - size.X));
            var yb = a.Max.Y + gap;
            var yt = a.Min.Y - gap - size.Y;
            var fitsB = yb + size.Y <= screen.Max.Y;
            var fitsT = yt >= screen.Min.Y;
            var useBottom = p.Preferred == Side.Bottom ? (fitsB || !fitsT) : (!fitsT && fitsB);
            var y = useBottom ? yb : yt;
            y = Math.Clamp(y, screen.Min.Y, MathF.Max(screen.Min.Y, screen.Max.Y - size.Y));
            pos = new Vector2(x, y);
            used = useBottom ? Side.Bottom : Side.Top;
        }

        var win = Rect.FromPosSize(pos, size);
        var t = p.TriggerRect;
        float cy0 = MathF.Max(t.Min.Y, win.Min.Y), cy1 = MathF.Min(t.Max.Y, win.Max.Y);
        float cx0 = MathF.Max(t.Min.X, win.Min.X), cx1 = MathF.Min(t.Max.X, win.Max.X);
        bridge = used switch
        {
            Side.Right => new Rect(new Vector2(a.Max.X, cy0), new Vector2(win.Min.X, cy1)),
            Side.Left => new Rect(new Vector2(win.Max.X, cy0), new Vector2(a.Min.X, cy1)),
            Side.Bottom => new Rect(new Vector2(cx0, a.Max.Y), new Vector2(cx1, win.Min.Y)),
            _ => new Rect(new Vector2(cx0, win.Max.Y), new Vector2(cx1, a.Min.Y)),
        };
        return pos;
    }

    private void UpdateHoverOut(Vector2 mouse)
    {
        var now = ImGui.GetTime();
        if(Held)
        {
            foreach(var q in _stack)
            {
                q.HoverLostSince = null;
            }

            return;
        }

        for(var i = 0; i < _stack.Count; i++)
        {
            var p = _stack[i];
            if(!p.Measured || !p.Def.CloseOnHoverOut || (i == 0 && p.Pinned))
            {
                p.HoverLostSince = null;
                continue;
            }

            var inside = p.TriggerRect.Contains(mouse);
            for(var j = i; j < _stack.Count && !inside; j++)
            {
                inside = _stack[j].WindowRect.Contains(mouse) || _stack[j].Bridge.Contains(mouse);
            }

            if(inside)
            {
                p.HoverLostSince = null;
                continue;
            }

            p.HoverLostSince ??= now;
            var delay = p.Def.HoverOutDelay ?? Options.HoverOutDelay;
            if(now - p.HoverLostSince.Value >= delay)
            {
                CloseFrom(i);
                break;
            }
        }
    }
}
