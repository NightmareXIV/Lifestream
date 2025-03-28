﻿using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.Sheets;
using NightmareUI;
using NightmareUI.PrimaryUI;
using Action = System.Action;

namespace Lifestream.GUI;

internal static unsafe class UISettings
{
    private static string AddNew = "";
    internal static void Draw()
    {
        NuiTools.ButtonTabs([[new("General", () => Wrapper(DrawGeneral)), new("Overlay", () => Wrapper(DrawOverlay))], [new("Expert", () => Wrapper(DrawExpert)), new("Service Accounts", () => Wrapper(UIServiceAccount.Draw)), new("Travel Block", TabTravelBan.Draw)]]);
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
            ImGui.Checkbox("Retry same-world failed world visits", ref P.Config.RetryWorldVisit);
            ImGui.Indent();
            ImGui.SetNextItemWidth(100f);
            ImGui.InputInt("Interval between retries, seconds##2", ref P.Config.RetryWorldVisitInterval.ValidateRange(1, 120));
            ImGui.SameLine();
            ImGuiEx.Text("+ up to");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            ImGui.InputInt("seconds##2", ref P.Config.RetryWorldVisitIntervalDelta.ValidateRange(0, 120));
            ImGuiEx.HelpMarker("To make it appear less bot-like");
            ImGui.Unindent();
            //ImGui.Checkbox("Use Return instead of Teleport when possible", ref P.Config.UseReturn);
            //ImGuiEx.HelpMarker("This includes any IPC calls");
        })

        .Section("Shortcuts")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.EnumCombo("\"/li\" command behavior", ref P.Config.LiCommandBehavior);
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
            if(Player.CID != 0) {
                ImGui.SetNextItemWidth(150f);
                var pref = P.Config.PreferredSharedEstates.SafeSelect(Player.CID);
                var name = pref switch
                {
                    (0, 0, 0) => "First available",
                    (-1, 0, 0) => "Disable",
                    _ => $"{ExcelTerritoryHelper.GetName((uint)pref.Territory)}, W{pref.Ward}, P{pref.Plot}"
                };
                if(ImGui.BeginCombo($"Preferred shared estate for {Player.NameWithWorld}", name))
                {
                    foreach(var x in Svc.AetheryteList.Where(x => x.IsSharedHouse))
                    {
                        if(ImGui.RadioButton("First available", pref == default))
                        {
                            P.Config.PreferredSharedEstates.Remove(Player.CID);
                        }
                        if(ImGui.RadioButton("Disable", pref == (-1,0,0)))
                        {
                            P.Config.PreferredSharedEstates[Player.CID] = (-1, 0, 0);
                        }
                        if(ImGui.RadioButton($"{ExcelTerritoryHelper.GetName(x.TerritoryId)}, Ward {x.Ward}, Plot {x.Plot}", pref == ((int)x.TerritoryId, x.Ward, x.Plot)))
                        {
                            P.Config.PreferredSharedEstates[Player.CID] = ((int)x.TerritoryId, x.Ward, x.Plot);
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            ImGui.Separator();
            ImGuiEx.Text("\"/li auto\" command priority:");
            ImGui.SameLine();
            if(ImGui.SmallButton("Reset")) P.Config.PropertyPrio.Clear();
            var dragDrop = Ref<ImGuiEx.RealtimeDragDrop<AutoPropertyData>>.Get(() => new("apddd", x => x.Type.ToString()));
            P.Config.PropertyPrio.AddRange(Enum.GetValues<TaskPropertyShortcut.PropertyType>().Where(x => x != TaskPropertyShortcut.PropertyType.Auto && !P.Config.PropertyPrio.Any(s => s.Type == x)).Select(x => new AutoPropertyData(false, x)));
            dragDrop.Begin();
            for(var i = 0; i < P.Config.PropertyPrio.Count; i++)
            {
                var d = P.Config.PropertyPrio[i];
                ImGui.PushID($"c{i}");
                dragDrop.NextRow();
                dragDrop.DrawButtonDummy(d, P.Config.PropertyPrio, i);
                ImGui.SameLine();
                ImGui.Checkbox($"{d.Type}", ref d.Enabled);
                ImGui.PopID();
            }
            dragDrop.End();
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
            ImGui.Checkbox($"Allow alternative world during DC transfer", ref P.Config.DcvUseAlternativeWorld);
            ImGuiEx.HelpMarker("If destination world isn't available but some other world on targeted data center is, it will be selected instead. Normal world visit will be enqueued after logging in.");
            ImGui.Checkbox($"Retry data center transfer if destination world is not available", ref P.Config.EnableDvcRetry);
            ImGui.Indent();
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("Max retries", ref P.Config.MaxDcvRetries.ValidateRange(1, int.MaxValue));
            ImGui.SetNextItemWidth(150f);
            ImGui.InputInt("Interval between retries, seconds", ref P.Config.DcvRetryInterval.ValidateRange(10, 1000));
            ImGui.Unindent();
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
        .Checkbox("Use Mount Roulette when auto-moving", () => ref P.Config.UseMount)
        .Checkbox("Use Sprint and Peloton when auto-moving", () => ref P.Config.UseSprintPeloton)

        .Section("Character Select Menu")
        .Checkbox("Enable Data center and World visit from Character Select Menu", () => ref P.Config.AllowDCTravelFromCharaSelect)
        .Checkbox("Use world visit instead of DC visit to travel to same world on guest DC", () => ref P.Config.UseGuestWorldTravel)

        .Section("Wotsit Integration")
        .Widget(() =>
        {
            var anyChanged = ImGui.Checkbox("Enable Wotsit Integration for teleporting to Aethernet destinations", ref P.Config.WotsitIntegrationEnabled);

            if(P.Config.WotsitIntegrationEnabled)
            {
                ImGui.Indent();
                if(ImGui.Checkbox("Include world select window", ref P.Config.WotsitIntegrationIncludes.WorldSelect))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include auto-teleport to property", ref P.Config.WotsitIntegrationIncludes.PropertyAuto))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to private estate", ref P.Config.WotsitIntegrationIncludes.PropertyPrivate))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to free company estate", ref P.Config.WotsitIntegrationIncludes.PropertyFreeCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to apartment", ref P.Config.WotsitIntegrationIncludes.PropertyApartment))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to inn room", ref P.Config.WotsitIntegrationIncludes.PropertyInn))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to grand company", ref P.Config.WotsitIntegrationIncludes.GrandCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to market board", ref P.Config.WotsitIntegrationIncludes.MarketBoard))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to island sanctuary", ref P.Config.WotsitIntegrationIncludes.IslandSanctuary))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include auto-teleport to aethernet destinations", ref P.Config.WotsitIntegrationIncludes.AetheryteAethernet))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include address book entries", ref P.Config.WotsitIntegrationIncludes.AddressBook))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include custom aliases", ref P.Config.WotsitIntegrationIncludes.CustomAlias))
                {
                    anyChanged = true;
                }
                ImGui.Unindent();
            }

            if(anyChanged)
            {
                PluginLog.Debug("Wotsit integration settings changed, re-initializing immediately");
                S.WotsitManager.TryClearWotsit();
                S.WotsitManager.MaybeTryInit(true);
            }
        })

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

                    ImGui.Unindent();
                }

                UtilsUI.NextSection();

                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt3("Button left/right padding", ref P.Config.ButtonWidthArray[0]);
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("Aetheryte button top/bottom padding", ref P.Config.ButtonHeightAetheryte);
                ImGui.SetNextItemWidth(100f);
                ImGui.InputInt("World button top/bottom padding", ref P.Config.ButtonHeightWorld);
                ImGui.Unindent();

                ImGui.Checkbox("Left-align text on buttons", ref P.Config.LeftAlignButtons);
            }
        })

        .Section("Instance changer")
        .Checkbox("Enabled", () => ref P.Config.ShowInstanceSwitcher)
        .Checkbox("Retry on failure", () => ref P.Config.InstanceSwitcherRepeat)
        .Checkbox("Return to the ground when flying before changing instance", () => ref P.Config.EnableFlydownInstance)
        .Widget("Display instance number in Server Info Bar", (x) =>
        {
            if(ImGui.Checkbox(x, ref P.Config.EnableDtrBar))
            {
                S.DtrManager.Refresh();
            }
        })
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
                    ImGuiEx.Text($"{Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(x)?.AethernetName.ValueNullable?.Name.ToString() ?? x.ToString()}");
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
