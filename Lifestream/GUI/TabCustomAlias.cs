using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods.TerritorySelection;
using ECommons.MathHelpers;
using ECommons.SplatoonAPI;
using FFXIVClientStructs;
using Lifestream.Data;
using Lifestream.Tasks.SameWorld;
using Newtonsoft.Json;
using NightmareUI;
using NightmareUI.ImGuiElements;
using Aetheryte = Lumina.Excel.Sheets.Aetheryte;

namespace Lifestream.GUI;
public static class TabCustomAlias
{
    private static ImGuiEx.RealtimeDragDrop<CustomAliasCommand> DragDrop = new("CusACmd", x => x.ID);
    private static readonly Vector4[] ChainColors = [ImGuiColors.DalamudRed, ImGuiColors.ParsedOrange, ImGuiColors.DalamudYellow, ImGuiColors.ParsedGreen, ImGuiColors.TankBlue, ImGuiColors.ParsedPurple];

    public static void Draw()
    {
        var selector = S.CustomAliasFileSystemManager.FileSystem.Selector;
        selector.Draw(150f.Scale());
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

    private static List<Action> PostTableActions = [];
    private static void DrawAlias(CustomAlias selected)
    {
        AssignAllChainGroups(selected);
        DrawSplatoon(selected);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add new"))
        {
            selected.Commands.Add(new() { Territory = Player.Available ? Player.Territory : 0 });
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "Paste"))
        {
            try
            {
                var result = JsonConvert.DeserializeObject<CustomAliasCommand>(Paste());
                if(result == null) throw new NullReferenceException();
                selected.Commands.Add(result);
            }
            catch(Exception e)
            {
                Notify.Error(e.Message);
                e.Log();
            }
        }
        ImGui.SameLine();
        ImGui.Checkbox("##en", ref selected.Enabled);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f.Scale());
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
        DragDrop.Begin();
        var cursor = ImGui.GetCursorPos();
        foreach(var x in PostTableActions)
        {
            x();
        }
        ImGui.SetCursorPos(cursor);
        PostTableActions.Clear();
        if(ImGuiEx.BeginDefaultTable(["Control", "~Command"], false))
        {
            for(var i = 0; i < selected.Commands.Count; i++)
            {
                var x = selected.Commands[i];
                var index = i;

                ImGui.TableNextRow();
                DragDrop.SetRowColor(x.ID);
                ImGui.TableNextColumn();
                DragDrop.NextRow();
                DragDrop.DrawButtonDummy(x, selected.Commands, i);
                ImGui.TableNextColumn();
                var curpos = ImGui.GetCursorPos() + ImGui.GetContentRegionAvail() with { Y = 0 };
                if(x.Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point, CustomAliasKind.Circular_movement))
                {
                    var insertIndex = i + 1;
                    PostTableActions.Add(() =>
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        var pos = curpos
                            - ImGuiHelpers.GetButtonSize("\uf0ab") with { Y = 0 }
                            - new Vector2(ImGui.CalcTextSize(FontAwesomeIcon.Link.ToIconString()).X + ImGui.GetStyle().ItemSpacing.X, 0);
                        ImGui.SetCursorPos(pos);
                        ImGui.PopFont();
                        ImGui.AlignTextToFramePadding();
                        var col = x.ChainGroup == 0 ? Vector4.Zero : TabCustomAlias.ChainColors.SafeSelect(x.ChainGroup - 1);
                        ImGuiEx.Text(col, UiBuilder.IconFont, FontAwesomeIcon.Link.ToIconString());
                        ImGui.SameLine();
                        if(ImGuiEx.IconButton((FontAwesomeIcon)'\uf0ab', x.ID, enabled: Player.Available))
                        {
                            new TickScheduler(() =>
                            {
                                selected.Commands.Insert(insertIndex, new()
                                {
                                    Kind = CustomAliasKind.Move_to_point,
                                    UseFlight = x.UseFlight,
                                    Point = Player.Position,
                                    Territory = Player.Territory == x.Territory ? x.Territory : (Player.Available ? Player.Territory : 0),
                                });
                            });
                        }
                        ImGuiEx.Tooltip($"Create move command after this command with player's position and territory");
                    });
                }

                ImGuiEx.TreeNodeCollapsingHeader($"Command {i + 1}: {x.Kind.ToString().Replace('_', ' ')}{GetExtraText(x)}###{x.ID}", () => DrawCommand(x, selected, i), ImGuiTreeNodeFlags.CollapsingHeader);
                DrawSplatoon(x, i);
            }
            ImGui.EndTable();
        }
        DragDrop.End();
    }

    private static string GetExtraText(CustomAliasCommand x)
    {
        if(x.Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point))
        {
            if(x.Territory == 0)
            {
                return $" {x.Point:F1}";
            }
            else
            {
                return $" {x.Point:F1} [{ExcelTerritoryHelper.GetName(x.Territory)}]";
            }
        }
        else if(x.Kind == CustomAliasKind.Circular_movement)
        {
            if(x.Territory != 0)
            {
                return $" [{ExcelTerritoryHelper.GetName(x.Territory)}]";
            }
        }
        else if(x.Kind == CustomAliasKind.Teleport_to_Aetheryte)
        {
            if(AetherytePlaceNames.TryGetValue(x.Aetheryte, out var ret))
            {
                return $" [{ret}]";
            }
        }
        else if(x.Kind == CustomAliasKind.Use_Aethernet)
        {
            return $" [{Utils.KnownAetherytes.SafeSelect(x.Aetheryte, x.Aetheryte.ToString())}]";
        }
        else if(x.Kind == CustomAliasKind.Change_world)
        {
            return $" [{ExcelWorldHelper.GetName(x.World)}]";
        }
        else if(x.Kind == CustomAliasKind.Interact)
        {
            return $" [{x.DataID}]";
        }
        return "";
    }

    private static void DrawSplatoon(CustomAlias alias)
    {
        if(!Splatoon.IsConnected()) return;
        {
            var lines = Utils.GenerateGroupConnectionLines(alias);
            foreach(var x in lines)
            {
                var line = S.Ipc.SplatoonManager.GetNextLine(EColor.GreenBright, 1f);
                line.SetRefCoord(x.Start);
                line.SetOffCoord(x.End);
                Splatoon.DisplayOnce(line);
            }
        }
        {
            var lines = Utils.GenerateGroupConnectionLines(alias, 0.25f);
            foreach(var x in lines)
            {
                var line = S.Ipc.SplatoonManager.GetNextLine(EColor.YellowBright, 1f);
                line.SetRefCoord(x.Start);
                line.SetOffCoord(x.End);
                Splatoon.DisplayOnce(line);
            }
        }
    }

    private static void DrawSplatoon(CustomAliasCommand command, int index)
    {
        if(!Splatoon.IsConnected()) return;
        if(command.Kind == CustomAliasKind.Circular_movement)
        {
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}: Circular movement");
                point.SetRefCoord(command.CenterPoint.ToVector3());
                Splatoon.DisplayOnce(point);
            }
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}: Circular exit");
                point.SetRefCoord(command.CircularExitPoint);
                Splatoon.DisplayOnce(point);
            }
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint();
                point.SetRefCoord(command.CenterPoint.ToVector3());
                point.Filled = false;
                point.radius = command.Clamp == null ? Math.Clamp(Player.DistanceTo(command.CenterPoint), 1f, 10f) : (command.Clamp.Value.Min + command.Clamp.Value.Max) / 2f;
                Splatoon.DisplayOnce(point);
            }
        }
        else if(command.Kind == CustomAliasKind.Move_to_point)
        {
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}: Walk to");
                point.SetRefCoord(command.Point);
                point.radius = command.Scatter;
                point.color = EColor.RedBright.ToUint();
                point.thicc = 2f;
                Splatoon.DisplayOnce(point);
            }
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint();
                point.SetRefCoord(command.Point);
                point.radius = command.Scatter + 0.25f;
                point.color = EColor.YellowBright.ToUint();
                point.thicc = 1f;
                Splatoon.DisplayOnce(point);
            }
        }
        else if(command.Kind == CustomAliasKind.Navmesh_to_point)
        {
            var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}: Navmesh to");
            point.SetRefCoord(command.Point);
            Splatoon.DisplayOnce(point);
        }
    }


    private static readonly uint[] Aetherytes = Svc.Data.GetExcelSheet<Aetheryte>().Where(x => x.PlaceName.ValueNullable?.Name.ToString().IsNullOrEmpty() == false && x.IsAetheryte).Select(x => x.RowId).ToArray();
    private static readonly Dictionary<uint, string> AetherytePlaceNames = Aetherytes.Select(Svc.Data.GetExcelSheet<Aetheryte>().GetRow).ToDictionary(x => x.RowId, x => x.PlaceName.Value.Name.ToString());

    private static void DrawCommand(CustomAliasCommand command, CustomAlias selected, int index)
    {
        ImGui.PushID(command.ID);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "Copy"))
        {
            Copy(EzConfig.DefaultSerializationFactory.Serialize(command, false));
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "Delete", ImGuiEx.Ctrl))
        {
            new TickScheduler(() => selected.Commands.Remove(command));
        }
        ImGuiEx.Tooltip("Press CTRL and click");

        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "Run", enabled: !Utils.IsBusy()))
        {
            selected.Enqueue(inclusiveStart: index, exclusiveEnd: index + 1);
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.AngleDoubleDown, "Run and continue", enabled: !Utils.IsBusy()))
        {
            selected.Enqueue(inclusiveStart: index);
        }

        ImGui.Separator();
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("Alias kind", ref command.Kind);

        if(command.Kind == CustomAliasKind.Teleport_to_Aetheryte)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.Combo("Select aetheryte to teleport to", ref command.Aetheryte, Aetherytes, names: AetherytePlaceNames);
            ImGui.SetNextItemWidth(60f.Scale());
            ImGui.DragFloat("Skip teleport if already at aetheryte within this range", ref command.SkipTeleport, 0.01f);
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point))
        {
            Utils.DrawVector3Selector($"walktopoint{command.ID}", ref command.Point);
            ImGui.SameLine();
            ImGuiEx.Text(UiBuilder.IconFont, FontAwesomeIcon.ArrowsLeftRight.ToIconString());
            ImGui.SameLine(0, 1);
            ImGui.SetNextItemWidth(50f.Scale());
            ImGuiEx.SliderFloat($"##scatter", ref command.Scatter, 0f, 2f);
            ImGuiEx.Tooltip("Scatter. Double-click to input manually.");
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Move_to_point))
        {
            drawFlight();
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point, CustomAliasKind.Circular_movement))
        {
            ImGui.SetNextItemWidth(150f.Scale());
            if(ImGui.BeginCombo("Restrict movement to zone", command.Territory == 0 ? "No Restriction" : ExcelTerritoryHelper.GetName(command.Territory)))
            {
                _ = new TerritorySelector((TerritorySelector sel, uint territory) =>
                {
                    command.Territory = territory;
                })
                {
                    Mode = TerritorySelector.DisplayMode.PlaceNameDutyUnion,
                    SelectedCategory = TerritorySelector.Category.All,
                };
                ImGui.CloseCurrentPopup();
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButton(FontAwesomeIcon.Eraser, enabled: command.Territory != 0))
            {
                command.Territory = 0;
            }
            ImGuiEx.Tooltip("Remove Requirement");
            ImGui.SameLine(0, 1);
            if(ImGuiEx.IconButton(FontAwesomeIcon.MapPin, enabled: Player.Available))
            {
                command.Territory = Player.Territory;
            }
            ImGui.SameLine(0, 1);
            ImGuiEx.Tooltip($"Set to {ExcelTerritoryHelper.GetName(Player.Territory)}");
            if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowDownUpAcrossLine))
            {
                ImGui.OpenPopup("SpreadTerritory");
            }
            ImGuiEx.Tooltip("Copy this property to adjacent commands...");
            if(ImGui.BeginPopup("SpreadTerritory"))
            {
                ImGuiEx.Text($"Copy territory requirement:\n{ExcelTerritoryHelper.GetName(command.Territory)}");
                ImGuiEx.TextV("Up or down until command number:");
                ImGui.SetNextItemWidth(150f.Scale());
                ImGuiEx.FilteringInputInt("##cmdNum", out var cmdNum);
                ImGui.SameLine();
                if(ImGui.Button("OK"))
                {
                    selected.CopyTerritoryRange(index, cmdNum);
                    ImGui.CloseCurrentPopup();
                }
                if(ImGui.Selectable("To all the commands within this alias"))
                {
                    selected.Commands.Each(x => x.Territory = command.Territory);
                }
                ImGui.EndPopup();
            }
        }


        if(command.Kind.EqualsAny(CustomAliasKind.Navmesh_to_point))
        {
            ImGui.SameLine(0, 1);
            ImGuiEx.ButtonCheckbox(FontAwesomeIcon.FastForward, ref command.UseTA, EColor.Green);
            ImGuiEx.Tooltip("Use TextAdvance for movement. Flight settings are inherited from TextAdvance.");
            if(!command.UseTA)
            {
                drawFlight();
            }
        }

        if(command.Kind == CustomAliasKind.Move_to_point)
        {
            if(command.ExtraPoints.Count > 0)
            {
                ImGuiEx.Text("Extra Points:");
            }
            ImGui.Indent();
            for(var i = 0; i < command.ExtraPoints.Count; i++)
            {
                var pointIndex = i;
                ImGui.PushID($"Point{i}");
                var x = command.ExtraPoints[i];
                Utils.DrawVector3Selector($"walktopointextra{i}{command.ID}", ref x);
                if(x != command.ExtraPoints[i])
                {
                    command.ExtraPoints[i] = x;
                }
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    new TickScheduler(() => command.ExtraPoints.RemoveAt(pointIndex));
                }
                ImGui.PopID();
            }
            ImGui.Unindent();

            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add Extra Point"))
            {
                command.ExtraPoints.Add(new());
            }
            ImGuiEx.Tooltip("Random point will be selected. Scatter will remain the same across all points.");
        }

        void drawFlight()
        {
            ImGui.SameLine(0, 1);
            ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Plane, ref command.UseFlight, EColor.Green);
            ImGuiEx.Tooltip("Fly for movement. Don't forget to use \"Mount Up\" command before. ");
        }

        if(command.Kind == CustomAliasKind.Change_world)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            WorldSelector.Instance.Draw(ref command.World);
            ImGui.SameLine();
            ImGuiEx.Text("Select world");
        }

        if(command.Kind == CustomAliasKind.Use_Aethernet)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            if(ImGui.BeginCombo("Select aethernet shard to teleport to", command.Aetheryte == 0 ? "- Not selected -" : Utils.KnownAetherytes.SafeSelect(command.Aetheryte, command.Aetheryte.ToString()), ImGuiComboFlags.HeightLarge))
            {
                ref var filter = ref Ref<string>.Get($"Filter{command.ID}");
                ImGui.SetNextItemWidth(200f);
                ImGui.InputTextWithHint("##filter", "Filter", ref filter, 50);
                foreach(var x in Utils.KnownAetherytesByCategories)
                {
                    bool shouldHide(ref string filter, KeyValuePair<uint, string> v) => filter.Length > 0 && !v.Value.Contains(filter, StringComparison.OrdinalIgnoreCase) && !x.Key.Contains(filter, StringComparison.OrdinalIgnoreCase);
                    foreach(var v in x.Value)
                    {
                        if(!shouldHide(ref filter, v)) goto Display;
                    }
                    continue;
                Display:
                    ImGuiEx.Text(EColor.YellowBright, $"{x.Key}:");
                    ImGui.Indent();
                    foreach(var v in x.Value)
                    {
                        if(shouldHide(ref filter, v)) continue;
                        var sel = command.Aetheryte == v.Key;
                        if(sel && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                        if(ImGui.Selectable($"{v.Value}##{v.Key}", sel))
                        {
                            command.Aetheryte = v.Key;
                        }
                    }
                    ImGui.Unindent();
                    ImGui.Separator();
                }
                ImGui.EndCombo();
            }
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
                Utils.DrawVector3Selector($"exit{command.ID}", ref command.CircularExitPoint);
                ImGui.Checkbox("Finish by walking to exit point", ref command.WalkToExit);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Precision: ");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.DragFloat("##precision", ref command.Precision.ValidateRange(4f, 100f), 0.01f);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"Tolerance: ");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f.Scale());
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
                    ImGui.SameLine(0, 1);
                    ImGui.SetNextItemWidth(50f.Scale());
                    ImGui.DragFloat("##prec1", ref v.Min, 0.01f);
                    ImGui.SameLine(0, 1);
                    ImGuiEx.Text("-");
                    ImGui.SameLine(0, 1);
                    ImGui.SetNextItemWidth(50f.Scale());
                    ImGui.DragFloat("##prec2", ref v.Max, 0.01f);
                    if(v.Min < v.Max)
                    {
                        command.Clamp = v;
                    }
                    if(Svc.Targets.Target != null)
                    {
                        ImGui.SameLine(0, 1);
                        ImGuiEx.Text($"To target: {Player.DistanceTo(Svc.Targets.Target):F1}");
                    }
                }

                ImGui.EndTable();
            }
        }
        if(command.Kind == CustomAliasKind.Interact)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.InputUint("Data ID", ref command.DataID);
            ImGui.SameLine(0, 1);
            if(ImGuiEx.Button("Target", Svc.Targets.Target?.DataId != 0))
            {
                command.DataID = Svc.Targets.Target.DataId;
            }
            ImGuiEx.InputFloat(100f, "Approach until reaching distance", ref command.InteractDistance, 1, 1);
        }
        if(command.Kind == CustomAliasKind.Mount_Up)
        {
            ImGui.Checkbox("Only mount up if enabled in configuration", ref command.MountUpConditional);
        }
        if(command.Kind.EqualsAny(CustomAliasKind.Select_Yes, CustomAliasKind.Select_List_Option))
        {
            ImGuiEx.TextWrapped($"List entries that you would like to select/confirm:");
            if(ImGuiEx.BeginDefaultTable("ItemLst", ["~1", "2"], false))
            {
                for(var i = 0; i < command.SelectOption.Count; i++)
                {
                    ImGui.PushID(i);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    var str = command.SelectOption[i];
                    if(ImGui.InputText("##selectOpt", ref str, 500))
                    {
                        command.SelectOption[i] = str;
                    }
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        var idx = i;
                        new TickScheduler(() => command.SelectOption.RemoveAt(idx));
                    }
                    ImGui.PopID();
                }
                ImGui.EndTable();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add New Option"))
                {
                    command.SelectOption.Add("");
                }
            }
        }
        if(command.Kind.EqualsAny(CustomAliasKind.Select_Yes, CustomAliasKind.Select_List_Option, CustomAliasKind.Confirm_Contents_Finder))
        {
            ImGui.Checkbox("Skip on screen fade", ref command.StopOnScreenFade);
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Wait_for_Transition))
        {
            ImGui.Checkbox("Require territory change", ref command.RequireTerritoryChange);
        }
        ImGui.PopID();
    }

    public static void AssignAllChainGroups(CustomAlias alias)
    {
        var commands = alias.Commands;
        if(commands == null || commands.Count == 0)
        {
            return;
        }

        var nextChainGroup = 1;

        for(var i = 0; i < commands.Count;)
        {
            if(i < commands.Count - 1 && Utils.IsChainedWithNext(alias, i))
            {
                var start = i;
                var end = i;

                while(end < commands.Count - 1 && Utils.IsChainedWithNext(alias, end))
                {
                    end++;
                }

                for(var j = start; j <= end; j++)
                {
                    commands[j].ChainGroup = nextChainGroup;
                }

                nextChainGroup++;
                i = end + 1;
            }
            else
            {
                commands[i].ChainGroup = 0;
                i++;
            }
        }
    }
}
