using ECommons.Configuration;
using ECommons.EzEventManager;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using Lifestream.Enums;
using Lifestream.Systems;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;

namespace Lifestream.GUI;

public class Overlay : Window
{
    private Overlay() : base("Lifestream Overlay", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing, true)
    {
        EzConfigGui.WindowSystem.AddWindow(this);
        IsOpen = true;
        new EzTerritoryChanged((x) => IsOpen = true);
    }

    private Vector2 bWidth = new(10, 10);
    private Vector2 ButtonSizeAetheryte => bWidth + new Vector2(C.ButtonWidthArray[0], C.ButtonHeightAetheryte);
    private Vector2 ButtonSizeWorld => bWidth + new Vector2(C.ButtonWidthArray[1], C.ButtonHeightWorld);
    private Vector2 ButtonSizeInstance => bWidth + new Vector2(C.ButtonWidthArray[2], C.InstanceButtonHeight);

    private Vector2 WSize = new(200, 200);

    public override void PreDraw()
    {
        if(C.FixedPosition)
        {
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(GetBasePosX(), GetBasePosY()) + C.Offset);
        }
        if(C.LeftAlignButtons)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
        }
    }

    public override void PostDraw()
    {
        if(C.LeftAlignButtons)
        {
            ImGui.PopStyleVar();
        }
    }

    private float GetBasePosX()
    {
        if(C.PosHorizontal == BasePositionHorizontal.Middle)
        {
            return ImGuiHelpers.MainViewport.Size.X / 2 - WSize.X / 2;
        }
        else if(C.PosHorizontal == BasePositionHorizontal.Right)
        {
            return ImGuiHelpers.MainViewport.Size.X - WSize.X;
        }
        else
        {
            return 0;
        }
    }

    private float GetBasePosY()
    {
        if(C.PosVertical == BasePositionVertical.Middle)
        {
            return ImGuiHelpers.MainViewport.Size.Y / 2 - WSize.Y / 2;
        }
        else if(C.PosVertical == BasePositionVertical.Bottom)
        {
            return ImGuiHelpers.MainViewport.Size.Y - WSize.Y;
        }
        else
        {
            return 0;
        }
    }

    private string Pad => (C.LeftAlignPadding > 0 ? " ".Repeat(C.LeftAlignPadding) : null);

    public override void Draw()
    {
        RespectCloseHotkey = C.AllowClosingESC2;
        List<Action> actions = [];
        if(S.Data.ResidentialAethernet.IsInResidentialZone())
        {
            if(C.ShowAethernet)
            {
                actions.Add(() => DrawResidentialAethernet(false));
                actions.Add(() => DrawResidentialAethernet(true));
            }
        }
        else if(S.Data.CustomAethernet.ZoneInfo.ContainsKey(P.Territory))
        {
            if(C.ShowAethernet) actions.Add(DrawCustomAethernet);
        }
        else if(P.ActiveAetheryte != null)
        {
            if(C.ShowAethernet) actions.Add(DrawNormalAethernet);
            if(P.ActiveAetheryte.Value.IsWorldChangeAetheryte() && C.ShowWorldVisit) actions.Add(DrawWorldVisit);
            if(C.ShowWards && Utils.HousingAethernet.Contains(P.Territory) && P.ActiveAetheryte.Value.IsResidentialAetheryte()) actions.Add(DrawHousingWards);
        }
        if(S.InstanceHandler.GetInstance() != 0 && C.ShowInstanceSwitcher
            && (P.ActiveAetheryte == null || P.ActiveAetheryte.Value.IsAetheryte))
        {
            actions.Add(DrawInstances);
        }

        if(actions.Count == 1)
        {
            Safe(actions[0]);
        }
        else
        {
            if(ImGui.BeginTable("LifestreamTable", Math.Max(1, actions.Count), ImGuiTableFlags.NoSavedSettings))
            {
                foreach(var action in actions)
                {
                    ImGui.TableNextColumn();
                    Safe(action);
                }
                ImGui.EndTable();
            }
        }

        if(C.ShowPlots && S.Data.ResidentialAethernet.ActiveAetheryte != null)
        {
            if(ImGui.BeginTable("##plots", 6, ImGuiTableFlags.SizingFixedSame))
            {
                ImGui.TableSetupColumn("1");
                ImGui.TableSetupColumn("2");
                ImGui.TableSetupColumn("3");
                ImGui.TableSetupColumn("4");
                ImGui.TableSetupColumn("5");
                ImGui.TableSetupColumn("6");

                for(var i = 0; i < 10; i++)
                {
                    ImGui.TableNextRow();
                    var buttonSize = new Vector2((ButtonSizeAetheryte.X - ImGui.GetStyle().ItemSpacing.X * 2) / 3, ButtonSizeAetheryte.Y);
                    for(var q = 0; q < 3; q++)
                    {
                        ImGui.TableNextColumn();
                        var num = i * 3 + q + 1;
                        if(ImGui.Button($"{Pad}{num}", buttonSize))
                        {
                            TaskTpAndGoToWard.EnqueueFromResidentialAetheryte(Utils.GetResidentialAetheryteByTerritoryType(P.Territory).Value, num - 1, false, false, false);
                        }
                    }
                    for(var q = 0; q < 3; q++)
                    {
                        ImGui.TableNextColumn();
                        var num = i * 3 + q + 30 + 1;
                        if(ImGui.Button($"{Pad}{num}", buttonSize))
                        {
                            TaskTpAndGoToWard.EnqueueFromResidentialAetheryte(Utils.GetResidentialAetheryteByTerritoryType(P.Territory).Value, num - 1, false, false, false);
                        }
                    }
                }
                ImGui.EndTable();
            }
        }

        WSize = ImGui.GetWindowSize();
    }

    private void DrawNormalAethernet()
    {
        var master = Utils.GetMaster();
        if(!C.Hidden.Contains(master.ID))
        {
            var name = (C.Favorites.Contains(master.ID) ? "★ " : "") + (C.Renames.TryGetValue(master.ID, out var value) ? value : master.Name);
            ResizeButton($"{Pad}{name}");
            var md = P.ActiveAetheryte == master;
            if(ImGuiEx.Button($"{Pad}{name}", ButtonSizeAetheryte, !md))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskAethernetTeleport.Enqueue(master);
            }
            Popup(master);
        }

        foreach(var x in S.Data.DataStore.Aetherytes[master])
        {
            if(!C.Hidden.Contains(x.ID))
            {
                var name = (C.Favorites.Contains(x.ID) ? "★ " : "") + (C.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                ResizeButton($"{Pad}{name}");
                var d = P.ActiveAetheryte == x;
                if(ImGuiEx.Button($"{Pad}{name}", ButtonSizeAetheryte, !d))
                {
                    TaskRemoveAfkStatus.Enqueue();
                    TaskAethernetTeleport.Enqueue(x);
                }
                Popup(x);
            }
        }

        if(P.ActiveAetheryte.Value.ID == 70 && C.Firmament)
        {
            var name = "Firmament";
            ResizeButton($"{Pad}{name}");
            if(ImGui.Button($"{Pad}{name}", ButtonSizeAetheryte))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskFirmanentTeleport.Enqueue();
            }
        }
    }

    private void DrawResidentialAethernet(bool? subdivision = null)
    {
        var zinfo = S.Data.ResidentialAethernet.ZoneInfo[P.Territory];
        Draw(true);
        Draw(false);
        void Draw(bool favorites)
        {
            foreach(var x in zinfo.Aetherytes)
            {
                if(C.Favorites.Contains(x.ID) != favorites) continue;
                if(subdivision != null && x.IsSubdivision != subdivision) continue;
                if(!C.Hidden.Contains(x.ID))
                {
                    var name = (C.Favorites.Contains(x.ID) ? "★ " : "") + (C.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                    ResizeButton(name);
                    var d = S.Data.ResidentialAethernet.ActiveAetheryte == x;
                    if(ImGuiEx.Button($"{Pad}{name}", ButtonSizeAetheryte, !d))
                    {
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(x.Name);
                    }
                    Popup(x);
                }
            }
        }
    }

    private void DrawCustomAethernet()
    {
        var zinfo = S.Data.CustomAethernet.ZoneInfo[P.Territory];
        Draw(true);
        Draw(false);
        void Draw(bool favorites)
        {
            foreach(var x in zinfo.Aetherytes)
            {
                if(C.Favorites.Contains(x.ID) != favorites) continue;
                if(!C.Hidden.Contains(x.ID))
                {
                    var name = (C.Favorites.Contains(x.ID) ? "★ " : "") + (C.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                    ResizeButton(name);
                    var d = S.Data.CustomAethernet.ActiveAetheryte == x;
                    if(ImGuiEx.Button($"{Pad}{name}", ButtonSizeAetheryte, !d))
                    {
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(x.Name);
                    }
                    Popup(x);
                }
            }
        }
    }

    private void DrawInstances()
    {
        if(S.InstanceHandler.InstancesInitizliaed(out var maxInstances))
        {
            for(var i = 1; i <= Math.Min(maxInstances, 9); i++)
            {
                var name = $"Instance {TaskChangeInstance.InstanceNumbers[i]}";
                ResizeButton(name);
                var d = S.InstanceHandler.GetInstance() == i;
                if(ImGuiEx.Button($"{Pad}{name}", ButtonSizeInstance, !d))
                {
                    TaskRemoveAfkStatus.Enqueue();
                    TaskChangeInstance.Enqueue(i);
                }
            }
        }
        else
        {
            ImGuiEx.Text($"""
                Instances available, 
                but not initialized.

                To initialize instances, 
                access aetheryte once.
                """);
        }
    }

    private void Popup(IAetheryte x)
    {
        if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup($"LifestreamPopup{x.ID}");
        }
        if(ImGui.BeginPopup($"LifestreamPopup{x.ID}"))
        {
            if(ImGuiEx.CollectionCheckbox("Favorite", x.ID, C.Favorites))
            {
                PluginLog.Debug($"Rebuilding data store");
                S.Data.DataStore = new();
                EzConfig.Save();
            }
            if(ImGuiEx.CollectionCheckbox("Hidden", x.ID, C.Hidden)) EzConfig.Save();
            var newName = C.Renames.TryGetValue(x.ID, out var value) ? value : "";
            ImGuiEx.Text($"Rename:");
            ImGui.SetNextItemWidth(200f.Scale());
            if(ImGui.InputText($"##LifestreamRename", ref newName, 100))
            {
                if(newName == "")
                {
                    C.Renames.Remove(x.ID);
                }
                else
                {
                    C.Renames[x.ID] = newName;
                }
                EzConfig.Save();
            }
            ImGui.EndPopup();
        }
    }

    private void DrawWorldVisit()
    {
        var cWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.ValueNullable?.Name.ToString();
        foreach(var x in S.Data.DataStore.Worlds)
        {
            ResizeButton($"{Pad}{x}");
            var isHomeWorld = x == Player.HomeWorld;
            var d = x == cWorld || Utils.IsDisallowedToChangeWorld();
            if(ImGuiEx.Button($"{Pad}{(isHomeWorld ? (Lang.Symbols.HomeWorld + " ") : "")}{x}", ButtonSizeWorld, !d))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskChangeWorld.Enqueue(x);
                TaskDesktopNotification.Enqueue($"Arrived to {x}");
                if(C.WorldVisitTPToAethernet && !C.WorldVisitTPTarget.IsNullOrEmpty() && !C.WorldVisitTPOnlyCmd)
                {
                    P.TaskManager.Enqueue(() => Player.Interactable);
                    P.TaskManager.Enqueue(() => TaskTryTpToAethernetDestination.Enqueue(C.WorldVisitTPTarget));
                }
            }
        }
    }

    private void DrawHousingWards()
    {
        for(var i = 1; i <= 30; i++)
        {
            ResizeButton($"{Pad}{i}");
            var buttonSize = new Vector2((ButtonSizeInstance.X - ImGui.GetStyle().ItemSpacing.X * 2) / 3, ButtonSizeWorld.Y);
            if(ImGuiEx.Button($"{Pad}{i}##ward", buttonSize))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskGoToResidentialDistrict.Enqueue(i);
            }
            if(i % 3 != 0) ImGui.SameLine();
        }
    }

    private void ResizeButton(string t)
    {
        var s = ImGuiHelpers.GetButtonSize(t);
        if(bWidth.X < s.X)
        {
            bWidth = s;
        }
    }

    public override bool DrawConditions()
    {
        if(!C.Enable) return false;
        var canUse = Utils.CanUseAetheryte();
        var ret = canUse != AetheryteUseState.None;
        if(canUse == AetheryteUseState.Normal)
        {
            if(P.ActiveAetheryte.Value.IsWorldChangeAetheryte())
            {
                ret = C.ShowWorldVisit || C.ShowAethernet;
            }
            else
            {
                ret = C.ShowAethernet;
            }
        }
        else if(canUse == AetheryteUseState.Residential || canUse == AetheryteUseState.Custom)
        {
            ret = C.ShowAethernet;
        }
        if(canUse == AetheryteUseState.None)
        {
            bWidth = new(10, 10);
        }
        if(S.InstanceHandler.CanChangeInstance())
        {
            ret = true;
        }
        return ret && !(C.HideAddon && Utils.IsAddonsVisible(C.HideAddonList));
    }
}
