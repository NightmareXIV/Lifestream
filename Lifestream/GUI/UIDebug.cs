using ECommons.Throttlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommons.Configuration;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ECommons.Automation;
using Lifestream.Schedulers;
using ECommons.GameFunctions;
using ECommons.UIHelpers;

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
            ImGuiEx.EzTabBar("debug",
                ("Data editor", Editor, null, true),
                ("AtkReader", Reader, null, true),
                ("Debug", Debug, null, true)
                );
        }

        static void Reader()
        {
            {
                if(TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && IsAddonReady(addon))
                {
                    var r = new ReaderLobbyDKTWorldList(addon);
                    ImGuiEx.Text($"Source: {r.Source}");
                    ImGuiEx.Text($"Destination: {r.Destination}");
                    foreach(var dc in r.Regions)
                    {
                        ImGuiEx.Text($"  {dc.RegionTitle}");
                        foreach(var world in dc.DataCenters)
                        {
                            ImGuiEx.Text($"    {world.Id}/{world.Name}");
                        }
                    }
                }
            }
            {
                if (TryGetAddonByName<AtkUnitBase>("TelepotTown", out var addon) && IsAddonReady(addon))
                {
                    var reader = new ReaderTelepotTown(addon);
                    for (int i = 0; i < reader.DestinationData.Count; i++)
                    {
                        var data = reader.DestinationData[i];
                        var name = reader.DestinationName[i];
                        ImGuiEx.Text($"{data.Type}|{data.State}|{data.CallbackData}|{data.IconID}|{name.Name}");
                    }
                }
            }
        }

        static int index = 0;
        static string str = "";
        static string str2 = "";
        static string str3 = "";
        static void Debug()
        {
            if (ImGui.CollapsingHeader("DCV"))
            {
                if(ImGui.Button("Enable AtkComponentTreeList_vf31Hook hook"))
                {
                    P.Memory.AtkComponentTreeList_vf31Hook.Enable();
                }
                {
                    if (TryGetAddonByName<AtkUnitBase>("LobbyDKTWorldList", out var addon) && ImGui.Button("Try event"))
                    {
                        //P.Memory.ConstructEvent(addon);
                        ImGuiEx.Text($"PTR: {(nint)(addon->UldManager.NodeList[7]->GetAsAtkComponentList() + 456):X16}");
                    }
                }
                if (ImGui.Button($"{nameof(DCChange.Logout)}")) PluginLog.Information($"{DCChange.Logout()}");
                if (ImGui.Button($"{nameof(DCChange.SelectYesLogout)}")) PluginLog.Information($"{DCChange.SelectYesLogout()}");
                if (ImGui.Button($"Enable AddonDKTWorldCheck_ReceiveEventHook")) P.Memory.AddonDKTWorldList_ReceiveEventHook.Enable();
                if (ImGui.Button($"{nameof(DCChange.TitleScreenClickStart)}")) PluginLog.Information($"{DCChange.TitleScreenClickStart()}") ;
                //if (ImGui.Button($"{nameof(DCChange.OpenContextMenuForChara)}")) PluginLog.Information($"{DCChange.OpenContextMenuForChara(str)}");
                ImGui.SameLine();
                ImGui.InputText($"Chara name", ref str, 100);
                if (ImGui.Button($"{nameof(DCChange.SelectVisitAnotherDC)}")) PluginLog.Information($"{DCChange.SelectVisitAnotherDC()}");
                if (ImGui.Button($"{nameof(DCChange.SelectTargetDataCenter)}")) PluginLog.Information($"{DCChange.SelectTargetDataCenter(str2)}");
                ImGui.SameLine();
                ImGui.InputText($"dc name", ref str2, 100);
                if (ImGui.Button($"{nameof(DCChange.SelectTargetWorld)}")) PluginLog.Information($"{DCChange.SelectTargetWorld(str3)}");
                ImGui.SameLine();
                ImGui.InputText($"w name", ref str3, 100);
                if (ImGui.Button($"{nameof(DCChange.ConfirmDcVisit)}")) PluginLog.Information($"{DCChange.ConfirmDcVisit()}");
                if (ImGui.Button($"{nameof(DCChange.ConfirmDcVisit2)}")) PluginLog.Information($"{DCChange.ConfirmDcVisit2()}");
                if (ImGui.Button($"{nameof(DCChange.SelectOk)}")) PluginLog.Information($"{DCChange.SelectOk()}");
                if (ImGui.Button($"{nameof(DCChange.ConfirmDcVisitIntention)}")) PluginLog.Information($"{DCChange.ConfirmDcVisitIntention()}");
                if (ImGui.Button($"{nameof(DCChange.SelectYesLogin)}")) PluginLog.Information($"{DCChange.SelectYesLogin()}");
                ImGui.InputInt("Index", ref index);
                if (ImGui.Button("Open context menu"))
                {
                    if (TryGetAddonByName<AtkUnitBase>("_CharaSelectListMenu", out var addon) && IsAddonReady(addon))
                    {
                        Callback.Fire(addon, false, (int)17, (int)1, (int)index);
                    }
                }
                ImGuiEx.TextWrapped($"Names: {Util.GetCharacterNames().Print()}");
            }
            if (ImGui.CollapsingHeader("Throttle"))
            {
                EzThrottler.ImGuiPrintDebugInfo();
                FrameThrottler.ImGuiPrintDebugInfo();
            }
            if (Svc.Targets.Target != null && Player.Available)
            {
                ImGuiEx.Text($"v.dist: {Svc.Targets.Target.Position.Y - Player.Object.Position.Y}");
                ImGuiEx.Text($"DTT3D: {Vector3.Distance(Svc.Targets.Target.Position, Player.Object.Position)}");
            }
        }

        static void Editor()
        {
            var bsize = ImGuiHelpers.GetButtonSize("A") with { X = 280 };
            if (ImGui.Button("Save"))
            {
                ImGui.SetClipboardText(JsonConvert.SerializeObject(P.DataStore.StaticData));
                P.DataStore.StaticData.SaveConfiguration(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, DataStore.FileName));
            }
            foreach (var x in P.DataStore.Aetherytes)
            {
                ImGui.Separator();
                if (ImGui.Button($"{x.Key.Name}", bsize))
                {
                    DebugAetheryte = x.Key;
                }
                {

                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(300);
                        var d = (int)P.DataStore.StaticData.Callback[x.Key.ID];
                        ImGui.SetNextItemWidth(100f);
                        if (ImGui.InputInt($"##{x.Key.Name}{x.Key.ID}data", ref d))
                        {
                            P.DataStore.StaticData.Callback[x.Key.ID] = (uint)d;
                        }
                    }
                    {
                        ImGui.SameLine();
                        if (!P.DataStore.StaticData.SortOrder.ContainsKey(x.Key.ID)) P.DataStore.StaticData.SortOrder[x.Key.ID] = 0;
                        var d = (int)P.DataStore.StaticData.SortOrder[x.Key.ID];
                        ImGui.SetNextItemWidth(100f);
                        if (ImGui.InputInt($"##{x.Key.Name}{x.Key.ID}sort", ref d))
                        {
                            P.DataStore.StaticData.SortOrder[x.Key.ID] = (uint)d;
                        }
                    }
                    if (ImGui.GetIO().KeyCtrl)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text($"{x.Key.Position}");
                    }
                    if (Svc.Targets.Target != null)
                    {
                        ImGui.SameLine();
                        if (ImGui.Button("Pos##" + x.Key.ID))
                        {
                            P.DataStore.StaticData.CustomPositions[x.Key.ID] = Svc.Targets.Target.Position;
                            DuoLog.Information($"Written {Svc.Targets.Target.Position} for {x.Key.ID}");
                        }
                    }
                }
                foreach (var l in x.Value)
                {
                    if (ImGui.Button($"    {l.Name}", bsize)) DebugAetheryte = l;
                    {
                        {
                            ImGui.SameLine();
                            ImGui.SetCursorPosX(300);
                            var d = (int)P.DataStore.StaticData.Callback[l.ID];
                            ImGui.SetNextItemWidth(100f);
                            if (ImGui.InputInt($"##{l.Name}{l.ID}data", ref d))
                            {
                                P.DataStore.StaticData.Callback[l.ID] = (uint)d;
                            }
                        }
                        {
                            ImGui.SameLine();
                            if (!P.DataStore.StaticData.SortOrder.ContainsKey(l.ID)) P.DataStore.StaticData.SortOrder[l.ID] = 0;
                            var d = (int)P.DataStore.StaticData.SortOrder[l.ID];
                            ImGui.SetNextItemWidth(100f);
                            if (ImGui.InputInt($"##{l.Name}{l.ID}sort", ref d))
                            {
                                P.DataStore.StaticData.SortOrder[l.ID] = (uint)d;
                            }
                        }
                        if (ImGui.GetIO().KeyCtrl)
                        {
                            ImGui.SameLine();
                            ImGuiEx.Text($"{l.Position}");
                        }
                        if (Svc.Targets.Target != null)
                        {
                            ImGui.SameLine();
                            if (ImGui.Button("Pos##" + l.ID))
                            {
                                P.DataStore.StaticData.CustomPositions[l.ID] = Svc.Targets.Target.Position;
                                DuoLog.Information($"Written {Svc.Targets.Target.Position} for {l.ID}");
                            }
                        }
                    }
                }
            }
            ImGuiEx.Text(Util.GetAvailableAethernetDestinations().Join("\n"));
            if (ImGui.Button($"null")) DebugAetheryte = null;
        }
    }
}
