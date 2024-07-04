using ECommons.EzEventManager;
using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Systems;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;
using System;

namespace Lifestream.GUI;

internal class Overlay : Window
{
    public Overlay() : base("Lifestream Overlay", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing, true)
    {
        IsOpen = true;
        new EzTerritoryChanged((x) => IsOpen = true);
    }

    Vector2 bWidth = new(10, 10);
    Vector2 ButtonSizeAetheryte => bWidth + new Vector2(P.Config.ButtonWidth, P.Config.ButtonHeightAetheryte);
    Vector2 ButtonSizeWorld => bWidth + new Vector2(P.Config.ButtonWidth, P.Config.ButtonHeightWorld);
    Vector2 ButtonSizeInstance => bWidth + new Vector2(P.Config.ButtonWidth, P.Config.InstanceButtonHeight);
    Vector2 WSize = new(200, 200);

    public override void PreDraw()
    {
        if (P.Config.FixedPosition)
        {
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(GetBasePosX(), GetBasePosY()) + P.Config.Offset);
        }
    }

    private float GetBasePosX()
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

    private float GetBasePosY()
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
        List<Action> actions = [];
        if (P.ResidentialAethernet.IsInResidentialZone())
        {
            if (P.Config.ShowAethernet)
            {
                actions.Add(() => DrawResidentialAethernet(false));
                actions.Add(() => DrawResidentialAethernet(true));
            }
        }
        else if(P.ActiveAetheryte != null)
        {
            if (P.Config.ShowAethernet) actions.Add(DrawNormalAethernet);
            if (P.ActiveAetheryte.Value.IsWorldChangeAetheryte() && P.Config.ShowWorldVisit) actions.Add(DrawWorldVisit);
            if (P.Config.ShowWards && Utils.HousingAethernet.Contains(Svc.ClientState.TerritoryType) && P.ActiveAetheryte.Value.IsResidentialAetheryte()) actions.Add(DrawHousingWards);
        }
        if(S.InstanceHandler.GetInstance() != 0)
        {
            actions.Add(DrawInstances);
        }
        ImGuiEx.EzTableColumns("LifestreamTable", [.. actions]);

				if (P.Config.ShowPlots && P.ResidentialAethernet.ActiveAetheryte != null)
				{
						if (ImGui.BeginTable("##plots", 6, ImGuiTableFlags.SizingFixedSame))
						{
								ImGui.TableSetupColumn("1");
								ImGui.TableSetupColumn("2");
								ImGui.TableSetupColumn("3");
								ImGui.TableSetupColumn("4");
								ImGui.TableSetupColumn("5");
								ImGui.TableSetupColumn("6");

								for (int i = 0; i < 10; i++)
								{
										ImGui.TableNextRow();
										var buttonSize = new Vector2((ButtonSizeAetheryte.X - ImGui.GetStyle().ItemSpacing.X * 2) / 3, ButtonSizeAetheryte.Y);
										for (int q = 0; q < 3; q++)
										{
												ImGui.TableNextColumn();
                        var num = i * 3 + q + 1;
												if (ImGui.Button($"{num}", buttonSize))
												{
                            TaskTpAndGoToWard.EnqueueFromResidentialAetheryte(Utils.GetResidentialAetheryteByTerritoryType(Svc.ClientState.TerritoryType).Value, num-1, false, false, false);
												}
										}
										for (int q = 0; q < 3; q++)
										{
												ImGui.TableNextColumn();
                        var num = i * 3 + q + 30 + 1;
												if (ImGui.Button($"{num}", buttonSize))
												{
														TaskTpAndGoToWard.EnqueueFromResidentialAetheryte(Utils.GetResidentialAetheryteByTerritoryType(Svc.ClientState.TerritoryType).Value, num-1, false, false, false);
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
        if (!P.Config.Hidden.Contains(master.ID))
        {
            var name = (P.Config.Favorites.Contains(master.ID) ? "★ " : "") + (P.Config.Renames.TryGetValue(master.ID, out var value) ? value : master.Name);
            ResizeButton(name);
            var md = P.ActiveAetheryte == master;
            if (ImGuiEx.Button(name, ButtonSizeAetheryte, !md))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskAethernetTeleport.Enqueue(master);
            }
            Popup(master);
        }

        foreach (var x in P.DataStore.Aetherytes[master])
        {
            if (!P.Config.Hidden.Contains(x.ID))
            {
                var name = (P.Config.Favorites.Contains(x.ID) ? "★ " : "") + (P.Config.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                ResizeButton(name);
                var d = P.ActiveAetheryte == x;
                if (ImGuiEx.Button(name, ButtonSizeAetheryte, !d))
                {
                    TaskRemoveAfkStatus.Enqueue();
                    TaskAethernetTeleport.Enqueue(x);
                }
                Popup(x);
            }
        }

        if (P.ActiveAetheryte.Value.ID == 70 && P.Config.Firmament)
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

    private void DrawResidentialAethernet(bool? subdivision = null)
    {
        var zinfo = P.ResidentialAethernet.ZoneInfo[P.Territory];
        Draw(true);
        Draw(false);
        void Draw(bool favorites)
        {
            foreach (var x in zinfo.Aetherytes)
            {
                if (P.Config.Favorites.Contains(x.ID) != favorites) continue;
                if (subdivision != null && x.IsSubdivision != subdivision) continue;
                if (!P.Config.Hidden.Contains(x.ID))
                {
                    var name = (P.Config.Favorites.Contains(x.ID) ? "★ " : "") + (P.Config.Renames.TryGetValue(x.ID, out var value) ? value : x.Name);
                    ResizeButton(name);
                    var d = P.ResidentialAethernet.ActiveAetheryte == x;
                    if (ImGuiEx.Button(name, ButtonSizeAetheryte, !d))
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
        if (S.InstanceHandler.InstancesInitizliaed(out var maxInstances))
        {
            for (int i = 1; i <= Math.Min(maxInstances, 9); i++)
            {
                var name = $"Instance {TaskChangeInstance.InstanceNumbers[i]}";
                ResizeButton(name);
                var d = S.InstanceHandler.GetInstance() == i;
                if (ImGuiEx.Button(name, ButtonSizeInstance, !d))
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

    private void DrawWorldVisit()
    {
        var cWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.GameData.Name.ToString();
        foreach (var x in P.DataStore.Worlds)
        {
            ResizeButton(x);
            var isHomeWorld = x == Player.HomeWorld;
            var d = x == cWorld || Utils.IsDisallowedToChangeWorld();
            if (ImGuiEx.Button((isHomeWorld ? (Lang.Symbols.HomeWorld + " ") : "") + x, ButtonSizeWorld, !d))
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
        }
    }

    private void DrawHousingWards()
    {
        for (int i = 1; i <= 30; i++)
        {
            ResizeButton($"{i}");
            var buttonSize = new Vector2((ButtonSizeWorld.X - ImGui.GetStyle().ItemSpacing.X * 2) / 3, ButtonSizeWorld.Y);
            if (ImGuiEx.Button($"{i}##ward", buttonSize))
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskGoToResidentialDistrict.Enqueue(i);
            }
            if (i % 3 != 0) ImGui.SameLine();
        }
    }

    private void ResizeButton(string t)
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
        var canUse = Utils.CanUseAetheryte();
        var ret = canUse != AetheryteUseState.None;
        if(canUse == AetheryteUseState.Normal)
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
        else if(canUse == AetheryteUseState.Residential)
        {
            ret = P.Config.ShowAethernet;
        }
        if (canUse == AetheryteUseState.None)
        {
            bWidth = new(10, 10);
        }
        if(S.InstanceHandler.CanChangeInstance())
        {
            ret = true;
        }
        return ret && !(P.Config.HideAddon && Utils.IsAddonsVisible(Utils.Addons));
    }
}
