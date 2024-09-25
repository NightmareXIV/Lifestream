using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lifestream.Data;
using Lumina.Excel.GeneratedSheets;
using NightmareUI;
using NightmareUI.ImGuiElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aetheryte = Lumina.Excel.GeneratedSheets.Aetheryte;

namespace Lifestream.GUI;
public static class TabCustomAlias
{
    public static void Draw()
    {
        ImGuiEx.Text(EColor.RedBright, "Alpha feature, please report bugs.");
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
        ImGui.SetNextItemWidth(150f);
        ImGui.InputText($"Alias", ref selected.Alias, 50);
        ImGuiEx.HelpMarker($"Will be available via \"/li {selected.Alias}\" command");
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
        }
    }

    private static void DrawCommand(CustomAliasCommand command, CustomAlias selected)
    {
        ImGui.PushID(command.ID);
        var aetherytes = Ref<uint[]>.Get("Aetherytes", () => Svc.Data.GetExcelSheet<Aetheryte>().Where(x => x.PlaceName.Value?.Name?.ToString().IsNullOrEmpty() == false && x.IsAetheryte).Select(x => x.RowId).ToArray());
        var names = Ref<Dictionary<uint, string>>.Get("Aetherytes", () => aetherytes.Select(Svc.Data.GetExcelSheet<Aetheryte>().GetRow).ToDictionary(x => x.RowId, x => x.PlaceName.Value.Name.ToString()));
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
            ImGuiEx.Combo("Select aetheryte to teleport to", ref command.Aetheryte, aetherytes, names: names);
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Walk_to_point, CustomAliasKind.Navmesh_to_point))
        {
            ImGui.SetNextItemWidth(200f);
            ImGui.InputFloat3("Point", ref command.Point);
            ImGui.SameLine();
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.User, "My position", Player.Available))
            {
                command.Point = Player.Position;
            }
        }

        if(command.Kind == CustomAliasKind.Change_world)
        {
            ImGui.SetNextItemWidth(150f);
            Ref<WorldSelector>.Get("Selector", () => new()).Draw(ref command.World);
            ImGui.SameLine();
            ImGuiEx.Text("Select world");
        }
        ImGui.PopID();
    }
}
