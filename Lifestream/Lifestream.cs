using AutoRetainerAPI;
using Dalamud.Game.Gui.Dtr;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ECommons.Singletons;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Game;
using Lifestream.GUI;
using Lifestream.GUI.Windows;
using Lifestream.IPC;
using Lifestream.Movement;
using Lifestream.Schedulers;
using Lifestream.Services;
using Lifestream.Systems;
using Lifestream.Systems.Custom;
using Lifestream.Systems.Legacy;
using Lifestream.Systems.Residential;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.Sheets;
using NotificationMasterAPI;
using GrandCompany = ECommons.ExcelServices.GrandCompany;

namespace Lifestream;

public unsafe class Lifestream : IDalamudPlugin
{
    public string Name => "Lifestream";
    internal static Lifestream P;
    internal static Config C => P.Config;
    private Config Config;

    internal TinyAetheryte? ActiveAetheryte = null;
    internal AutoRetainerApi AutoRetainerApi;
    internal uint Territory => TerritoryWatcher.GetRealTerritoryType();
    internal NotificationMasterApi NotificationMasterApi;

    public TaskManager TaskManager;

    internal FollowPath followPath = null;
    public static IDtrBarEntry? Entry;

    public FollowPath FollowPath
    {
        get
        {
            followPath ??= new();
            return followPath;
        }
    }
    public bool DisableHousePathData = false;
    public CharaSelectOverlay CharaSelectOverlay;

    public Lifestream(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        ECommonsMain.Init(pluginInterface, this, Module.SplatoonAPI);
#if CUSTOMCS
        PluginLog.Warning($"Using custom FFXIVClientStructs");
        var gameVersion = DalamudReflector.TryGetDalamudStartInfo(out var ver) ? ver.GameVersion.ToString() : "unknown";
        InteropGenerator.Runtime.Resolver.GetInstance.Setup(Svc.SigScanner.SearchBase, gameVersion, new(Svc.PluginInterface.ConfigDirectory.FullName + "/cs.json"));
        FFXIVClientStructs.Interop.Generated.Addresses.Register();
        InteropGenerator.Runtime.Resolver.GetInstance.Resolve();
#endif
        new TickScheduler(delegate
        {
            TerritoryWatcher.Initialize();
            Config = EzConfig.Init<Config>();
            Utils.CheckConfigMigration();
            EzConfigGui.Init(MainGui.Draw);
            TaskManager = new(new(showDebug: true));
            CharaSelectOverlay = new();
            EzConfigGui.WindowSystem.AddWindow(CharaSelectOverlay);
            EzCmd.Add("/lifestream", ProcessCommand, null);
            EzCmd.Add("/li", ProcessCommand, "\n" + Lang.Help);
            ProperOnLogin.RegisterAvailable(() =>
            {
                Config.CharaMap[Player.CID] = Player.NameWithWorld;
            });
            Svc.Framework.Update += Framework_Update;
            Svc.Toasts.ErrorToast += Toasts_ErrorToast;
            AutoRetainerApi = new();
            NotificationMasterApi = new(Svc.PluginInterface);
            SingletonServiceManager.Initialize(typeof(Service));
        });
    }

    private void Toasts_ErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if(!Svc.ClientState.IsLoggedIn)
        {
            //430	60	8	0	False	Please wait and try logging in later.
            if(message.GetText().Trim() == Svc.Data.GetExcelSheet<LogMessage>().GetRow(430).Text.GetText().Trim())
            {
                PluginLog.Warning($"CharaSelectListMenuError encountered");
                EzThrottler.Throttle("CharaSelectListMenuError", 2.Minutes(), true);
            }
        }
    }

    internal void ProcessCommand(string command, string arguments)
    {
        foreach(var x in (string[])[
                    ..Utils.LifestreamNativeCommands,
                    ..C.CustomAliases.Where(x => x.Enabled && x.Alias != "").Select(x => x.Alias),
                    ..C.AddressBookFolders.SelectMany(x => x.Entries).Where(x => x.AliasEnabled && x.Alias != "").Select(x => x.Alias)])
        {
            if(arguments.EndsWith($" {x}", StringComparison.OrdinalIgnoreCase))
            {
                arguments = arguments[0..(arguments.Length - x.Length)] + $",{x}";
                PluginLog.Information($"New args: {arguments}");
            }
        }
        var argsSplit = arguments.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var primary = argsSplit.SafeSelect(0) ?? "";
        var additionalCommand = argsSplit.Length > 1 ? argsSplit[1..].Join(",") : null;
        if(arguments.StartsWith("debug TaskAetheryteAethernetTeleport "))
        {
            var args = arguments.Split(" ");
            if(args.Length == 4 && args[3] == "firmament")
            {
                args[3] = TaskAetheryteAethernetTeleport.FirmamentAethernetId.ToString();
            }
            if(args.Length != 4 || !uint.TryParse(args[2], out var a) || !uint.TryParse(args[3], out var b))
            {
                DuoLog.Error("Invalid arguments");
                return;
            }

            try
            {
                TaskAetheryteAethernetTeleport.Enqueue(a, b);
            }
            catch(Exception e)
            {
                DuoLog.Error(e.Message);
            }
        }
        else if(arguments == "debug WotsitManager clear")
        {
            S.Ipc.WotsitManager.TryClearWotsit();
            Notify.Info("WotsitManager cleared, see logs for details");
        }
        else if(arguments == "debug WotsitManager init")
        {
            S.Ipc.WotsitManager.MaybeTryInit();
            Notify.Info("WotsitManager reinitialized, see logs for details");
        }
        else if(arguments == "stop")
        {
            Notify.Info($"Discarding {TaskManager.NumQueuedTasks + (TaskManager.IsBusy ? 1 : 0)} tasks");
            TaskManager.Abort();
            followPath?.Stop();
            TabUtility.TargetWorldID = 0;
        }
        else if(arguments != "" && C.AllowCustomOverrides && ProcessCustomShortcuts(arguments))
        {
            return;
        }
        else if(arguments.Length == 1 && int.TryParse(arguments, out var val) && val.InRange(1, 9))
        {
            if(S.InstanceHandler.GetInstance() == val)
            {
                DuoLog.Warning($"Already in instance {val}");
            }
            else if(S.InstanceHandler.CanChangeInstance())
            {
                TaskChangeInstance.Enqueue(val);
            }
            else
            {
                DuoLog.Error($"Can't change instance now");
            }
        }
        else if(arguments.EqualsIgnoreCaseAny("open", "select", "window", "w", "world", "travel"))
        {
            S.Gui.SelectWorldWindow.IsOpen = true;
        }
        else if(arguments == "auto")
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Auto);
        }
        else if(arguments.EqualsIgnoreCaseAny("home", "house", "private"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Home);
        }
        else if(arguments.EqualsIgnoreCaseAny("fc", "free", "company", "free company"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.FC);
        }
        else if(arguments.EqualsIgnoreCaseAny("ws", "workshop"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.FC, workshop: true);
        }
        else if(arguments.EqualsIgnoreCaseAny("apartment", "apt"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Apartment);
        }
        else if(arguments.EqualsIgnoreCaseAny("shared"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Shared_Estate);
        }
        else if(arguments.EqualsIgnoreCaseAny("inn", "hinn") || arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "inn ", "hinn "))
        {
            var x = arguments.Split(" ");
            int? innNum = x.Length == 1 ? null : int.Parse(x[1]) - 1;
            if(innNum != null && !innNum.Value.InRange(0, TaskPropertyShortcut.InnData.Count))
            {
                var num = 1;
                DuoLog.Warning($"Invalid inn index. Valid inns are: \n{TaskPropertyShortcut.InnData.Select(s => $"{num++} - {Utils.GetInnNameFromTerritory(s.Key)}").Print("\n")}");
            }
            else
            {
                TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Inn, innIndex: innNum, useSameWorld: !arguments.StartsWithAny("hinn"));
            }
        }
        else if(arguments.EqualsAny("gc", "gcc", "hc", "hcc", "fcgc", "gcfc") || arguments.StartsWithAny("gc ", "gcc ", "hc ", "hcc ", "fcgc ", "gcfc "))
        {
            var arglist = arguments.Split(" ");
            var isChest = arguments.StartsWithAny("gcc", "hcc");
            var fcgc = arguments.StartsWithAny("fcgc", "gcfc");
            var returnHome = arguments[0] == 'h';
            if(arglist.Length == 1)
            {
                TaskGCShortcut.Enqueue(null, isChest, returnHome, fcgc);
            }
            else
            {
                if(arglist[1].EqualsIgnoreCaseAny(GrandCompany.TwinAdder.ToString(), "Twin Adder", "Twin", "Adder", "TA", "A", "serpent"))
                {
                    TaskGCShortcut.Enqueue(GrandCompany.TwinAdder, isChest, returnHome, fcgc);
                }
                else if(arglist[1].EqualsIgnoreCaseAny(GrandCompany.Maelstrom.ToString(), "Mael", "S", "M", "storm", "strom"))
                {
                    TaskGCShortcut.Enqueue(GrandCompany.Maelstrom, isChest, returnHome, fcgc);
                }
                else if(arglist[1].EqualsIgnoreCaseAny(GrandCompany.ImmortalFlames.ToString(), "Immortal Flames", "Immortal", "Flames", "IF", "F", "flame"))
                {
                    TaskGCShortcut.Enqueue(GrandCompany.ImmortalFlames, isChest, returnHome, fcgc);
                }
                else if(Enum.TryParse<GrandCompany>(arglist[1], out var result))
                {
                    TaskGCShortcut.Enqueue(result, isChest, returnHome, fcgc);
                }
                else
                {
                    DuoLog.Error($"Could not parse input: {arglist[1]}");
                }
            }
        }
        else if(arguments.EqualsIgnoreCaseAny("mb", "market"))
        {
            if(!P.TaskManager.IsBusy && Player.Interactable)
            {
                TaskMBShortcut.Enqueue();
            }
        }
        else if(arguments.EqualsIgnoreCaseAny("island", "is", "sanctuary") || arguments.StartsWithAny("island ", "is ", "sanctuary "))
        {
            var arglist = arguments.Split(" ");
            if(arglist.Length == 1)
                TaskISShortcut.Enqueue();
            else
            {
                var name = arglist[1];
                if(S.Data.DataStore.IslandNPCs.TryGetFirst(x => x.Value.Any(y => y.Contains(name, StringComparison.OrdinalIgnoreCase)), out var npc))
                    TaskISShortcut.Enqueue(npc.Key);
                else
                    DuoLog.Error($"Could not parse input: {name}");
            }
        }
        else if(arguments.EqualsIgnoreCaseAny("cosmic", "ardorum", "moon"))
        {
            if(!Utils.IsBusy())
            {
                if(!Player.IsInHomeWorld)
                {
                    P.TPAndChangeWorld(Player.HomeWorld, !Player.IsInHomeDC, null, true, null, false, false);
                }
                P.TaskManager.Enqueue(() => Player.Interactable && Player.IsInHomeWorld && IsScreenReady());
                StaticAlias.CosmicExploration.Enqueue(true);
            }
            else
            {
                Notify.Error("Lifestream is busy");
            }
        }
        else if(arguments.EqualsIgnoreCase("occult"))
        {
            if(!Utils.IsBusy())
            {
                StaticAlias.OccultCrescent.Enqueue(true);
            }
            else
            {
                Notify.Error("Lifestream is busy");
            }
        }
        else if(arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "tp"))
        {
            var destination = primary[(primary.IndexOf("tp") + 2)..].Trim();
            if(destination == null || destination == "")
            {
                DuoLog.Error($"Please type something");
            }
            else
            {
                if(!P.TaskManager.IsBusy && Player.Interactable)
                {
                    if(Utils.EnqueueTeleport(destination, additionalCommand))
                    {
                        ProcessAdditionalCommand(additionalCommand);
                    }
                }
            }
        }
        else if(Utils.TryParseAddressBookEntry(arguments, out var entry))
        {
            ChatPrinter.Green($"[Lifestream] Address parsed: {entry.GetAddressString()}");
            entry.GoTo();
        }
        else
        {
            if(command.EqualsIgnoreCase("/lifestream") && arguments == "")
            {
                EzConfigGui.Open();
            }
            else
            {
                if(arguments == "")
                {
                    if(Config.LiCommandBehavior == LiCommandBehavior.Open_World_Change_Menu)
                    {
                        S.Gui.SelectWorldWindow.IsOpen = true;
                        return;
                    }
                    else if(Config.LiCommandBehavior == LiCommandBehavior.Open_Configuration)
                    {
                        EzConfigGui.Open();
                        return;
                    }
                    else if(Config.LiCommandBehavior == LiCommandBehavior.Do_Nothing)
                    {
                        return;
                    }
                }
                if(ProcessCustomShortcuts(primary))
                {
                    ProcessAdditionalCommand(additionalCommand);
                    return;
                }

                WorldChangeAetheryte? gateway = null;
                if(additionalCommand == "mb")
                {
                    gateway = WorldChangeAetheryte.Uldah;
                }

                if(S.Data.DataStore.Worlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var w))
                {
                    PluginLog.Information($"Same dc/{primary}/{w}");
                    TPAndChangeWorld(w, false, gateway: gateway);
                }
                else if(S.Data.DataStore.DCWorlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var dcw))
                {
                    PluginLog.Information($"Cross dc/{primary}/{w}");
                    TPAndChangeWorld(dcw, true, gateway: gateway);
                }
                else if(Utils.TryGetWorldFromDataCenter(primary, out var world, out var dc))
                {
                    Utils.DisplayInfo($"Random world from {Svc.Data.GetExcelSheet<WorldDCGroupType>().GetRow(dc).Name}: {world}");
                    TPAndChangeWorld(world, Player.Object.CurrentWorld.ValueNullable?.DataCenter.RowId != dc, gateway: gateway);
                }
                else
                {
                    TaskTryTpToAethernetDestination.Enqueue(primary, true, true);
                }

                ProcessAdditionalCommand(additionalCommand);
            }
        }
    }

    private void ProcessAdditionalCommand(string additionalCommand)
    {
        if(additionalCommand != null)
        {
            TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            TaskManager.Enqueue(() => Svc.Framework.RunOnTick(() => Svc.Commands.ProcessCommand($"/li {additionalCommand}"), delayTicks: 1));
        }
    }

    internal void TPAndChangeWorld(string destinationWorld, bool isDcTransfer = false, string secondaryTeleport = null, bool noSecondaryTeleport = false, WorldChangeAetheryte? gateway = null, bool? doNotify = null, bool? returnToGateway = null, bool skipChecks = false)
    {
        try
        {
            Utils.AssertCanTravel(Player.Name, Player.Object.HomeWorld.RowId, Player.Object.CurrentWorld.RowId, destinationWorld);
            CharaSelectVisit.ApplyDefaults(ref returnToGateway, ref gateway, ref doNotify);
            if(!skipChecks)
            {
                if(isDcTransfer && !C.AllowDcTransfer)
                {
                    Notify.Error($"Data center transfers are not enabled in the configuration.");
                    return;
                }
                if(TaskManager.IsBusy)
                {
                    Notify.Error("Another task is in progress");
                    return;
                }
            }
            if(!Player.Available)
            {
                Notify.Error("No player");
                return;
            }
            if(destinationWorld == Player.CurrentWorld)
            {
                Notify.Error("Already in this world");
                return;
            }
            /*if(ActionManager.Instance()->GetActionStatus(ActionType.Spell, 5) != 0)
            {
                Notify.Error("You are unable to teleport at this time");
                return;
            }*/
            if(Svc.Party.Length > 1 && !C.LeavePartyBeforeWorldChange && !C.LeavePartyBeforeWorldChange)
            {
                Notify.Warning("You must disband party in order to switch worlds");
            }
            Utils.DisplayInfo($"Destination: {destinationWorld}");
            if(isDcTransfer)
            {
                var type = DCVType.Unknown;
                var homeDC = Player.Object.HomeWorld.ValueNullable?.DataCenter.ValueNullable?.Name.ToString() ?? throw new NullReferenceException("Home DC is null ??????");
                var currentDC = Player.Object.CurrentWorld.ValueNullable?.DataCenter.ValueNullable?.Name.ToString() ?? throw new NullReferenceException("Current DC is null ??????"); ;
                var targetDC = Utils.GetDataCenterName(destinationWorld);
                if(currentDC == homeDC)
                {
                    type = DCVType.HomeToGuest;
                }
                else
                {
                    if(targetDC == homeDC)
                    {
                        type = DCVType.GuestToHome;
                    }
                    else
                    {
                        type = DCVType.GuestToGuest;
                    }
                }
                TaskRemoveAfkStatus.Enqueue();
                if(type != DCVType.Unknown)
                {
                    if(Config.TeleportToGatewayBeforeLogout && !(TerritoryInfo.Instance()->InSanctuary || ExcelTerritoryHelper.IsSanctuary(P.Territory)) && !(currentDC == homeDC && Player.HomeWorld != Player.CurrentWorld))
                    {
                        TaskTpToAethernetDestination.Enqueue(gateway.Value.AdjustGateway());
                    }
                    if(Config.LeavePartyBeforeLogout)
                    {
                        if(Svc.Party.Length > 1 || Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance])
                        {
                            TaskManager.EnqueueTask(new(WorldChange.LeaveAnyParty));
                        }
                    }
                }
                if(type == DCVType.HomeToGuest)
                {
                    if(!Player.IsInHomeWorld) TaskTPAndChangeWorld.Enqueue(Player.HomeWorld, gateway.Value.AdjustGateway(), false);
                    TaskWaitUntilInHomeWorld.Enqueue();
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    CharaSelectVisit.HomeToGuest(destinationWorld, Player.Name, Player.HomeWorldId, Player.HomeWorldId, secondaryTeleport, noSecondaryTeleport, gateway, doNotify, returnToGateway);
                }
                else if(type == DCVType.GuestToHome)
                {
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    CharaSelectVisit.GuestToHome(destinationWorld, Player.Name, Player.HomeWorldId, Player.CurrentWorldId, secondaryTeleport, noSecondaryTeleport, gateway, doNotify, returnToGateway);
                }
                else if(type == DCVType.GuestToGuest)
                {
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    CharaSelectVisit.GuestToGuest(destinationWorld, Player.Name, Player.HomeWorldId, Player.CurrentWorldId, secondaryTeleport, noSecondaryTeleport, gateway, doNotify, returnToGateway);
                }
                else
                {
                    DuoLog.Error($"Error - unknown data center visit type");
                }
                PluginLog.Information($"Data center visit: {type}");
            }
            else
            {
                TaskRemoveAfkStatus.Enqueue();
                /*if(Config.LeavePartyBeforeWorldChangeSameWorld && (Svc.Party.Length > 1 || Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance]))
                {
                    TaskManager.EnqueueTask(new(WorldChange.LeaveAnyParty));
                }*/
                TaskTPAndChangeWorld.Enqueue(destinationWorld, gateway.Value.AdjustGateway(), false);
                if(doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {destinationWorld}");
                CharaSelectVisit.EnqueueSecondary(noSecondaryTeleport, secondaryTeleport);
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    private void Framework_Update(object framework)
    {
        YesAlreadyManager.Tick();
        followPath?.Update();
        if(Svc.ClientState.LocalPlayer != null && S.Data.DataStore.Territories.Contains(P.Territory))
        {
            UpdateActiveAetheryte();
        }
        else
        {
            ActiveAetheryte = null;
        }
        S.Data.ResidentialAethernet.Tick();
        S.Data.CustomAethernet.Tick();
        MonitorChatInput();
        if(!Svc.ClientState.IsLoggedIn)
        {
            if(TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m) && m.IsAddonReady)
            {
                foreach(var chara in m.Characters)
                {
                    Config.CharaMap[chara.Entry->ContentId] = $"{chara.Name}@{ExcelWorldHelper.GetName(chara.HomeWorld)}";
                }
            }
        }
        if(P.TaskManager.IsBusy)
        {
            if(EzThrottler.Throttle("EnsureEnhancedLoginIsOff")) Utils.EnsureEnhancedLoginIsOff();
            if(TryGetAddonByName<AtkUnitBase>("Trade", out var trade))
            {
                Callback.Fire(trade, true, -1);
            }
            if(TryGetAddonMaster<AddonMaster.Talk>("Talk", out var m) && m.IsAddonReady)
            {
                m.Click();
            }
        }
        if(TabUtility.TargetWorldID != 0)
        {
            if(Player.Available && Player.Object.CurrentWorld.RowId == TabUtility.TargetWorldID && IsScreenReady())
            {
                if(EzThrottler.Throttle("TerminateGame", 60000))
                {
                    Environment.Exit(0);
                }
                else
                {
                    if(EzThrottler.Throttle("WarnTerminate", 1000))
                    {
                        DuoLog.Warning($"Arrived to {ExcelWorldHelper.GetName(TabUtility.TargetWorldID)}. Game is shutting down in {EzThrottler.GetRemainingTime("TerminateGame") / 1000} seconds. Type \"/li stop\" to cancel.");
                    }
                }
            }
            else
            {
                EzThrottler.Throttle("TerminateGame", 60000, true);
            }
        }
    }

    public void Dispose()
    {
        Svc.Framework.Update -= Framework_Update;
        Svc.Toasts.ErrorToast -= Toasts_ErrorToast;
        followPath?.Dispose();
        ECommonsMain.Dispose();
        P = null;
    }

    private void UpdateActiveAetheryte()
    {
        var a = Utils.GetValidAetheryte();
        if(a != null)
        {
            var pos2 = a.Position.ToVector2();
            foreach(var x in S.Data.DataStore.Aetherytes)
            {
                if(x.Key.TerritoryType == P.Territory && Vector2.Distance(x.Key.Position, pos2) < 10)
                {
                    if(ActiveAetheryte == null)
                    {
                        S.Gui.Overlay.IsOpen = true;
                    }
                    ActiveAetheryte = x.Key;
                    return;
                }
                foreach(var l in x.Value)
                {
                    if(l.TerritoryType == P.Territory && Vector2.Distance(l.Position, pos2) < 10)
                    {
                        if(ActiveAetheryte == null)
                        {
                            S.Gui.Overlay.IsOpen = true;
                        }
                        ActiveAetheryte = l;
                        return;
                    }
                }
            }
        }
        else
        {
            ActiveAetheryte = null;
        }
        if(!S.Gui.Overlay.IsOpen)
        {
            if(C.ShowInstanceSwitcher && S.InstanceHandler.GetInstance() != 0 && TaskChangeInstance.GetAetheryte() == null && ActiveAetheryte == null)
            {
                S.Gui.Overlay.IsOpen = true;
            }
        }
    }

    private unsafe void MonitorChatInput()
    {
        try
        {
            if(S.SearchHelperOverlay == null) return;

            if(!C.EnableAutoCompletion)
            {
                if(S.SearchHelperOverlay.IsOpen)
                {
                    S.SearchHelperOverlay.IsOpen = false;
                }
                return;
            }

            var component = GetActiveTextInput();
            if(component == null)
            {
                if(S.SearchHelperOverlay.IsOpen)
                {
                    S.SearchHelperOverlay.IsOpen = false;
                }
                return;
            }

            var addon = component->ContainingAddon;
            if(addon == null) addon = component->ContainingAddon2;
            if(addon == null || addon->NameString != "ChatLog")
            {
                if(S.SearchHelperOverlay.IsOpen)
                {
                    S.SearchHelperOverlay.IsOpen = false;
                }
                return;
            }

            var currentText = component->UnkText1.ToString();

            if(currentText.StartsWith("/li", StringComparison.OrdinalIgnoreCase))
            {
                if(currentText.Length >= 3)
                {
                    S.SearchHelperOverlay.UpdateFilter(currentText);
                    S.SearchHelperOverlay.IsOpen = true;
                }
            }
            else
            {
                if(S.SearchHelperOverlay.IsOpen)
                {
                    S.SearchHelperOverlay.IsOpen = false;
                }
            }
        }
        catch(Exception ex)
        {
            if(EzThrottler.Throttle("ChatMonitorError", 5000))
            {
                PluginLog.Debug($"Chat monitor error: {ex.Message}");
            }
        }
    }

    private unsafe AtkComponentTextInput* GetActiveTextInput()
    {
        try
        {
            var mod = RaptureAtkModule.Instance();
            if(mod == null) return null;

            var basePtr = mod->TextInput.TargetTextInputEventInterface;
            if(basePtr == null) return null;

            // Memory signature from Dalamud's Completion.cs (line 102)
            // Used to identify the correct text input component vtable
            var wantedVtblPtr = Svc.SigScanner.GetStaticAddressFromSig(
                "48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 8B 48 68",
                4);

            var vtblPtr = *(nint*)basePtr;
            if(vtblPtr != wantedVtblPtr) return null;

            return (AtkComponentTextInput*)((AtkComponentInputBase*)basePtr - 1);
        }
        catch
        {
            return null;
        }
    }

    private bool ProcessCustomShortcuts(string arguments)
    {
        foreach(var b in Config.AddressBookFolders)
        {
            foreach(var e in b.Entries)
            {
                if(e.AliasEnabled && e.Alias != "" && e.Alias.EqualsIgnoreCase(arguments))
                {
                    e.GoTo();
                    return true;
                }
            }
        }
        foreach(var x in Config.CustomAliases)
        {
            if(!x.Enabled || x.Alias == "") continue;
            if(x.Alias.EqualsIgnoreCase(arguments))
            {
                x.Enqueue();
                return true;
            }
        }
        return false;
    }
}