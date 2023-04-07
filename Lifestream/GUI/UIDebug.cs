using ECommons.Throttlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommons.Configuration;

namespace Lifestream.GUI
{
    internal static unsafe class UIDebug
    {
        internal static uint DebugTerritory = 0;
        internal static TinyAetheryte? DebugAetheryte = null;
        internal static int DC = 0;
        internal static int Destination = 0;
        internal static void Draw()
        {
            if (ImGui.CollapsingHeader("Debug"))
            {
                if (ImGui.Button("Save"))
                {
                    ImGui.SetClipboardText(JsonConvert.SerializeObject(P.DataStore.StaticData));
                    P.DataStore.StaticData.SaveConfiguration(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, DataStore.FileName));
                }
                foreach (var x in P.DataStore.Aetherytes)
                {
                    ImGui.Separator();
                    if (ImGui.Button($"{x.Key.Name}"))
                    {
                        DebugAetheryte = x.Key;
                    }
                    {

                        ImGui.SameLine();
                        var d = (int)P.DataStore.StaticData.Data[x.Key.ID];
                        ImGui.SetNextItemWidth(100f);
                        if (ImGui.InputInt($"##{x.Key.Name}{x.Key.ID}data", ref d))
                        {
                            P.DataStore.StaticData.Data[x.Key.ID] = (uint)d;
                        }
                    }
                    foreach (var l in x.Value)
                    {
                        if (ImGui.Button($"    {l.Name}")) DebugAetheryte = l;
                        {

                            ImGui.SameLine();
                            var d = (int)P.DataStore.StaticData.Data[l.ID];
                            ImGui.SetNextItemWidth(100f);
                            if (ImGui.InputInt($"##{l.Name}{l.ID}data", ref d))
                            {
                                P.DataStore.StaticData.Data[l.ID] = (uint)d;
                            }
                        }
                    }
                }
                ImGuiEx.Text(Util.GetAvailableAethernetDestinations().Join("\n"));
                if (ImGui.Button($"null")) DebugAetheryte = null;
            }
            if (ImGui.CollapsingHeader("Throttle"))
            {
                EzThrottler.ImGuiPrintDebugInfo();
            }
        }
    }
}
