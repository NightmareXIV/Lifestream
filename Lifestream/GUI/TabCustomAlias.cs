using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.SplatoonAPI;
using FFXIVClientStructs;
using Lifestream.Data;
using NightmareUI.ImGuiElements;
using Aetheryte = Lumina.Excel.Sheets.Aetheryte;

namespace Lifestream.GUI;
public static class TabCustomAlias
{
    public static void Draw()
    {
        ImGuiEx.Text(EColor.Orange, "Beta feature, please report bugs.");
        var selector = S.CustomAliasFileSystemManager.FileSystem.Selector;
        selector.Draw(150f);
        ImGui.SameLine();
        if(ImGui.BeginChild("Child"))
        {
            if(selector.Selected != null)
            {
                var item = selector.Selected;
                DrawAlias(item);
            }
            else
            {
                ImGuiEx.TextWrapped($"To begin, select an alias you want to edit or create a new one.");
            }
        }
        ImGui.EndChild();
    }

    private static void DrawAlias(CustomAlias selected)
    {
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add new"))
        {
            selected.Commands.Add(new());
        }
        ImGui.SameLine();
        ImGui.Checkbox("##en", ref selected.Enabled);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        if(!selected.Enabled) ImGui.BeginDisabled();
        ImGui.InputText($"##Alias", ref selected.Alias, 50);
        if(!selected.Enabled) ImGui.EndDisabled();
        ImGuiEx.Tooltip("Enabled");
        ImGui.SameLine();
        ImGuiEx.HelpMarker($"Will be available via \"/li {selected.Alias}\" command");
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Run", enabled: !Utils.IsBusy()))
        {
            selected.Enqueue();
        }
        ImGui.SameLine();
        ImGuiEx.Text("Visualisation:");
        ImGuiEx.PluginAvailabilityIndicator([new("Splatoon")]);
        for(var i = 0; i < selected.Commands.Count; i++)
        {
            var x = selected.Commands[i];
            ImGui.PushID(x.ID);
            if(ImGui.ArrowButton("##up", ImGuiDir.Up))
            {
                if(i > 0) (selected.Commands[i], selected.Commands[i - 1]) = (selected.Commands[i - 1], selected.Commands[i]);
            }
            ImGui.SameLine(0, 1);
            if(ImGui.ArrowButton("##down", ImGuiDir.Down))
            {
                if(i < selected.Commands.Count - 1) (selected.Commands[i], selected.Commands[i + 1]) = (selected.Commands[i + 1], selected.Commands[i]);
            }
            ImGui.SameLine(0, 1);
            ImGui.PopID();
            ImGuiEx.TreeNodeCollapsingHeader($"Command {i + 1}: {x.Kind}###{x.ID}", () => DrawCommand(x, selected), ImGuiTreeNodeFlags.CollapsingHeader);
            DrawSplatoon(x, i);
        }
    }

    private static void DrawSplatoon(CustomAliasCommand command, int index)
    {
        if(!Splatoon.IsConnected()) return;
        if(command.Kind == CustomAliasKind.Circular_movement)
        {
            {
                var point = P.SplatoonManager.GetNextPoint($"{index + 1}: Circular movement");
                point.SetRefCoord(command.CenterPoint.ToVector3());
                Splatoon.DisplayOnce(point);
            }
            {
                var point = P.SplatoonManager.GetNextPoint($"{index + 1}: Circular exit");
                point.SetRefCoord(command.CircularExitPoint);
                Splatoon.DisplayOnce(point);
            }
            {
                var point = P.SplatoonManager.GetNextPoint();
                point.SetRefCoord(command.CenterPoint.ToVector3());
                point.Filled = false;
                point.radius = command.Clamp == null ? Math.Clamp(Player.DistanceTo(command.CenterPoint), 1f, 10f) : (command.Clamp.Value.Min + command.Clamp.Value.Max) / 2f;
                Splatoon.DisplayOnce(point);
            }
        }
        else if(command.Kind == CustomAliasKind.Walk_to_point)
        {
            var point = P.SplatoonManager.GetNextPoint($"{index + 1}: Walk to");
            point.SetRefCoord(command.Point);
            Splatoon.DisplayOnce(point);
        }
        else if(command.Kind == CustomAliasKind.Navmesh_to_point)
        {
            var point = P.SplatoonManager.GetNextPoint($"{index + 1}: Navmesh to");
            point.SetRefCoord(command.Point);
            Splatoon.DisplayOnce(point);
        }
    }

    private static void DrawCommand(CustomAliasCommand command, CustomAlias selected)
    {
        ImGui.PushID(command.ID);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy"))
        {
            Copy(EzConfig.DefaultSerializationFactory.Serialize(command, false));
        }
        var aetherytes = Ref<uint[]>.Get("Aetherytes", () => Svc.Data.GetExcelSheet<Aetheryte>().Where(x => x.PlaceName.ValueNullable?.Name.ToString().IsNullOrEmpty() == false && x.IsAetheryte).Select(x => x.RowId).ToArray());
        var aetherytePlaceNames = Ref<Dictionary<uint, string>>.Get("Aetherytes", () => aetherytes.Select(Svc.Data.GetExcelSheet<Aetheryte>().GetRow).ToDictionary(x => x.RowId, x => x.PlaceName.Value.Name.ToString()));

        var aethernet = Ref<uint[]>.Get("Aethernet", () => Utils.GetAllRegisteredAethernetDestinations().ToArray());
        var aethernetNames = Ref<Dictionary<uint, string>>.Get("Aethernet", () => aethernet.Select(Svc.Data.GetExcelSheet<Aetheryte>().GetRow).ToDictionary(x => x.RowId, x => x.AethernetName.Value.Name.ToString()));
        ImGui.Separator();
        ImGui.SetNextItemWidth(150f);
        ImGuiEx.EnumCombo("Alias kind", ref command.Kind);
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Delete"))
        {
            new TickScheduler(() => selected.Commands.Remove(command));
        }

        if(command.Kind == CustomAliasKind.Teleport_to_Aetheryte)
        {
            ImGui.SetNextItemWidth(150f);
            ImGuiEx.Combo("Select aetheryte to teleport to", ref command.Aetheryte, aetherytes, names: aetherytePlaceNames);
            ImGui.SetNextItemWidth(60f);
            ImGui.DragFloat("Skip teleport if already at aetheryte within this range", ref command.SkipTeleport, 0.01f);
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Walk_to_point, CustomAliasKind.Navmesh_to_point))
        {
            Utils.DrawVector3Selector("walktopoint", ref command.Point);
        }

        if(command.Kind == CustomAliasKind.Change_world)
        {
            ImGui.SetNextItemWidth(150f);
            WorldSelector.Instance.Draw(ref command.World);
            ImGui.SameLine();
            ImGuiEx.Text("Select world");
        }

        if(command.Kind == CustomAliasKind.Use_Aethernet)
        {
            ImGui.SetNextItemWidth(150f);
            ImGuiEx.Combo("Select aethernet shard to teleport to", ref command.Aetheryte, aethernet, names: aethernetNames);
        }

        if(command.Kind == CustomAliasKind.Circular_movement)
        {
            if(ImGui.BeginTable("circular", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings))
            {
                ImGui.TableSetupColumn("1");
                ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Center point: ");
                ImGui.TableNextColumn();
                Utils.DrawVector2Selector("center", ref command.CenterPoint);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Exit point: ");
                ImGui.TableNextColumn();
                Utils.DrawVector3Selector("exit", ref command.CircularExitPoint);
                ImGui.Checkbox("Finish by walking to exit point", ref command.WalkToExit);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Precision: ");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragFloat("##precision", ref command.Precision.ValidateRange(4f, 100f), 0.01f);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Tolerance: ");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f);
                ImGui.DragInt("##tol", ref command.Tolerance.ValidateRange(1, (int)(command.Precision * 0.75f)), 0.01f);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Distance limit: ");
                ImGui.TableNextColumn();
                var en = command.Clamp != null;
                if(ImGui.Checkbox($"##clamp", ref en))
                {
                    if(en)
                    {
                        command.Clamp = (0, 10);
                    }
                    else
                    {
                        command.Clamp = null;
                    }
                }
                if(command.Clamp != null)
                {
                    var v = command.Clamp.Value;
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##prec1", ref v.Min, 0.01f);
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f);
                    ImGui.DragFloat("##prec2", ref v.Max, 0.01f);
                    if(v.Min < v.Max)
                    {
                        command.Clamp = v;
                    }
                    if(Svc.Targets.Target != null)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text($"To target: {Player.DistanceTo(Svc.Targets.Target):F1}");
                    }
                }

                ImGui.EndTable();
            }
        }
        if(command.Kind == CustomAliasKind.Interact)
        {
            ImGui.SetNextItemWidth(150f);
            ImGuiEx.InputUint("Data ID", ref command.DataID);
            ImGui.SameLine();
            if(ImGuiEx.Button("Target", Svc.Targets.Target?.DataId != 0))
            {
                command.DataID = Svc.Targets.Target.DataId;
            }
        }
        ImGui.PopID();
    }
}
