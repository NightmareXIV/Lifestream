using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI
{
    internal class ProgressOverlay : Window
    {
        public ProgressOverlay() : base("Lifestream progress overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysAutoResize)
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
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Colors.Violet);
            ImGui.ProgressBar(percent, new(ImGui.GetContentRegionAvail().X, 20));
            ImGui.PopStyleColor();
            this.Position = new(0, ImGuiHelpers.MainViewport.Size.Y - ImGui.GetWindowSize().Y);
        }

        public override bool DrawConditions()
        {
            return P.TaskManager.IsBusy && P.TaskManager.MaxTasks > 0;
        }
    }
}
