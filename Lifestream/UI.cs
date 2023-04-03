using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal unsafe static class UI
    {
        internal static uint DebugAetheryte = 0;
        internal static void Draw()
        {
            if (ImGui.CollapsingHeader("Debug"))
            {
                ImGuiEx.InputUint("Debug aetheryte", ref DebugAetheryte);
                foreach(var x in P.DataStore.Aetherytes)
                {
                    ImGui.Separator();
                    ImGuiEx.Text($"{x.Key.Name}");
                    foreach(var l in x.Value)
                    {
                        ImGuiEx.Text($"    {l.Name}");
                    }
                }
            }
        }
    }
}
