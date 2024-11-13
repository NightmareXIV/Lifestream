using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.SplatoonAPI;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lifestream.Data;
using Lifestream.Enums;
using NightmareUI;
using NightmareUI.PrimaryUI;

namespace Lifestream.GUI;
#nullable enable
public static unsafe class UIHouseReg
{
    public static ImGuiEx.RealtimeDragDrop<Vector3> PathDragDrop = new("UIHouseReg", (x) => x.ToString());

    public static void Draw()
    {
        if(Player.Available)
        {
            NuiTools.ButtonTabs([[new("Private House", DrawPrivate), new("Free Company House", DrawFC), new("Overview", DrawOverview)]]);
        }
        else
        {
            ImGuiEx.TextWrapped("Please log in to be able to create and edit registrations. ");
            DrawOverview();
        }
    }

    private static void DrawOverview()
    {
        var charas = P.Config.HousePathDatas.Select(x => x.CID).Distinct();
        if(ImGui.BeginTable("##charaTable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings))
        {
            ImGui.TableSetupColumn("Name or CID", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Private");
            ImGui.TableSetupColumn("FC");
            ImGui.TableSetupColumn("Workshop");
            ImGui.TableHeadersRow();

            foreach(var x in charas)
            {
                ImGui.PushID($"{x}");
                var priv = P.Config.HousePathDatas.FirstOrDefault(z => z.IsPrivate && z.CID == x);
                var fc = P.Config.HousePathDatas.FirstOrDefault(z => !z.IsPrivate && z.CID == x);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"{Utils.GetCharaName(x)}");
                ImGui.TableNextColumn();
                if(priv != null)
                {
                    NuiTools.RenderResidentialIcon((uint)priv.ResidentialDistrict.GetResidentialTerritory());
                    ImGui.SameLine();
                    ImGuiEx.Text($"W{priv.Ward + 1}, P{priv.Plot + 1}{(priv.PathToEntrance.Count > 0 ? ", +path" : "")}");
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton((FontAwesomeIcon)'\ue50b', "DelePrivate"))
                    {
                        new TickScheduler(() => P.Config.HousePathDatas.RemoveAll(z => z.IsPrivate && z.CID == x));
                    }
                    ImGuiEx.Tooltip("Cancel private house registration");
                    if(priv.PathToEntrance.Count > 0)
                    {
                        ImGui.SameLine();
                        if(ImGuiEx.IconButton((FontAwesomeIcon)'\ue566', "DelePrivatePath"))
                        {
                            priv.PathToEntrance.Clear();
                        }
                        ImGuiEx.Tooltip("Delete path to private house");
                    }
                }
                else
                {
                    ImGuiEx.TextV(ImGuiColors.DalamudGrey3, "Not registered");
                }

                ImGui.TableNextColumn();
                if(fc != null)
                {
                    NuiTools.RenderResidentialIcon((uint)fc.ResidentialDistrict.GetResidentialTerritory());
                    ImGui.SameLine();
                    ImGuiEx.Text($"W{fc.Ward + 1}, P{fc.Plot + 1}{(fc.PathToEntrance.Count > 0 ? ", +path" : "")}");
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton((FontAwesomeIcon)'\ue50b', "DeleFc"))
                    {
                        new TickScheduler(() => P.Config.HousePathDatas.RemoveAll(z => !z.IsPrivate && z.CID == x));
                    }
                    ImGuiEx.Tooltip("Cancel FC house registration");
                    if(fc.PathToEntrance.Count > 0)
                    {
                        ImGui.SameLine();
                        if(ImGuiEx.IconButton((FontAwesomeIcon)'\ue566', "DeleFcPath"))
                        {
                            fc.PathToEntrance.Clear();
                        }
                        ImGuiEx.Tooltip("Delete path to FC house");
                    }
                }
                else
                {
                    ImGuiEx.TextV(ImGuiColors.DalamudGrey3, "Not registered");
                }

                ImGui.TableNextColumn();
                if(fc == null || fc.PathToWorkshop.Count == 0)
                {
                    ImGuiEx.TextV(ImGuiColors.DalamudGrey3, "Not registered");
                }
                else
                {
                    ImGuiEx.TextV($"{fc.PathToWorkshop.Count} points");
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton((FontAwesomeIcon)'\ue566', "DeleFcWorkshopPath"))
                    {
                        fc.PathToWorkshop.Clear();
                    }
                    ImGuiEx.Tooltip("Delete path to workshop");
                }
                ImGui.PopID();
            }

            ImGui.EndTable();
        }
        
    }

    private static void DrawFC()
    {
        var data = Utils.GetFCPathData();
        DrawHousingData(data, false);
    }

    private static void DrawPrivate()
    {
        var data = Utils.GetPrivatePathData();
        DrawHousingData(data, true);
    }

    private static void DrawHousingData(HousePathData? data, bool isPrivate)
    {
        var plotDataAvailable = TryGetCurrentPlotInfo(out var kind, out var ward, out var plot);
        if(data == null)
        {
            ImGuiEx.Text($"No data found. ");
            if(plotDataAvailable && Player.IsInHomeWorld)
            {
                if(ImGui.Button($"Register {kind.GetName()}, ward {ward + 1}, plot {plot + 1} as {(isPrivate ? "private" : "free company")} house."))
                {
                    var newData = new HousePathData()
                    {
                        CID = Player.CID,
                        Plot = plot,
                        Ward = ward,
                        ResidentialDistrict = kind,
                        IsPrivate = isPrivate
                    };
                    P.Config.HousePathDatas.Add(newData);
                }
            }
            else
            {
                ImGuiEx.Text($"Go to your plot to register the data.");
            }
        }
        else
        {
            ImGuiEx.TextWrapped(ImGuiColors.ParsedGreen, $"{data.ResidentialDistrict.GetName()}, Ward {data.Ward + 1}, Plot {data.Plot + 1} is registered as {(data.IsPrivate ? "private" : "free company")} house.");
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Cancel registration", ImGuiEx.Ctrl))
            {
                P.Config.HousePathDatas.Remove(data);
            }
            ImGui.Checkbox("Override teleport behavior", ref data.EnableHouseEnterModeOverride);
            if(data.EnableHouseEnterModeOverride)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f);
                ImGuiEx.EnumCombo("##override", ref data.EnterModeOverride);
            }
            if(data.ResidentialDistrict == kind && data.Ward == ward && data.Plot == plot)
            {
                if(!IsInsideHouse())
                {
                    var path = data.PathToEntrance;
                    new NuiBuilder()
                        .Section("Path to house")
                        .Widget(() =>
                        {
                            ImGuiEx.TextWrapped($"Create path from plot entrance to house entrance. A path should have it's first point slightly inside your plot to which you can run in a straight line after teleporting and last point next to house entrance from where you can enter the house.");

                            ImGui.PushID($"path{isPrivate}");
                            DrawPathEditor(path, data);
                            ImGui.PopID();

                        }).Draw();
                }
                else if(!isPrivate)
                {
                    var path = data.PathToWorkshop;
                    new NuiBuilder()
                        .Section("Path to workshop")
                        .Widget(() =>
                        {
                            ImGuiEx.TextWrapped($"Create path from house entrance to workshop entrance. This feature can curretly only be used by external plugins.");

                            ImGui.PushID($"workshop");
                            DrawPathEditor(path, data);
                            ImGui.PopID();

                        }).Draw();
                }
                else
                {
                    ImGuiEx.TextWrapped("Go to registered plot to edit path");
                }
            }
            else
            {
                ImGuiEx.TextWrapped("Go to registered plot to edit path");
            }
        }
    }

    public static void DrawPathEditor(List<Vector3> path, HousePathData? data = null)
    {
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add to the end of the list"))
        {
            path.Add(Player.Position);
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add to the beginning of the list"))
        {
            path.Insert(0, Player.Position);
        }
        if(data != null)
        {
            var entryPoint = Utils.GetPlotEntrance(data.ResidentialDistrict.GetResidentialTerritory(), data.Plot);
            if(entryPoint != null)
            {
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Test", data.ResidentialDistrict.GetResidentialTerritory() == Player.Territory && Vector3.Distance(Player.Position, entryPoint.Value) < 10f))
                {
                    P.FollowPath.Move(data.PathToEntrance, true);
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Test Workshop", data.PathToWorkshop.Count > 0 && IsInsideHouse()))
                {
                    P.FollowPath.Move(data.PathToWorkshop, true);
                }
                if(ImGui.IsItemHovered())
                {
                    ImGuiEx.Tooltip($"""
                        ResidentialDistrict territory: {data.ResidentialDistrict.GetResidentialTerritory()}
                        Player territory: {Player.Territory}
                        Distance to entry point: {Vector3.Distance(Player.Position, entryPoint.Value)}
                        """);
                }
            }
        }
        PathDragDrop.Begin();
        if(ImGui.BeginTable($"pathtable", 4, ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("##num");
            ImGui.TableSetupColumn("##move");
            ImGui.TableSetupColumn("Coords", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("##control");
            ImGui.TableHeadersRow();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGuiEx.Text($"Entrance to plot");

            for(var i = 0; i < path.Count; i++)
            {
                ImGui.PushID($"point{i}");
                var p = path[i];
                ImGui.TableNextRow();
                PathDragDrop.SetRowColor(p.ToString());
                ImGui.TableNextColumn();
                PathDragDrop.NextRow();
                ImGuiEx.TextV($"{i + 1}");
                ImGui.TableNextColumn();
                PathDragDrop.DrawButtonDummy(p, path, i);
                Visualise();
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"{p:F1}");
                Visualise();

                ImGui.TableNextColumn();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.MapPin, "To my position"))
                {
                    path[i] = Player.Position;
                }
                Visualise();
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Delete", ImGuiEx.Ctrl))
                {
                    var toRem = i;
                    new TickScheduler(() => path.RemoveAt(toRem));
                }
                Visualise();
                ImGui.PopID();

                void Visualise()
                {
                    if(ImGui.IsItemHovered() && Splatoon.IsConnected())
                    {
                        var e = new Element(ElementType.CircleAtFixedCoordinates);
                        e.SetRefCoord(p);
                        e.Filled = false;
                        e.thicc = 2f;
                        e.radius = (Environment.TickCount64 % 1000f / 1000f) * 2f;
                        Splatoon.DisplayOnce(e);
                    }
                }
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGuiEx.Text($"Entrance to the house");

            ImGui.EndTable();
        }
        PathDragDrop.End();

        P.SplatoonManager.RenderPath(path, false, true);
    }

    private static bool IsOutside()
    {
        return P.ResidentialAethernet.ZoneInfo.ContainsKey(Svc.ClientState.TerritoryType);
    }

    private static bool IsInsideHouse()
    {
        return Svc.ClientState.TerritoryType.EqualsAny(
            Houses.Private_Cottage_Mist, Houses.Private_House_Mist, Houses.Private_Mansion_Mist,
            Houses.Private_Cottage_Empyreum, Houses.Private_House_Empyreum, Houses.Private_Mansion_Empyreum,
            Houses.Private_Cottage_Shirogane, Houses.Private_House_Shirogane, Houses.Private_Mansion_Shirogane,
            Houses.Private_Cottage_The_Goblet, Houses.Private_House_The_Goblet, Houses.Private_Mansion_The_Goblet,
            Houses.Private_Cottage_The_Lavender_Beds, Houses.Private_House_The_Lavender_Beds, Houses.Private_Mansion_The_Lavender_Beds
            );
    }

    public static bool TryGetCurrentPlotInfo(out ResidentialAetheryteKind kind, out int ward, out int plot)
    {
        var h = HousingManager.Instance();
        if(h != null)
        {
            ward = h->GetCurrentWard();
            plot = h->GetCurrentPlot();
            if(ward < 0 || plot < 0)
            {
                kind = default;
                return false;
            }
            kind = Utils.GetResidentialAetheryteByTerritoryType(Svc.ClientState.TerritoryType) ?? 0;
            return kind != 0;
        }
        kind = default;
        ward = default;
        plot = default;
        return false;
    }
}
