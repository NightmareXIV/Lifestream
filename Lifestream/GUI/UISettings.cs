using BadgeOverlay;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lifestream.Data;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.Sheets;
using NightmareUI;
using NightmareUI.Censoring;
using NightmareUI.PrimaryUI;
using System.Globalization;
using System.Threading;
using TerraFX.Interop.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Action = System.Action;

namespace Lifestream.GUI;

internal static unsafe class UISettings
{
    private static string AddNew = "";
    internal static void Draw()
    {
        NuiTools.ButtonTabs([
            [new("General", () => Wrapper(DrawGeneral)),
            new("Badge Overlay", () => Wrapper(DrawOverlay)),
            new("Legacy Overlay", () => Wrapper(DrawLegacyOverlay)),
            ], 
            [new("Expert", () => Wrapper(DrawExpert)), 
            new("Service Accounts", () => Wrapper(UIServiceAccount.Draw)), 
            new("Travel Block", TabTravelBan.Draw)]]);
    }

    private static void Wrapper(Action action)
    {
        ImGui.Dummy(new(5f));
        action();
    }

    private static void DrawLegacyOverlay()
    {
        new NuiBuilder()
        .Section("General Overlay Settings")
        .Widget(() =>
        {
            ImGuiEx.TextWrapped(ImGuiColors.DalamudOrange, $"Warning! Legacy overlay is generally not supported and will not receive new features down the road. Additionally, should it break in a way that a full rewrite will be required, it will be removed instead. Please consider using Badge Overlay instead. ");
            ImGui.Checkbox("Enable Legacy Overlay", ref C.EnableLegacy);
            if(C.EnableLegacy)
            {
                ImGui.Indent();
                ImGui.Checkbox($"Display Aethernet menu", ref C.ShowAethernet);
                ImGui.Checkbox($"Display World Visit menu", ref C.ShowWorldVisit);
                ImGui.Indent();
                ImGui.Checkbox($"Show only worlds from current virtual data center", ref C.ShowWorldVisitCurrentDc);
                ImGui.Unindent();
                ImGui.Checkbox($"Display Housing Ward buttons", ref C.ShowWards);
                ImGui.Checkbox($"Display Instance changer menu", ref C.ShowInstanceSwitcher);
                ImGui.Indent();
                ImGui.SetNextItemWidth(150f);
                ImGui.SliderInt("Extra button height", ref C.InstanceButtonHeight, 0, 50);
                ImGui.Unindent();
                ImGui.Checkbox($"Display overlay near Miniature Aetherytes", ref C.ShowMiniAetheryteOverlay);

                UtilsUI.NextSection();

                ImGui.Checkbox("Fixed Lifestream Overlay position", ref C.FixedPosition);
                if(C.FixedPosition)
                {
                    ImGui.Indent();
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGuiEx.EnumCombo("Horizontal base position", ref C.PosHorizontal);
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGuiEx.EnumCombo("Vertical base position", ref C.PosVertical);
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGui.DragFloat2("Offset", ref C.Offset);

                    ImGui.Unindent();
                }

                UtilsUI.NextSection();

                ImGui.SetNextItemWidth(100f.Scale());
                fixed(int* ptr = &C.ButtonWidthArray[0])
                fixed(byte* sptr = "Button left/right padding\0"u8)
                {
                    ImGuiNative.InputInt3(sptr, ptr, ImGuiInputTextFlags.None);
                }
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt("Aetheryte button top/bottom padding", ref C.ButtonHeightAetheryte);
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt("World button top/bottom padding", ref C.ButtonHeightWorld);
                ImGui.Unindent();


                ImGui.Checkbox("Left-align text on buttons", ref C.LeftAlignButtons);
                if(C.LeftAlignButtons)
                {
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("Left padding, spaces", ref C.LeftAlignPadding, 0.1f, 0, 20);
                }
            }
        })

        .Section("Game Window Integration")
        .Checkbox($"Hide Lifestream if the following game windows are open", () => ref C.HideAddon)
        .If(() => C.HideAddon)
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
                    C.HideAddonList.Add(AddNew);
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
                        if(C.HideAddonList.Contains(name)) continue;
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV(EColor.Green, $"Focused: {name}");
                        ImGui.TableNextColumn();
                        ImGui.PushID(name);
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                        {
                            C.HideAddonList.Add(name);
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.TableNextRow();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, 0x88888888);
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, 0x88888888);
                ImGui.TableNextColumn();
                ImGui.Dummy(new Vector2(5f));

                foreach(var s in C.HideAddonList)
                {
                    ImGui.PushID(s);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(focused.Contains(s) ? EColor.Green : null, s);
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => C.HideAddonList.Remove(s));
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        })
        .EndIf()
        .Draw();
    }

    private static void DrawGeneral()
    {
        new NuiBuilder()
        .Section("Teleport Configuration")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f.Scale());
            ImGuiEx.EnumCombo($"Teleport world change gateway", ref C.WorldChangeAetheryte, Lang.WorldChangeAetherytes);
            ImGuiEx.HelpMarker($"Where would you like to teleport for world changes");
            ImGui.Checkbox($"Teleport to specific aethernet destination after world/dc visit", ref C.WorldVisitTPToAethernet);
            if(C.WorldVisitTPToAethernet)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(250f.Scale());
                ImGui.InputText("Aethernet destination, as if you'd use in \"/li\" command", ref C.WorldVisitTPTarget, 50);
                ImGui.Checkbox($"Only teleport from command but not from overlay", ref C.WorldVisitTPOnlyCmd);
                ImGui.Unindent();
            }
            ImGui.Checkbox($"Add firmament location into Foundation aetheryte", ref C.Firmament);
            ImGui.Checkbox($"Automatically leave non cross-world party upon changing world", ref C.LeavePartyBeforeWorldChange);
            ImGui.Checkbox($"Show teleport destination in chat", ref C.DisplayChatTeleport);
            ImGui.Checkbox($"Show teleport destination in popup notifications", ref C.DisplayPopupNotifications);
            ImGui.Checkbox("Retry same-world failed world visits", ref C.RetryWorldVisit);
            ImGui.Indent();
            ImGui.SetNextItemWidth(100f.Scale());
            ImGui.InputInt("Interval between retries, seconds##2", ref C.RetryWorldVisitInterval.ValidateRange(1, 120));
            ImGui.SameLine();
            ImGuiEx.Text("+ up to");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f.Scale());
            ImGui.InputInt("seconds##2", ref C.RetryWorldVisitIntervalDelta.ValidateRange(0, 120));
            ImGuiEx.HelpMarker("To make it appear less bot-like");
            ImGui.Unindent();
            //ImGui.Checkbox("Use Return instead of Teleport when possible", ref C.UseReturn);
            //ImGuiEx.HelpMarker("This includes any IPC calls");
            ImGui.Checkbox("Enable tray notifications upon travel completion", ref C.EnableNotifications);
            ImGuiEx.PluginAvailabilityIndicator([new("NotificationMaster")]);
        })

        .Section("Shortcuts")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f.Scale());
            ImGuiEx.EnumCombo("\"/li\" command behavior", ref C.LiCommandBehavior);
            ImGui.Checkbox("When teleporting to your own apartment, enter inside", ref C.EnterMyApartment);
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.EnumCombo("When teleporting to your/fc house, perform this action", ref C.HouseEnterMode);
            ImGui.SetNextItemWidth(150f.Scale());
            if(ImGui.BeginCombo("Preferred Inn", Utils.GetInnNameFromTerritory(C.PreferredInn), ImGuiComboFlags.HeightLarge))
            {
                foreach(var x in (uint[])[0, .. TaskPropertyShortcut.InnData.Keys])
                {
                    if(ImGui.Selectable(Utils.GetInnNameFromTerritory(x), x == C.PreferredInn)) C.PreferredInn = x;
                }
                ImGui.EndCombo();
            }
            if(Player.CID != 0)
            {
                ImGui.SetNextItemWidth(150f.Scale());
                var pref = C.PreferredSharedEstates.SafeSelect(Player.CID);
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
                            C.PreferredSharedEstates.Remove(Player.CID);
                        }
                        if(ImGui.RadioButton("Disable", pref == (-1, 0, 0)))
                        {
                            C.PreferredSharedEstates[Player.CID] = (-1, 0, 0);
                        }
                        if(ImGui.RadioButton($"{ExcelTerritoryHelper.GetName(x.TerritoryId)}, Ward {x.Ward}, Plot {x.Plot}", pref == ((int)x.TerritoryId, x.Ward, x.Plot)))
                        {
                            C.PreferredSharedEstates[Player.CID] = ((int)x.TerritoryId, x.Ward, x.Plot);
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            ImGui.Separator();
            ImGuiEx.Text("\"/li auto\" command priority:");
            var prio = C.PropertyPrio;
            var custom = C.PropertyPrioOverrides.TryGetValue(Player.CID, out var value);
            if(custom)
            {
                prio = value;
            }

            if(Player.Available)
            {
                ImGuiEx.Text($"For {Censor.Character(Player.NameWithWorld)}:");
                ImGui.Indent();
                if(ImGui.RadioButton("Use Global Settings", !custom))
                {
                    C.PropertyPrioOverrides.Remove(Player.CID);
                }
                ImGui.SameLine();

                if(ImGui.RadioButton("Use Individual Settings", custom))
                {
                    if(!custom)
                    {
                        C.PropertyPrioOverrides[Player.CID] = [];
                    }
                }
                ImGui.Unindent();
            }
            else
            {
                ImGuiEx.TextV($"Editing Global Settings:");
            }
            ImGui.SameLine();
            if(ImGuiEx.IconButton($"\uf2ea"))
            {
                prio.Clear();
            }
            ImGuiEx.Tooltip("Reset to default order");

            var dragDrop = Ref<ImGuiEx.RealtimeDragDrop<AutoPropertyData>>.Get(() => new("apddd", x => x.Type.ToString()));
            prio.AddRange(Enum.GetValues<TaskPropertyShortcut.PropertyType>().Where(x => x != TaskPropertyShortcut.PropertyType.Auto && !prio.Any(s => s.Type == x)).Select(x => new AutoPropertyData(false, x)));
            dragDrop.Begin();
            for(var i = 0; i < prio.Count; i++)
            {
                var d = prio[i];
                ImGui.PushID($"c{i}");
                dragDrop.NextRow();
                dragDrop.DrawButtonDummy(d, prio, i);
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
            ImGui.Checkbox("Click Aethernet Shard on map for quick teleport", ref C.UseMapTeleport);
            ImGui.Checkbox("Only process when next to aetheryte in the same map", ref C.DisableMapClickOtherTerritory);
        })

        .Section("Command completion")
        .Widget(() =>
        {
            ImGuiEx.Text($"Suggest autocompletion when typing Lifestream commands in chat");
            ImGui.Checkbox("Enable", ref C.EnableAutoCompletion);
            ImGui.Checkbox("Display popup window at fixed position", ref C.AutoCompletionFixedWindow);
            ImGui.Indent();
            ImGui.SetNextItemWidth(200f.Scale());
            ImGui.DragFloat2("Position", ref C.AutoCompletionWindowOffset, 1f);
            ImGuiEx.RadioButtonBool("From bottom", "From top", ref C.AutoCompletionWindowBottom, sameLine: true, inverted: true);
            ImGuiEx.RadioButtonBool("From right", "From left", ref C.AutoCompletionWindowRight, sameLine: true, inverted: true);
            ImGui.Unindent();
        })

        .Section("Cross-Datacenter")
        .Widget(() =>
        {
            ImGui.Checkbox($"Allow travelling to another data center", ref C.AllowDcTransfer);
            ImGui.Checkbox($"Leave party before switching data center", ref C.LeavePartyBeforeLogout);
            ImGui.Checkbox($"Teleport to gateway aetheryte before switching data center if not in sanctuary", ref C.TeleportToGatewayBeforeLogout);
            ImGui.Checkbox($"Teleport to gateway aetheryte after completing data center travel", ref C.DCReturnToGateway);
            ImGui.Checkbox($"Allow alternative world during DC transfer", ref C.DcvUseAlternativeWorld);
            ImGuiEx.HelpMarker("If destination world isn't available but some other world on targeted data center is, it will be selected instead. Normal world visit will be enqueued after logging in.");
            ImGui.Checkbox($"Retry data center transfer if destination world is not available", ref C.EnableDvcRetry);
            ImGui.Indent();
            ImGui.SetNextItemWidth(150f.Scale());
            ImGui.InputInt("Max retries", ref C.MaxDcvRetries.ValidateRange(1, int.MaxValue));
            ImGui.SetNextItemWidth(150f.Scale());
            ImGui.InputInt("Interval between retries, seconds", ref C.DcvRetryInterval.ValidateRange(10, 1000));
            ImGui.Unindent();
        })

        .Section("Address Book")
        .Widget(() =>
        {
            ImGui.Checkbox($"Disable pathing to a plot", ref C.AddressNoPathing);
            ImGuiEx.HelpMarker($"You will be left at a closest aetheryte to the ward");
            ImGui.Checkbox($"Disable entering an apartment", ref C.AddressApartmentNoEntry);
            ImGuiEx.HelpMarker($"You will be left at an entry confirmation dialogue");
        })

        .Section("Movement")
        .Checkbox("Use Mount when auto-moving", () => ref C.UseMount)
        .Widget(() =>
        {
            Dictionary<int, string> mounts = [new KeyValuePair<int, string>(0, "Mount roulette"), .. Svc.Data.GetExcelSheet<Mount>().Where(x => x.Singular != "").ToDictionary(x => (int)x.RowId, x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x.Singular.GetText()))];
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.Combo("Preferred Mount", ref C.Mount, mounts.Keys, names: mounts);
        })
        .Checkbox("Dismount upon arrriving to housing plot", () => ref C.AutoDismount)
        .Checkbox("Use Sprint when auto-moving", () => ref C.UseSprintPeloton)
        .Checkbox("Use Peloton when auto-moving", () => ref C.UsePeloton)

        .Section("Character Select Menu")
        .Checkbox("Enable Data center and World visit from Character Select Menu", () => ref C.AllowDCTravelFromCharaSelect)
        .Checkbox("Use world visit instead of DC visit to travel to same world on guest DC", () => ref C.UseGuestWorldTravel)

        .Section("Wotsit Integration")
        .Widget(() =>
        {
            var anyChanged = ImGui.Checkbox("Enable Wotsit Integration for teleporting to Aethernet destinations", ref C.WotsitIntegrationEnabled);
            ImGuiEx.PluginAvailabilityIndicator([new("Dalamud.FindAnything", "Wotsit")]);

            if(C.WotsitIntegrationEnabled)
            {
                ImGui.Indent();
                if(ImGui.Checkbox("Include world select window", ref C.WotsitIntegrationIncludes.WorldSelect))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include auto-teleport to property", ref C.WotsitIntegrationIncludes.PropertyAuto))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to private estate", ref C.WotsitIntegrationIncludes.PropertyPrivate))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to free company estate", ref C.WotsitIntegrationIncludes.PropertyFreeCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to apartment", ref C.WotsitIntegrationIncludes.PropertyApartment))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to inn room", ref C.WotsitIntegrationIncludes.PropertyInn))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to grand company", ref C.WotsitIntegrationIncludes.GrandCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to market board", ref C.WotsitIntegrationIncludes.MarketBoard))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include teleport to island sanctuary", ref C.WotsitIntegrationIncludes.IslandSanctuary))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include auto-teleport to aethernet destinations", ref C.WotsitIntegrationIncludes.AetheryteAethernet))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include address book entries", ref C.WotsitIntegrationIncludes.AddressBook))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("Include custom aliases", ref C.WotsitIntegrationIncludes.CustomAlias))
                {
                    anyChanged = true;
                }
                ImGui.Unindent();
            }

            if(anyChanged)
            {
                PluginLog.Debug("Wotsit integration settings changed, re-initializing immediately");
                S.Ipc.WotsitManager.TryClearWotsit();
                S.Ipc.WotsitManager.MaybeTryInit(true);
            }
        })

        .Draw();
    }

    private static void DrawOverlay()
    {
        new NuiBuilder()
        .Section("Presets")
        .Widget(() =>
        {
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.ExclamationTriangle, "Full Reset - All Badge Overlay Settings (Hold CTRL+SHIFT)", enabled:ImGuiEx.Ctrl && ImGuiEx.Shift))
            {
                C.BadgeBarStyle.Dock = BadgeDock.Left;
                C.BadgeBarStyle.Sizing = BadgeSizing.FitText;
                C.BadgeBarStyle.UniformLength = 0f;
                C.BadgeBarStyle.Anchor = BadgeAnchor.Start;
                C.BadgeBarStyle.KeepOrderWhenAnchoredToEnd = false;
                C.BadgeBarStyle.TextRotation = VerticalTextRotation.Degrees270;
                C.BadgeBarStyle.Spacing = 4f;
                C.BadgeBarStyle.PaddingStart = 8f;
                C.BadgeBarStyle.PaddingEnd = 8f;
                C.BadgeBarStyle.ScreenMargin = 0f;
                C.BadgeBarStyle.InnerPadding = 8f;
                C.BadgeBarStyle.CrossPadding = 4f;
                C.BadgeBarStyle.Thickness = 0f;
                C.BadgeBarStyle.Rounding = 4f;
                C.BadgeBarStyle.BorderThickness = 1f;
                C.BadgeBarStyle.BackgroundColor = OverlayColor.Rgba(32, 34, 40, 230);
                C.BadgeBarStyle.HoveredColor = OverlayColor.Rgba(52, 56, 66, 240);
                C.BadgeBarStyle.ActiveColor = OverlayColor.Rgba(70, 100, 170, 245);
                C.BadgeBarStyle.TextColor = OverlayColor.Rgba(235, 235, 240);
                C.BadgeBarStyle.HeaderTextColor = OverlayColor.Rgba(100, 150, 225);
                C.BadgeBarStyle.BorderColor = OverlayColor.Rgba(90, 94, 104);
                C.BadgeBarStyle.PopoutAlignment = PopoutAlignment.Center;
                C.OverlayOptions.HoverOutDelay = 2f;
                C.OverlayOptions.PopoutGap = 2f;
                C.OverlayOptions.DefaultSelectableTrigger = TriggerMode.Hover;
                C.OverlayOptions.AlwaysOnTop = false;
                C.OverlayOptions.PopoutRounding = 0f;
                C.BadgeFontSize = 2f;
                C.PopoutFontSize = 1.5f;
                C.BadgeTriggerMode = TriggerMode.Hover;
                C.AethernetPinned = false;
                C.InstancePinned = false;
                C.OverlayMinWidth = 300f;
                ApplyBB();
            }
            ImGui.Separator();

            ImGuiEx.TextWrapped("The following buttons will only change sizing options. Hold CTRL and click to apply preset.");
            if(ImGuiEx.Button("Default", enabled: ImGuiEx.Ctrl))
            {
                C.BadgeBarStyle.Sizing = BadgeSizing.Uniform;
                C.BadgeBarStyle.ScreenMargin = 0f;
                C.BadgeBarStyle.Spacing = 15f;
                C.BadgeBarStyle.PaddingStart = 40f;
                C.BadgeBarStyle.PaddingEnd = 40f;
                C.BadgeBarStyle.UniformLength = 200f;
                C.BadgeBarStyle.Thickness = 0f;
                C.OverlayOptions.PopoutGap = 2f;
                C.BadgeFontSize = 2f;
                C.PopoutFontSize = 1.5f;
                C.OverlayMinWidth = 300f;
                ApplyBB();
            }
            ImGui.SameLine();
            if(ImGuiEx.Button("Large", enabled: ImGuiEx.Ctrl))
            {
                C.BadgeBarStyle.Sizing = BadgeSizing.Uniform;
                C.BadgeBarStyle.ScreenMargin = 0f;
                C.BadgeBarStyle.Spacing = 15f;
                C.BadgeBarStyle.PaddingStart = 40f;
                C.BadgeBarStyle.PaddingEnd = 40f;
                C.BadgeBarStyle.UniformLength = 200f;
                C.OverlayOptions.PopoutGap = 2f;
                C.BadgeBarStyle.Thickness = 0f;
                C.BadgeFontSize = 3f;
                C.PopoutFontSize = 2f;
                C.OverlayMinWidth = 300f;
                ApplyBB();
            }
            ImGui.SameLine();
            if(ImGuiEx.Button("Compact", enabled: ImGuiEx.Ctrl))
            {
                C.BadgeBarStyle.Sizing = BadgeSizing.FitText;
                C.BadgeBarStyle.ScreenMargin = 0f;
                C.BadgeBarStyle.Spacing = 5f;
                C.BadgeBarStyle.PaddingStart = 20;
                C.BadgeBarStyle.PaddingEnd = 20f;
                C.BadgeBarStyle.UniformLength = 150f;
                C.OverlayOptions.PopoutGap = 2f;
                C.BadgeBarStyle.Thickness = 0f;
                C.BadgeFontSize = 1.5f;
                C.PopoutFontSize = 1.2f;
                C.OverlayMinWidth = 200f;
                ApplyBB();
            }
            ImGui.SameLine();
            if(ImGuiEx.Button("Micro", enabled: ImGuiEx.Ctrl))
            {
                C.BadgeBarStyle.Sizing = BadgeSizing.FitText;
                C.BadgeBarStyle.ScreenMargin = 0f;
                C.BadgeBarStyle.Spacing = 5f;
                C.BadgeBarStyle.PaddingStart = 5f;
                C.BadgeBarStyle.PaddingEnd = 5f;
                C.BadgeBarStyle.UniformLength = 150f;
                C.OverlayOptions.PopoutGap = 2f;
                C.BadgeBarStyle.Thickness = 0f;
                C.BadgeFontSize = 1.2f;
                C.PopoutFontSize = 1f;
                C.OverlayMinWidth = 150f;
                ApplyBB();
            }

            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.PaintBrush, "Reset Colors", enabled: ImGuiEx.Ctrl))
            {
                C.BadgeBarStyle.BackgroundColor = OverlayColor.Rgba(32, 34, 40, 230);
                C.BadgeBarStyle.HoveredColor = OverlayColor.Rgba(52, 56, 66, 240);
                C.BadgeBarStyle.ActiveColor = OverlayColor.Rgba(70, 100, 170, 245);
                C.BadgeBarStyle.TextColor = OverlayColor.Rgba(235, 235, 240);
                C.BadgeBarStyle.HeaderTextColor = OverlayColor.Rgba(100, 150, 225);
                C.BadgeBarStyle.BorderColor = OverlayColor.Rgba(90, 94, 104);
            }
        })
        .Section("Badge Overlay Settings")
        .Widget(() =>
        {
            ImGui.Checkbox("Enable Badge Overlay", ref C.Enable);
            if(C.Enable)
            {
                ImGui.Indent();
                ImGui.Checkbox($"Display Aethernet Badge", ref C.ShowAethernetBOM);
                ImGui.Indent();
                ImGui.Checkbox("Auto-open Aethernet Badge when coming close to aetheryte", ref C.AethernetPinned);
                ImGuiEx.HelpMarker("Click on badge or hover to another badge to close overlay.");
                ImGui.Unindent();

                ImGui.Checkbox($"Display Cosmic Exploration Planet Badge", ref C.ShowCosmicBOM);
                ImGui.Indent();
                ImGui.Checkbox("Auto-open Cosmic Exploration Planet Badge when coming close to aetheryte", ref C.CosmicPinned);
                ImGuiEx.HelpMarker("Click on badge or hover to another badge to close overlay.");
                ImGui.Unindent();

                ImGui.Checkbox($"Display World Visit Badge", ref C.ShowWorldVisitBOM);
                ImGui.Indent();
                ImGui.Checkbox("Auto-open World Visit Badge when coming close to aetheryte", ref C.WorldPinned);
                ImGuiEx.HelpMarker("Click on badge or hover to another badge to close overlay.");
                ImGuiEx.EnumCombo(150f, "Different Data Center Worlds", ref C.OtherDCShowMode);
                ImGui.Unindent();
                ImGui.Checkbox($"Display Housing Ward Badge", ref C.ShowWardsBOM);
                ImGui.Indent();
                ImGui.Checkbox("Auto-open Housing Ward Badge when coming close to aetheryte", ref C.WardPinned);
                ImGuiEx.HelpMarker("Click on badge or hover to another badge to close overlay.");
                ImGui.Unindent();
                ImGui.Checkbox($"Display Instance Badge", ref C.ShowInstanceSwitcherBOM);
                ImGui.Indent();
                ImGui.Checkbox("Auto-open Instance Badge when coming close to aetheryte", ref C.InstancePinned);
                ImGuiEx.HelpMarker("Click on badge or hover to another badge to close overlay.");
                ImGui.Unindent();
                ImGui.Unindent();
            }
        })

        .Section("Badge Overlay Options")
        .Widget(() =>
        {
            ImGuiEx.InputFloat(150f, "Hover Out Delay", ref C.OverlayOptions.HoverOutDelay);
            ImGuiEx.HelpMarker("How many seconds should pass before popout window will be closed if cursor leaves it.");
            ImGuiEx.InputFloat(150f, "Popout Gap", ref C.OverlayOptions.PopoutGap);
            ImGuiEx.HelpMarker("Empty space between badge and popout window or between two popout windows.");
            ImGuiEx.EnumCombo(150f, "Badge Trigger", ref C.BadgeTriggerMode);
            ImGuiEx.HelpMarker("This setting will take effect only after you press \"Apply Settings\" button", symbolOverride: FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGuiEx.HelpMarker("Whether to hover or click badge to open popout menu.");
            ImGuiEx.EnumCombo(150f, "Popout Selectable Trigger", ref C.OverlayOptions.DefaultSelectableTrigger);
            ImGuiEx.HelpMarker("This setting will take effect only after you press \"Apply Settings\" button", symbolOverride: FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGuiEx.HelpMarker("Whether to hover or click menu item with submenu to open a submenu.");
            ImGuiEx.InputFloat(150f, "Popout Rounding", ref C.OverlayOptions.PopoutRounding);
            ImGuiEx.Checkbox("Always On Top", ref C.OverlayOptions.AlwaysOnTop);

            ImGuiEx.InputFloat(150f, "Minimum Popout Width", ref C.OverlayMinWidth);
            ImGuiEx.HelpMarker("This setting will take effect only after you press \"Apply Settings\" button", symbolOverride: FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGuiEx.HelpMarker("Minimum width of popout window.");
        })

        .Section("Badge Overlay Style")
        .Widget(() =>
        {
            ImGuiEx.EnumCombo(150f, "Dock", ref C.BadgeBarStyle.Dock);
            ImGuiEx.HelpMarker("Where should badges be placed.");
            ImGuiEx.EnumCombo(150f, "Popout Alignment", ref C.BadgeBarStyle.PopoutAlignment);
            ImGuiEx.HelpMarker("How popout window will be placed relative to the badge. ");
            ImGuiEx.EnumCombo(150f, "Sizing", ref C.BadgeBarStyle.Sizing);
            ImGuiEx.HelpMarker("Sizing type for badges.");
            ImGuiEx.InputFloat(150f, "Uniform Length", ref C.BadgeBarStyle.UniformLength);
            ImGuiEx.HelpMarker("Length of a badge. Only applicable if Sizing is set to Uniform. Regardless of this setting minimal value will be dictated by largest text on a badge.");
            ImGuiEx.EnumCombo(150f, "Anchor", ref C.BadgeBarStyle.Anchor);
            ImGuiEx.HelpMarker("Where should badges be aligned horizontally (if docked at top/bottom) or vertically (if docked left/right).");
            ImGuiEx.EnumCombo(150f, "Text Rotation", ref C.BadgeBarStyle.TextRotation);
            ImGuiEx.HelpMarker("Only applicable when Dock is either Left or Right.");
            ImGuiEx.InputFloat(150f, "Spacing", ref C.BadgeBarStyle.Spacing);
            ImGuiEx.HelpMarker("Space between badges.");
            ImGuiEx.InputFloat(150f, "Padding Start", ref C.BadgeBarStyle.PaddingStart);
            ImGuiEx.HelpMarker("Empty space from the beginning of the alignment grid.");
            ImGuiEx.InputFloat(150f, "Padding End", ref C.BadgeBarStyle.PaddingEnd);
            ImGuiEx.HelpMarker("Empty space from the end of the alignment grid.");
            ImGuiEx.InputFloat(150f, "Screen Margin", ref C.BadgeBarStyle.ScreenMargin);
            ImGuiEx.HelpMarker("Empty space from the docking side.");
            ImGuiEx.InputFloat(150f, "Inner Padding", ref C.BadgeBarStyle.InnerPadding);
            ImGuiEx.HelpMarker("Empty space at left and right sides relative to text. Only applicable with sizing set to Fit Text.");
            ImGuiEx.InputFloat(150f, "Cross Padding", ref C.BadgeBarStyle.CrossPadding);
            ImGuiEx.HelpMarker("Empty space at top and bottom sides relative to text. Can be negative. Not applicable when thickness is explicity set.");
            ImGuiEx.InputFloat(150f, "Thickness", ref C.BadgeBarStyle.Thickness);
            ImGuiEx.HelpMarker("Height of the badge. 0 - autosize.");
            ImGuiEx.InputFloat(150f, "Rounding", ref C.BadgeBarStyle.Rounding);
            ImGuiEx.InputFloat(150f, "Border Thickness", ref C.BadgeBarStyle.BorderThickness);
            ImGui.Checkbox("Use Custom Colors", ref C.BadgeBarStyle.UseCustomColors);
            if(C.BadgeBarStyle.UseCustomColors)
            {
                ImGui.Indent();
                ImGuiEx.ColorEdit4("Background Color", ref C.BadgeBarStyle.BackgroundColor, ImGuiColorEditFlags.NoInputs);
                ImGuiEx.ColorEdit4("Hovered Color", ref C.BadgeBarStyle.HoveredColor, ImGuiColorEditFlags.NoInputs);
                ImGuiEx.ColorEdit4("Active Color", ref C.BadgeBarStyle.ActiveColor, ImGuiColorEditFlags.NoInputs);
                ImGuiEx.ColorEdit4("Text Color", ref C.BadgeBarStyle.TextColor, ImGuiColorEditFlags.NoInputs);
                ImGuiEx.ColorEdit4("Border Color", ref C.BadgeBarStyle.BorderColor, ImGuiColorEditFlags.NoInputs);
                ImGuiEx.ColorEdit4("Table Header Color", ref C.BadgeBarStyle.HeaderTextColor, ImGuiColorEditFlags.NoInputs);
                ImGui.Unindent();
            }
        })

        .Section("Badge Overlay Fonts")
        .Widget(() =>
        {
            ImGuiEx.InputFloat(150f, "Badge Font Scale", ref C.BadgeFontSize.ValidateRange(0.1f, 20f));
            ImGuiEx.HelpMarker("This setting will take effect only after you press \"Apply Settings\" button", symbolOverride: FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGuiEx.HelpMarker("Multiplier for default font size for badges");
            ImGuiEx.InputFloat(150f, "Popout Window Font Scale", ref C.PopoutFontSize.ValidateRange(0.1f, 20f));
            ImGuiEx.HelpMarker("This setting will take effect only after you press \"Apply Settings\" button", symbolOverride: FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGuiEx.HelpMarker("Multiplier for default font size for popout menus");
        })

        .Section("Apply Badge Overlay Settings")
        .Widget(() =>
        {
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Check, "Apply Badge Overlay Settings"))
            {
                ApplyBB();
            }
        })

        .Section("Instance changer")
        .Checkbox("Retry on failure", () => ref C.InstanceSwitcherRepeat)
        .Checkbox("Return to the ground when flying before changing instance", () => ref C.EnableFlydownInstance)
        .Widget("Display instance number in Server Info Bar", (x) =>
        {
            if(ImGui.Checkbox(x, ref C.EnableDtrBar))
            {
                S.DtrManager.Refresh();
            }
        })
        .Widget("Reset Instance Data", (x) =>
        {
            if(ImGuiEx.Button(x, C.PublicInstances.Count > 0))
            {
                C.PublicInstances.Clear();
                EzConfig.Save();
            }
        })

        /*.Section("Game Window Integration")
        .Checkbox($"Hide Lifestream if the following game windows are open", () => ref C.HideAddon)
        .If(() => C.HideAddon)
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
                    C.HideAddonList.Add(AddNew);
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
                        if(C.HideAddonList.Contains(name)) continue;
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV(EColor.Green, $"Focused: {name}");
                        ImGui.TableNextColumn();
                        ImGui.PushID(name);
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                        {
                            C.HideAddonList.Add(name);
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.TableNextRow();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, 0x88888888);
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, 0x88888888);
                ImGui.TableNextColumn();
                ImGui.Dummy(new Vector2(5f));

                foreach(var s in C.HideAddonList)
                {
                    ImGui.PushID(s);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(focused.Contains(s) ? EColor.Green : null, s);
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => C.HideAddonList.Remove(s));
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        })
        .EndIf()*/
        .Draw();

        if(C.Hidden.Count > 0)
        {
            new NuiBuilder()
            .Section("Hidden Aetherytes")
            .Widget(() =>
            {
                uint toRem = 0;
                foreach(var x in C.Hidden)
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
                    C.Hidden.Remove(toRem);
                }
            })
            .Draw();
        }
    }

    static void ApplyBB()
    {
        S.Gui.BadgeOverlayManager.Dispose();
        S.Gui.BadgeOverlayManager = new();
    }

    private static void DrawExpert()
    {
        new NuiBuilder()
        .Section("Expert Settings")
        .Widget(() =>
        {
            ImGui.Checkbox($"Slow down aetheryte teleporting", ref C.SlowTeleport);
            ImGuiEx.HelpMarker($"Slows down aethernet teleportation by specified amount.");
            if(C.SlowTeleport)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(200f.Scale());
                ImGui.DragInt("Teleport delay (ms)", ref C.SlowTeleportThrottle);
                ImGui.Unindent();
            }
            ImGuiEx.CheckboxInverted($"Skip waiting until game screen is ready", ref C.WaitForScreenReady);
            ImGuiEx.HelpMarker($"Enable this option for faster teleports but be careful that you may get stuck.");
            ImGui.Checkbox($"Hide progress bar", ref C.NoProgressBar);
            ImGuiEx.HelpMarker($"Hiding progress bar leaves you with no way to stop Lifestream from executing it's tasks.");
            ImGuiEx.CheckboxInverted($"Don't walk to nearby aetheryte on world change command from greater distance", ref C.WalkToAetheryte);
            ImGui.Checkbox($"Progress overlay at top of the sreen", ref C.ProgressOverlayToTop);
            ImGui.Checkbox("Allow custom alias and house alias to override built-in commands", ref C.AllowCustomOverrides);
            ImGui.Indent();
            ImGuiEx.TextWrapped(EColor.RedBright, "Warning! Other plugins may rely on built-in commands. Ensure that it is not the case if you decide to enable this option and override commands.");
            ImGui.Unindent();
        })
        .Draw();
    }
}
