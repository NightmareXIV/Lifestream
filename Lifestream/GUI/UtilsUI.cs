namespace Lifestream.GUI;
public static class UtilsUI
{
    public static void DrawSection(string name, Vector4? color, Action drawAction)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(7f));
        if(ImGui.BeginTable(name, 1, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn(name, ImGuiTableColumnFlags.WidthStretch);
            if(color != null) ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, color.Value);
            ImGui.TableHeadersRow();
            if(color != null) ImGui.PopStyleColor();
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            Safe(drawAction);
            ImGui.EndTable();
        }
        ImGui.Dummy(new(5f));
        ImGui.PopStyleVar();
    }

    public static void NextSection()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
    }
}
