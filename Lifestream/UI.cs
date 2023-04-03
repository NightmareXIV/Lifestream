using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal unsafe static class UI
    {
        internal static uint DebugTerritory = 0;
        internal static TinyAetheryte? DebugAetheryte = null;
        internal static int DC = 0;
        internal static void Draw()
        {
            if (ImGui.CollapsingHeader("Debug"))
            {
                ImGui.InputInt("DC", ref DC);
                if(ImGui.Button("Init DC"))
                {
                    P.DataStore.BuildWorlds((uint)DC);
                }
                ImGuiEx.InputUint("DebugTerritory", ref DebugTerritory);
                foreach(var x in P.DataStore.Aetherytes)
                {
                    ImGui.Separator();
                    if(ImGui.Button($"{x.Key.Name}")) DebugAetheryte = x.Key;
                    foreach(var l in x.Value)
                    {
                        if(ImGui.Button($"    {l.Name} {l.Position} {l.TerritoryType}")) DebugAetheryte = l;
                    }
                }
                if (ImGui.Button($"null")) DebugAetheryte = null;
            }
        }
    }
}
