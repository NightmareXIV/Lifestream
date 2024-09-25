using AutoRetainerAPI;
using ECommons.Automation.NeoTaskManager;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ECommons.Singletons;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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
using Lifestream.Systems.Legacy;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.GeneratedSheets;
using NightmareUI.OtterGuiWrapper.FileSystems.Generic;
using NotificationMasterAPI;
using GrandCompany = ECommons.ExcelServices.GrandCompany;

namespace Lifestream;

public unsafe class Lifestream : IDalamudPlugin
{
    public string Name => "Lifestream";
    internal static Lifestream P;
    internal Config Config;
    internal DataStore DataStore;
    internal Memory Memory;
    internal Overlay Overlay;

    internal TinyAetheryte? ActiveAetheryte = null;
    internal AutoRetainerApi AutoRetainerApi;
    internal uint Territory => Svc.ClientState.TerritoryType;
    internal NotificationMasterApi NotificationMasterApi;

    public TaskManager TaskManager;

    public ResidentialAethernet ResidentialAethernet;
    internal FollowPath followPath = null;
    public Provider IPCProvider;
    public FollowPath FollowPath
    {
        get
        {
            followPath ??= new();
            return followPath;
        }
    }
    public VnavmeshManager VnavmeshManager;
    public SplatoonManager SplatoonManager;
    public bool DisableHousePathData = false;
    public CharaSelectOverlay CharaSelectOverlay;

    public Lifestream(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        ECommonsMain.Init(pluginInterface, this, Module.SplatoonAPI);
        new TickScheduler(delegate
        {
            Config = EzConfig.Init<Config>();
            EzConfigGui.Init(MainGui.Draw);
            Overlay = new();
            TaskManager = new();
            TaskManager.DefaultConfiguration.ShowDebug = true;
            EzConfigGui.WindowSystem.AddWindow(Overlay);
            EzConfigGui.WindowSystem.AddWindow(new ProgressOverlay());
            CharaSelectOverlay = new();
            EzConfigGui.WindowSystem.AddWindow(CharaSelectOverlay);
            EzCmd.Add("/lifestream", ProcessCommand, "Open plugin configuration");
            EzCmd.Add("/li", ProcessCommand, """
                return to your home world
                /li <worldname> - go to specified world
                /li <dataCenterName> - go to random world in specified Data Center
                /li <aethernetName> - go to specified aethernet destination if you are next to any supported aetheryte

                /li <address> - go to specified plot in current world, where address - plot adddress formatted in "residential district, ward, plot" format (without quotes)
                /li <worldname> <address> - go to specified plot in specified world

                /li gc|hc - go to your grand company
                /li gc|hc <company name> - go to specified grand company
                /li gcc|hcc - go to your grand company's fc chest
                /li gcc|hcc <company name> - go to specified grand company's fc chest
                ...where "gc" or "gcc" will move you to grand company in current world while "hc" or "hcc" will return you to home world first

                /li auto - go to your private estate, free company estate or apartment, whatever is found in this order
                /li home|house|private - go to your private estate
                /li fc|free|company|free company - go to your free company estate
                /li apartment|apt - go to your apartment

                /li w|world|open|select - open world travel window
                """);
            DataStore = new();
            ProperOnLogin.RegisterAvailable(() => DataStore.BuildWorlds());
            Svc.Framework.Update += Framework_Update;
            Memory = new();
            //EqualStrings.RegisterEquality("Guilde des aventuriers (Guildes des armuriers & forgeron...", "Guilde des aventuriers (Guildes des armuriers & forgerons/Maelstrom)");
            Svc.Toasts.ErrorToast += Toasts_ErrorToast;
            AutoRetainerApi = new();
            NotificationMasterApi = new(Svc.PluginInterface);
            ResidentialAethernet = new();
            VnavmeshManager = new();
            SplatoonManager = new();
            IPCProvider = new();
            SingletonServiceManager.Initialize(typeof(Service));
        });
    }

    private void Toasts_ErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if(!Svc.ClientState.IsLoggedIn)
        {
            //430	60	8	0	False	Please wait and try logging in later.
            if(message.ExtractText().Trim() == Svc.Data.GetExcelSheet<LogMessage>().GetRow(430).Text.ExtractText().Trim())
            {
                PluginLog.Warning($"CharaSelectListMenuError encountered");
                EzThrottler.Throttle("CharaSelectListMenuError", 2.Minutes(), true);
            }
        }
    }

    internal void ProcessCommand(string command, string arguments)
    {
        if(arguments == "stop")
        {
            Notify.Info($"Discarding {TaskManager.NumQueuedTasks + (TaskManager.IsBusy ? 1 : 0)} tasks");
            TaskManager.Abort();
            followPath?.Stop();
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
            S.SelectWorldWindow.IsOpen = true;
        }
        else if(arguments == "auto")
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Auto);
        }
        else if(arguments.EqualsIgnoreCaseAny("home", "house", "private"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Home);
        }
        else if(arguments.EqualsIgnoreCaseAny("fc", "free", "company", "company", "free company"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.FC);
        }
        else if(arguments.EqualsIgnoreCaseAny("apartment", "apt"))
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Apartment);
        }
        else if(arguments.EqualsIgnoreCaseAny("inn") || arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "inn "))
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
                TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Inn, default, innNum);
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
        else if (arguments.EqualsIgnoreCaseAny("island", "is", "sanctuary") || arguments.StartsWithAny("island ", "is ", "sanctuary "))
        {
            var arglist = arguments.Split(" ");
            if (arglist.Length == 1)
                TaskISShortcut.Enqueue();
            else
            {
                var name = arglist[1];
                if (DataStore.IslandNPCs.TryGetFirst(x => x.Value.Any(y => y.Contains(name, StringComparison.OrdinalIgnoreCase)), out var npc))
                    TaskISShortcut.Enqueue(npc.Key);
                else
                    DuoLog.Error($"Could not parse input: {name}");
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
                var primary = arguments.Split(' ').SafeSelect(0);
                var secondary = arguments.Split(' ').SafeSelect(1);
                foreach(var b in Config.AddressBookFolders)
                {
                    foreach(var e in b.Entries)
                    {
                        if(e.AliasEnabled && e.Alias != "" && e.Alias.EqualsIgnoreCase(primary))
                        {
                            e.GoTo();
                            return;
                        }
                    }
                }
                foreach(var x in Config.CustomAliases)
                {
                    if(x.Alias.EqualsIgnoreCase(primary))
                    {
                        x.Enqueue();
                        return;
                    }
                }
                if(DataStore.Worlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var w))
                {
                    TPAndChangeWorld(w, false, secondary);
                }
                else if(DataStore.DCWorlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var dcw))
                {
                    TPAndChangeWorld(dcw, true, secondary);
                }
                else if(Utils.TryGetWorldFromDataCenter(primary, out var world, out var dc))
                {
                    Utils.DisplayInfo($"Random world from {Svc.Data.GetExcelSheet<WorldDCGroupType>().GetRow(dc).Name}: {world}");
                    TPAndChangeWorld(world, Player.Object.CurrentWorld.GameData.DataCenter.Row != dc, secondary);
                }
                else
                {
                    TaskTryTpToAethernetDestination.Enqueue(primary);
                }
            }
        }
    }

    internal void TPAndChangeWorld(string destinationWorld, bool isDcTransfer = false, string secondaryTeleport = null, bool noSecondaryTeleport = false, WorldChangeAetheryte? gateway = null, bool? doNotify = null, bool? returnToGateway = null)
    {
        try
        {
            CharaSelectVisit.ApplyDefaults(ref returnToGateway, ref gateway, ref doNotify);
            if(isDcTransfer && !P.Config.AllowDcTransfer)
            {
                Notify.Error($"Data center transfers are not enabled in the configuration.");
                return;
            }
            if(TaskManager.IsBusy)
            {
                Notify.Error("Another task is in progress");
                return;
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
            if(Svc.Party.Length > 1 && !P.Config.LeavePartyBeforeWorldChange && !P.Config.LeavePartyBeforeWorldChange)
            {
                Notify.Warning("You must disband party in order to switch worlds");
            }
            Utils.DisplayInfo($"Destination: {destinationWorld}");
            if(isDcTransfer)
            {
                var type = DCVType.Unknown;
                var homeDC = Player.Object.HomeWorld.GameData.DataCenter.Value.Name.ToString();
                var currentDC = Player.Object.CurrentWorld.GameData.DataCenter.Value.Name.ToString();
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
                    if(Config.TeleportToGatewayBeforeLogout && !(TerritoryInfo.Instance()->InSanctuary || ExcelTerritoryHelper.IsSanctuary(Svc.ClientState.TerritoryType)) && !(currentDC == homeDC && Player.HomeWorld != Player.CurrentWorld))
                    {
                        TaskTpToAethernetDestination.Enqueue(gateway.Value.AdjustGateway());
                    }
                    if(Config.LeavePartyBeforeLogout && (Svc.Party.Length > 1 || Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance]))
                    {
                        TaskManager.EnqueueTask(new(WorldChange.LeaveAnyParty));
                    }
                }
                if(type == DCVType.HomeToGuest)
                {
                    if(!Player.IsInHomeWorld) TaskTPAndChangeWorld.Enqueue(Player.HomeWorld, gateway.Value.AdjustGateway(), false);
                    TaskWaitUntilInHomeWorld.Enqueue();
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    CharaSelectVisit.HomeToGuest(destinationWorld, Player.Name, Player.Object.HomeWorld.Id, secondaryTeleport, noSecondaryTeleport, gateway, doNotify, returnToGateway);
                }
                else if(type == DCVType.GuestToHome)
                {
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    CharaSelectVisit.GuestToHome(destinationWorld, Player.Name, Player.Object.HomeWorld.Id, secondaryTeleport, noSecondaryTeleport, gateway, doNotify, returnToGateway);
                }
                else if(type == DCVType.GuestToGuest)
                {
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    CharaSelectVisit.GuestToGuest(destinationWorld, Player.Name, Player.Object.HomeWorld.Id, secondaryTeleport, noSecondaryTeleport, gateway, doNotify, returnToGateway);
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
        if(Svc.ClientState.LocalPlayer != null && DataStore.Territories.Contains(Svc.ClientState.TerritoryType))
        {
            UpdateActiveAetheryte();
        }
        else
        {
            ActiveAetheryte = null;
        }
        P.ResidentialAethernet.Tick();
    }

    public void Dispose()
    {
        Svc.Framework.Update -= Framework_Update;
        Svc.Toasts.ErrorToast -= Toasts_ErrorToast;
        Memory.Dispose();
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
            foreach(var x in DataStore.Aetherytes)
            {
                if(x.Key.TerritoryType == Svc.ClientState.TerritoryType && Vector2.Distance(x.Key.Position, pos2) < 10)
                {
                    if(ActiveAetheryte == null)
                    {
                        Overlay.IsOpen = true;
                    }
                    ActiveAetheryte = x.Key;
                    return;
                }
                foreach(var l in x.Value)
                {
                    if(l.TerritoryType == Svc.ClientState.TerritoryType && Vector2.Distance(l.Position, pos2) < 10)
                    {
                        if(ActiveAetheryte == null)
                        {
                            Overlay.IsOpen = true;
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
        if(!Overlay.IsOpen)
        {
            if(P.Config.ShowInstanceSwitcher && S.InstanceHandler.GetInstance() != 0 && TaskChangeInstance.GetAetheryte() == null && ActiveAetheryte == null)
            {
                Overlay.IsOpen = true;
            }
        }
    }
}