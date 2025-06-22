using ECommons.SimpleGui;
using ECommons.SplatoonAPI;

namespace Lifestream.GUI;

public class ProgressOverlay : Window
{
    public ProgressOverlay() : base("Lifestream progress overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        EzConfigGui.WindowSystem.AddWindow(this);
        IsOpen = true;
        RespectCloseHotkey = false;
    }

    public override void PreDraw()
    {
        SizeConstraints = new()
        {
            MinimumSize = new(ImGuiHelpers.MainViewport.Size.X, 0),
            MaximumSize = new(0, float.MaxValue)
        };
    }

    public override void Draw()
    {
        CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
        if(ImGui.IsWindowHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SetTooltip("Right click to stop all tasks and movement");
            if(ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                P.TaskManager.Abort();
                P.followPath?.Stop();
            }
        }
        float percent;
        Vector4 col;
        string overlay;
        if(P.followPath != null && P.followPath.Waypoints.Count > 0)
        {
            percent = 1f - (float)P.FollowPath.Waypoints.Count / (float)P.FollowPath.MaxWaypoints;
            col = GradientColor.Get(EColor.Red, EColor.Violet);
            overlay = $"Lifestream Movement: {P.FollowPath.MaxWaypoints - P.FollowPath.Waypoints.Count}/{P.FollowPath.MaxWaypoints}";
            if(Splatoon.IsConnected())
            {
                S.Ipc.SplatoonManager.RenderPath(P.FollowPath.Waypoints);
            }
        }
        else
        {
            percent = 1f - (float)P.TaskManager.NumQueuedTasks / (float)P.TaskManager.MaxTasks;
            col = EColor.Violet;
            overlay = $"Lifestream Progress: {P.TaskManager.MaxTasks - P.TaskManager.NumQueuedTasks}/{P.TaskManager.MaxTasks}";
        }
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, col);
        ImGui.ProgressBar(percent, new(ImGui.GetContentRegionAvail().X, 20), overlay);
        ImGui.PopStyleColor();
        // Toggle ProgressOverlay position logic
        if(C.ProgressOverlayToTop)
        {
            Position = new(0, 0);
        }
        else
        {
            Position = new(0, ImGuiHelpers.MainViewport.Size.Y - ImGui.GetWindowSize().Y);
        }
    }

    public override bool DrawConditions()
    {
        //return ((P.TaskManager.IsBusy && P.TaskManager.MaxTasks > 0)) && !C.NoProgressBar;
        return ((P.TaskManager.IsBusy && P.TaskManager.MaxTasks > 0) || (P.followPath != null && P.followPath.Waypoints.Count > 0)) && !C.NoProgressBar;
    }
}
