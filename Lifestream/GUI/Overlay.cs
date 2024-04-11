using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;

namespace Lifestream.GUI;

internal class Overlay : Window
{
    public Overlay() : base("Lifestream Overlay", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing, true)
    {
        IsOpen = true;
    }

    Vector2 bWidth = new(10, 10);
    Vector2 ButtonSizeAetheryte => bWidth + new Vector2(P.Config.ButtonWidth, P.Config.ButtonHeightAetheryte);
    Vector2 ButtonSizeWorld => bWidth + new Vector2(P.Config.ButtonWidth, P.Config.ButtonHeightWorld);
    Vector2 WSize = new(200, 200);

    public override void PreDraw()
    {
        if (P.Config.FixedPosition)
        {
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(GetBasePosX(), GetBasePosY()) + P.Config.Offset);
        }
    }

    float GetBasePosX()
    {
        if (P.Config.PosHorizontal == BasePositionHorizontal.Middle)
        {
            return ImGuiHelpers.MainViewport.Size.X / 2 - WSize.X / 2;
        }
        else if (P.Config.PosHorizontal == BasePositionHorizontal.Right)
        {
            return ImGuiHelpers.MainViewport.Size.X - WSize.X;
        }
        else
        {
            return 0;
        }
    }

    float GetBasePosY()
    {
        if (P.Config.PosVertical == BasePositionVertical.Middle)
        {
            return ImGuiHelpers.MainViewport.Size.Y / 2 - WSize.Y / 2;
        }
        else if (P.Config.PosVertical == BasePositionVertical.Bottom)
        {
            return ImGuiHelpers.MainViewport.Size.Y - WSize.Y;
        }
        else
        {
            return 0;
        }
    }

    public override void Draw()
    {
        RespectCloseHotkey = P.Config.AllowClosingESC2;
        List<Action> actions = new();
        if (P.Config.ShowAethernet) actions.Add(DrawAethernet);
        if (P.ActiveAetheryte.Value.IsWorldChangeAetheryte() && P.Config.ShowWorldVisit) actions.Add(DrawWorldVisit);
        ImGuiEx.EzTableColumns("LifestreamTable", actions.ToArray());
        WSize = ImGui.GetWindowSize();
    }

    void DrawAethernet()
    {
        var master = Util.GetMaster();
        if (!P.Config.Hidden.Contains(master.ID))
        {
            var name = (P.Config.Favorites.Contains(master.ID) ? "★ " : "") + (P.Config.Renames.TryGetValue(master.ID, out var value) ? value : master.Name);
            ResizeButton(name);
            var md = P.ActiveAetheryte == master;
            if (md) ImGui.BeginDisabled();
            if (ImGui.Button(name, ButtonSizeAetheryte))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskAethernetTeleport.Enqueue(master);
            }
            if (md) ImGui.EndDisabled();
            Popup(master);
        }

        foreach (var x in P.DataStore.Aetherytes[master])
        {
            if (!P.Config.Hidden.Contains(x.ID))
            {
                var name = (P.Config.Favorites.Contains(x.ID) ? "★ " : "") + (P.Config.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                ResizeButton(name);
                var d = P.ActiveAetheryte == x;
                if (d) ImGui.BeginDisabled();
                if (ImGui.Button(name, ButtonSizeAetheryte))
                {
                    TaskRemoveAfkStatus.Enqueue();
                    TaskAethernetTeleport.Enqueue(x);
                }
                if (d) ImGui.EndDisabled();
                Popup(x);
            }
        }

        if(P.ActiveAetheryte.Value.ID == 70 && P.Config.Firmament)
        {
            var name = "Firmament";
            ResizeButton(name);
            if (ImGui.Button(name, ButtonSizeAetheryte))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskFirmanentTeleport.Enqueue();
            }
        }
    }

    void Popup(TinyAetheryte x)
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup($"LifestreamPopup{x.ID}");
        }
        if (ImGui.BeginPopup($"LifestreamPopup{x.ID}"))
        {
            if(ImGuiEx.CollectionCheckbox("Favorite", x.ID, P.Config.Favorites))
            {
                PluginLog.Debug($"Rebuilding data store");
                P.DataStore = new();
            }
            ImGuiEx.CollectionCheckbox("Hidden", x.ID, P.Config.Hidden);
            var newName = P.Config.Renames.TryGetValue(x.ID, out string value) ? value : "";
            ImGuiEx.Text($"Rename:");
            ImGui.SetNextItemWidth(200);
            if(ImGui.InputText($"##LifestreamRename", ref newName, 100))
            {
                if (newName == "")
                {
                    P.Config.Renames.Remove(x.ID);
                }
                else
                {
                    P.Config.Renames[x.ID] = newName;
                }
            }
            ImGui.EndPopup();
        }
    }

    void DrawWorldVisit()
    {
        var cWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.GameData.Name.ToString();
        foreach (var x in P.DataStore.Worlds)
        {
            ResizeButton(x);
            var isHomeWorld = x == Player.HomeWorld;
            var d = x == cWorld || Util.IsDisallowedToChangeWorld();
            if (d) ImGui.BeginDisabled();
            if (ImGui.Button((isHomeWorld ? (Lang.Symbols.HomeWorld + " ") : "") + x, ButtonSizeWorld))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskChangeWorld.Enqueue(x);
                TaskDesktopNotification.Enqueue($"Arrived to {x}");
                if (P.Config.WorldVisitTPToAethernet && !P.Config.WorldVisitTPTarget.IsNullOrEmpty() && !P.Config.WorldVisitTPOnlyCmd)
                {
                    P.TaskManager.Enqueue(() => Player.Interactable);
                    P.TaskManager.Enqueue(() => TaskTryTpToAethernetDestination.Enqueue(P.Config.WorldVisitTPTarget));
                }
            }
            if (d) ImGui.EndDisabled();
        }

    }

    void ResizeButton(string t)
    {
        var s = ImGuiHelpers.GetButtonSize(t);
        if (bWidth.X < s.X)
        {
            bWidth = s;
        }
    }

    public override bool DrawConditions()
    {
        if (!P.Config.Enable) return false;
        var ret = Util.CanUseAetheryte();
        if(ret)
        {
            if (P.ActiveAetheryte.Value.IsWorldChangeAetheryte())
            {
                ret = P.Config.ShowWorldVisit || P.Config.ShowAethernet;
            }
            else
            {
                ret = P.Config.ShowAethernet;
            }
        }
        if (!ret)
        {
            bWidth = new(10, 10);
        }
        return ret && !(P.Config.HideAddon && Util.IsAddonsVisible(Util.Addons));
    }
}
