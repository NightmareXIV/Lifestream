namespace Lifestream.GUI;

internal class ProgressOverlay : Window
{
    public ProgressOverlay() : base("Lifestream progress overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        this.IsOpen = true;
        this.RespectCloseHotkey = false;
    }

    public override void PreDraw()
    {
        this.SizeConstraints = new()
        {
            MinimumSize = new(ImGuiHelpers.MainViewport.Size.X, 0),
            MaximumSize = new(0, float.MaxValue)
        };
    }

    public override void Draw()
    {
        CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
        if (ImGui.IsWindowHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip("Right click to stop all tasks");
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                P.TaskManager.Abort();
            }
        }
        var percent = 1f - (float)P.TaskManager.NumQueuedTasks / (float)P.TaskManager.MaxTasks;
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, EColor.Violet);
        ImGui.ProgressBar(percent, new(ImGui.GetContentRegionAvail().X, 20));
        ImGui.PopStyleColor();
        this.Position = new(0, ImGuiHelpers.MainViewport.Size.Y - ImGui.GetWindowSize().Y);
    }

    public override bool DrawConditions()
    {
        return P.TaskManager.IsBusy && P.TaskManager.MaxTasks > 0 && !P.Config.NoProgressBar;
    }
}
