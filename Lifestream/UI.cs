using ECommons.Configuration;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lifestream
{
    internal unsafe static class UI
    {
        internal static uint DebugTerritory = 0;
        internal static TinyAetheryte? DebugAetheryte = null;
        internal static int DC = 0;
        internal static int Destination = 0;
        internal static void Draw()
        {
            if (ImGui.CollapsingHeader("Debug"))
            {
                /*if(TryGetAddonByName<AtkUnitBase>("TelepotTown", out var telep) && IsAddonReady(telep))
                {
                    ImGui.InputInt("dest", ref Destination);
                    if (ImGui.Button("Fire"))
                    {
                        Callback(telep, (int)11, (uint)Destination);
                        Callback(telep, (int)11, (uint)Destination);
                    }
                }
                if(Svc.Targets.Target != null)
                {
                    ImGuiEx.Text($"Vector2 distance to target: {Vector2.Distance(Svc.ClientState.LocalPlayer.Position.ToVector2(), Svc.Targets.Target.Position.ToVector2())}");
                    ImGuiEx.Text($"Vector3 distance to target: {Vector3.Distance(Svc.ClientState.LocalPlayer.Position, Svc.Targets.Target.Position)}");
                }
                ImGui.InputInt("DC", ref DC);
                if(ImGui.Button("Init DC"))
                {
                    P.DataStore.BuildWorlds((uint)DC);
                }
                ImGuiEx.InputUint("DebugTerritory", ref DebugTerritory);*/
                if (ImGui.Button("Save"))
                {
                    ImGui.SetClipboardText(JsonConvert.SerializeObject(P.DataStore.CallbackData));
                    EzConfig.SaveConfiguration(P.DataStore.CallbackData, Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "CallbackData.json"));
                }
                foreach(var x in P.DataStore.Aetherytes)
                {
                    ImGui.Separator();
                    if (ImGui.Button($"{x.Key.Name}"))
                    {
                        DebugAetheryte = x.Key;
                    }
                    {

                        ImGui.SameLine();
                        var d = (int)P.DataStore.CallbackData.Data[x.Key.ID];
                        ImGui.SetNextItemWidth(100f);
                        if (ImGui.InputInt($"##{x.Key.Name}{x.Key.ID}data", ref d))
                        {
                            P.DataStore.CallbackData.Data[x.Key.ID] = (uint)d;
                        }
                    }
                    foreach(var l in x.Value)
                    {
                        if(ImGui.Button($"    {l.Name}")) DebugAetheryte = l;
                        {

                            ImGui.SameLine();
                            var d = (int)P.DataStore.CallbackData.Data[l.ID];
                            ImGui.SetNextItemWidth(100f);
                            if (ImGui.InputInt($"##{l.Name}{l.ID}data", ref d))
                            {
                                P.DataStore.CallbackData.Data[l.ID] = (uint)d;
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
