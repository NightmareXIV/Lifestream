using Lifestream.Schedulers;
using NightmareUI.ImGuiElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI.Windows;
public class GameCloseWindow : Window
{
    public int World = 0;
    private WorldSelector WorldSelector = new()
    {
        EmptyName = "Disabled",
    };
    public GameCloseWindow() : base("Lifestream Scheduler", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize)
    {
        RespectCloseHotkey = false;
        ShowCloseButton = false;
    }

    public override void Draw()
    {
        if(World == 0)
        {
            ImGuiEx.Text("Inactive, select target world");
        }
        else
        {
            ImGuiEx.Text(EColor.RedBright, "Active");
        }
        ImGuiEx.Text($"Shutdown game upon arriving to:");
        ImGui.SetNextItemWidth(200);
        WorldSelector.Draw(ref World);
    }
}
