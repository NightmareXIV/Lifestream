using ECommons.GameHelpers;
using NightmareUI;
using NightmareUI.ImGuiElements;
using NightmareUI.PrimaryUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI;
public static class TabUtility
{
    public static int TargetWorldID = 0;
    static WorldSelector WorldSelector = new()
    {
        DisplayCurrent = false,
        EmptyName = "Disabled",
        ShouldHideWorld = (x) => x == Player.Object?.CurrentWorld.RowId
    };

    public static void Draw()
    {
        new NuiBuilder()
            .Section("Shutdown game upon arriving to the world")
            .Widget(() =>
            {
                ImGuiEx.SetNextItemFullWidth();
                WorldSelector.Draw(ref TargetWorldID);
            })
            .Draw();
    }
}
