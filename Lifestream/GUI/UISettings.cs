using ECommons.Configuration;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.GeneratedSheets;
using NightmareUI;
using NightmareUI.PrimaryUI;
using System;
using Action = System.Action;

namespace Lifestream.GUI;

internal static unsafe class UISettings
{
    private static string AddNew = "";
    internal static void Draw()
    {
        NuiTools.ButtonTabs([[new("General", () => Wrapper(DrawGeneral)), new("Overlay", () => Wrapper(DrawOverlay)), new("Expert", () => Wrapper(DrawExpert))]]);
    }

    private static void Wrapper(Action action)
    {
        ImGui.Dummy(new(5f));
        action();
    }

    private static void DrawGeneral()
    {
        new NuiBuilder()
        .Section("Teleport Configuration")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.EnumCombo($"Teleport world change gateway", ref P.Config.WorldChangeAetheryte, Lang.WorldChangeAetherytes);
            ImGuiEx.HelpMarker($"Where would you like to teleport for world changes");
            ImGui.Checkbox($"Teleport to specific aethernet destination after world/dc visit", ref P.Config.WorldVisitTPToAethernet);
            if(P.Config.WorldVisitTPToAethernet)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(250f);
                ImGui.InputText("Aethernet destination, as if you'd use in \"/li\" command", ref P.Config.WorldVisitTPTarget, 50);
                ImGui.Checkbox($"Only teleport from command but not from overlay", ref P.Config.WorldVisitTPOnlyCmd);
                ImGui.Unindent();
            }
            ImGui.Checkbox($"Add firmament location into Foundation aetheryte", ref P.Config.Firmament);
            ImGui.Checkbox($"Automatically leave non cross-world party upon changing world", ref P.Config.LeavePartyBeforeWorldChange);
            ImGui.Checkbox($"Show teleport destination in chat", ref P.Config.DisplayChatTeleport);
            ImGui.Checkbox($"Show teleport destination in popup notifications", ref P.Config.DisplayPopupNotifications);
            
            //ImGui.Checkbox("Use Return instead of Teleport when possible", ref P.Config.UseReturn);
            //ImGuiEx.HelpMarker("This includes any IPC calls");
        })

        .Section("Shortcuts")
        .Widget(() =>
        {
            ImGui.Checkbox("When teleporting to your own apartment, enter inside", ref P.Config.EnterMyApartment);
            ImGui.SetNextItemWidth(150f);
            ImGuiEx.EnumCombo("When teleporting to your/fc house, perform this action", ref P.Config.HouseEnterMode);
            ImGui.SetNextItemWidth(150f);
            if(ImGui.BeginCombo("Preferred Inn", Utils.GetInnNameFromTerritory(P.Config.PreferredInn), ImGuiComboFlags.HeightLarge))
            {
                foreach(var x in (uint[])[0, .. TaskPropertyShortcut.InnData.Keys])
                {
                    if(ImGui.Selectable(Utils.GetInnNameFromTerritory(x), x == P.Config.PreferredInn)) P.Config.PreferredInn = x;
                }
                ImGui.EndCombo();
            }
            ImGui.Separator();
            ImGuiEx.Text("\"/li auto\" command priority:");
            for(int i = 0; i < P.Config.PropertyPrio.Count; i++)
            {
                var d = P.Config.PropertyPrio[i];
                ImGui.PushID($"c{i}");
                if(ImGui.ArrowButton("##up", ImGuiDir.Up) && i > 0)
                {
                    try
                    {
                        (P.Config.PropertyPrio[i - 1], P.Config.PropertyPrio[i]) = (P.Config.PropertyPrio[i], P.Config.PropertyPrio[i - 1]);
                    }
                    catch(Exception e)
                    {
                        e.Log();
                    }
                }
                ImGui.SameLine();
                if(ImGui.ArrowButton("##down", ImGuiDir.Down) && i < P.Config.PropertyPrio.Count - 1)
                {
                    try
                    {
                        (P.Config.PropertyPrio[i + 1], P.Config.PropertyPrio[i]) = (P.Config.PropertyPrio[i], P.Config.PropertyPrio[i + 1]);
                    }
                    catch(Exception e)
                    {
                        e.Log();
                    }
                }
                ImGui.SameLine();
                ImGui.Checkbox($"{d.Type}", ref d.Enabled);
                ImGui.PopID();
            }
            ImGui.Separator();
        })

        .Section("Map Integration")
        .Widget(() =>
        {
            ImGui.Checkbox("Click Aethernet Shard on map for quick teleport", ref P.Config.UseMapTeleport);
        })

        .Section("Cross-Datacenter")
        .Widget(() =>
        {
            ImGui.Checkbox($"Allow travelling to another data center", ref P.Config.AllowDcTransfer);
            ImGui.Checkbox($"Leave party before switching data center", ref P.Config.LeavePartyBeforeLogout);
            ImGui.Checkbox($"Teleport to gateway aetheryte before switching data center if not in sanctuary", ref P.Config.TeleportToGatewayBeforeLogout);
            ImGui.Checkbox($"Teleport to gateway aetheryte after completing data center travel", ref P.Config.DCReturnToGateway);
        })

        .Section("Address Book")
        .Widget(() =>
        {
            ImGui.Checkbox($"Disable pathing to a plot", ref P.Config.AddressNoPathing);
            ImGuiEx.HelpMarker($"You will be left at a closest aetheryte to the ward");
            ImGui.Checkbox($"Disable entering an apartment", ref P.Config.AddressApartmentNoEntry);
            ImGuiEx.HelpMarker($"You will be left at an entry confirmation dialogue");
        })

        .Section("Movement")
        .Checkbox("Use Sprint and Peloton when auto-moving", () => ref P.Config.UseSprintPeloton)

        .Draw();
    }

    private static void DrawOverlay()
    {
        new NuiBuilder()
        .Section("General Overlay Settings")
        .Widget(() =>
        {
            ImGui.Checkbox("Enable Overlay", ref P.Config.Enable);
            if(P.Config.Enable)
            {
                ImGui.Indent();
                ImGui.Checkbox($"Display Aethernet menu", ref P.Config.ShowAethernet);
                ImGui.Checkbox($"Display World Visit menu", ref P.Config.ShowWorldVisit);
                ImGui.Checkbox($"Display Housing Ward buttons", ref P.Config.ShowWards);

                UtilsUI.NextSection();

                ImGui.Checkbox("Fixed Lifestream Overlay position", ref P.Config.FixedPosition);
                if(P.Config.FixedPosition)
                {
                    ImGui.Indent();
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.EnumCombo("Horizontal base position", ref P.Config.PosHorizontal);
                    ImGui.SetNextItemWidth(200f);
                    ImGuiEx.EnumCombo("Vertical base position", ref P.Config.PosVertical);
                    ImGui.SetNextItemWidth(200f);
                    ImGui.DragFloat2("Offset", ref P.Config.Offset);

                    UtilsUI.NextSection();

                    ImGui.SetNextItemWidth(100f);
                    ImGui.InputInt("Button left/right padding", ref P.Config.ButtonWidth);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.InputInt("Aetheryte button top/bottom padding", ref P.Config.ButtonHeightAetheryte);
                    ImGui.SetNextItemWidth(100f);
                    ImGui.InputInt("World button top/bottom padding", ref P.Config.ButtonHeightWorld);

                    ImGui.Unindent();
                }
                ImGui.Unindent();
            }
        })

        .Section("Instance changer")
        .Checkbox("Enabled", () => ref P.Config.ShowInstanceSwitcher)
        .Checkbox("Retry on failure", () => ref P.Config.InstanceSwitcherRepeat)
        .Checkbox("Return to the ground when flying before changing instance", () => ref P.Config.EnableFlydownInstance)
        .SliderInt(150f, "Extra button height", () => ref P.Config.InstanceButtonHeight, 0, 50)
        .Widget("Reset Instance Data", (x) =>
        {
            if(ImGuiEx.Button(x, P.Config.PublicInstances.Count > 0))
            {
                P.Config.PublicInstances.Clear();
                EzConfig.Save();
            }
        })

        .Section("Game Window Integration")
        .Checkbox($"Hide Lifestream if the following game windows are open", () => ref P.Config.HideAddon)
        .If(() => P.Config.HideAddon)
        .Widget(() =>
        {
            if(ImGui.BeginTable("HideAddonTable", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("col1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("col2");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##addnew", "Window name... /xldata ai - to find it", ref AddNew, 100);
                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    P.Config.HideAddonList.Add(AddNew);
                    AddNew = "";
                }

                List<string> focused = [];
                try
                {
                    foreach(var x in RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries)
                    {
                        if(x.Value == null) continue;
                        focused.Add(x.Value->NameString);
                    }
                }
                catch(Exception e) { e.Log(); }

                if(focused != null)
                {
                    foreach(var name in focused)
                    {
                        if(name == null) continue;
                        if(P.Config.HideAddonList.Contains(name)) continue;
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV(EColor.Green, $"Focused: {name}");
                        ImGui.TableNextColumn();
                        ImGui.PushID(name);
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                        {
                            P.Config.HideAddonList.Add(name);
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.TableNextRow();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, 0x88888888);
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, 0x88888888);
                ImGui.TableNextColumn();
                ImGui.Dummy(new Vector2(5f));

                foreach(var s in P.Config.HideAddonList)
                {
                    ImGui.PushID(s);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(focused.Contains(s) ? EColor.Green : null, s);
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => P.Config.HideAddonList.Remove(s));
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        })
        .EndIf()
        .Draw();

        if(P.Config.Hidden.Count > 0)
        {
            new NuiBuilder()
            .Section("Hidden Aetherytes")
            .Widget(() =>
            {
                uint toRem = 0;
                foreach(var x in P.Config.Hidden)
                {
                    ImGuiEx.Text($"{Svc.Data.GetExcelSheet<Aetheryte>().GetRow(x)?.AethernetName.Value?.Name.ToString() ?? x.ToString()}");
                    ImGui.SameLine();
                    if(ImGui.SmallButton($"Delete##{x}"))
                    {
                        toRem = x;
                    }
                }
                if(toRem > 0)
                {
                    P.Config.Hidden.Remove(toRem);
                }
            })
            .Draw();
        }
    }

    private static void DrawExpert()
    {
        new NuiBuilder()
        .Section("Expert Settings")
        .Widget(() =>
        {
            ImGui.Checkbox($"Slow down aetheryte teleporting", ref P.Config.SlowTeleport);
            ImGuiEx.HelpMarker($"Slows down aethernet teleportation by specified amount.");
            if(P.Config.SlowTeleport)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(200f);
                ImGui.DragInt("Teleport delay (ms)", ref P.Config.SlowTeleportThrottle);
                ImGui.Unindent();
            }
            ImGuiEx.CheckboxInverted($"Skip waiting until game screen is ready", ref P.Config.WaitForScreenReady);
            ImGuiEx.HelpMarker($"Enable this option for faster teleports but be careful that you may get stuck.");
            ImGui.Checkbox($"Hide progress bar", ref P.Config.NoProgressBar);
            ImGuiEx.HelpMarker($"Hiding progress bar leaves you with no way to stop Lifestream from executing it's tasks.");
            ImGuiEx.CheckboxInverted($"Don't walk to nearby aetheryte on world change command from greater distance", ref P.Config.WalkToAetheryte);
        })
        .Draw();
    }
}
