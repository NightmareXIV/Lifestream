﻿using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ImGuiScene;
using Lifestream.Data;
using Lifestream.Tasks.Utility;
using OtterGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI;
public static class TabMultipath
{
    private static MultiPath Selected = null;
    private static int Cursor = -1;
    private static bool EditMode = false;

    public static void Draw()
    {
        if(IsKeyPressed((int)System.Windows.Forms.Keys.LButton)) Cursor = -1;
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add"))
        {
            var x = new MultiPath();
            P.Config.MultiPathes.Add(x);
            Selected = x;
            x.Name = x.GUID.ToString();
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "Paste"))
        {
            Safe(() =>
            {
                var mp = EzConfig.DefaultSerializationFactory.Deserialize<MultiPath>(Paste());
                mp.Name += " - copy";
                mp.GUID = Guid.NewGuid();
                P.Config.MultiPathes.Add(mp);
                Selected = mp;
            });
        }
        ImGui.SameLine();
        ImGuiEx.SetNextItemFullWidth();
        if(ImGui.BeginCombo($"##select", $"{Selected?.Name ?? "..."}"))
        {
            foreach(var x in P.Config.MultiPathes)
            {
                if(ImGui.Selectable($"{x.Name}##{x.GUID}"))
                {
                    Selected = x;
                }
            }
            ImGui.EndCombo();
        }

        if(Selected != null)
        {
            ImGui.SetNextItemWidth(200f);
            ImGui.InputText($"##name", ref Selected.Name, 100);
            ImGui.SameLine();
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.FastForward, "Execute", !P.TaskManager.IsBusy && Player.Interactable))
            {
                TaskMultipathExecute.Enqueue(Selected);
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Delete", ImGuiEx.Ctrl))
            {
                new TickScheduler(() => P.Config.MultiPathes.Remove(Selected));
                Selected = null;
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy"))
            {
                Copy(EzConfig.DefaultSerializationFactory.Serialize(Selected, false));
            }
            var currentPath = Selected?.Entries.FirstOrDefault(x => x.Territory == Svc.ClientState.TerritoryType);
            if(currentPath == null)
            {
                if(ImGui.Button($"Create for {ExcelTerritoryHelper.GetName(Svc.ClientState.TerritoryType)}"))
                {
                    Selected.Entries.Add(new() { Territory = Svc.ClientState.TerritoryType });
                }
            }
            else
            {
                if(!P.TaskManager.IsBusy) P.SplatoonManager.RenderPath(currentPath.Points, false);
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add at current position", EditMode))
                {
                    currentPath.Points.Add(Player.Object.Position);
                }
                if(EditMode && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    currentPath.Points.Insert(0, Player.Object.Position);
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.MousePointer, "Add at cursor", EditMode))
                {
                    currentPath.Points.Add(Player.Object.Position);
                    Cursor = currentPath.Points.Count - 1;
                }
                if(EditMode && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    currentPath.Points.Insert(0, Player.Object.Position);
                    Cursor = 0;
                }
                ImGui.SameLine();
                ImGui.Checkbox("Sprint", ref currentPath.Sprint);
                ImGui.SameLine();
                ImGui.Checkbox("Edit", ref EditMode);
                if(ImGui.BeginTable("Multipath", 3, ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Sort");
                    ImGui.TableSetupColumn("Point", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Control");
                    for(var i = 0; i < currentPath.Points.Count; i++)
                    {
                        var x = currentPath.Points[i];
                        ImGui.PushID(i);
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        if(ImGui.ArrowButton("##up", ImGuiDir.Up) && i > 0)
                        {
                            (currentPath.Points[i], currentPath.Points[i - 1]) = (currentPath.Points[i - 1], currentPath.Points[i]);
                        }
                        ImGui.SameLine(0, 1);
                        if(ImGui.ArrowButton("##down", ImGuiDir.Down) && i < currentPath.Points.Count - 1)
                        {
                            (currentPath.Points[i], currentPath.Points[i + 1]) = (currentPath.Points[i + 1], currentPath.Points[i]);
                        }

                        ImGui.TableNextColumn();

                        ImGuiEx.Text($"{x}");

                        ImGui.TableNextColumn();

                        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.MapPin, "To my pos", EditMode))
                        {
                            currentPath.Points[i] = Player.Object.Position;
                        }
                        ImGui.SameLine(0, 1);
                        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.MousePointer, "To cursor", EditMode))
                        {
                            Cursor = i;
                        }
                        ImGui.SameLine(0, 1);
                        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Del", ImGuiEx.Ctrl && EditMode))
                        {
                            var idx = i;
                            new TickScheduler(() => currentPath.Points.RemoveAt(idx));
                        }

                        if(Cursor == i)
                        {
                            if(Svc.GameGui.ScreenToWorld(ImGui.GetMousePos(), out var pos))
                            {
                                currentPath.Points[i] = pos;
                            }
                        }

                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }
            }
        }

    }
}
