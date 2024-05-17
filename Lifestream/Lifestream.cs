using AutoRetainerAPI;
using ECommons.Automation.NeoTaskManager;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ChatMethods;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.Reflection;
using ECommons.SimpleGui;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Game;
using Lifestream.GUI;
using Lifestream.IPC;
using Lifestream.Movement;
using Lifestream.Schedulers;
using Lifestream.Systems;
using Lifestream.Systems.Legacy;
using Lifestream.Tasks;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.GeneratedSheets;
using NightmareUI.OtterGuiWrapper.FileSystems;
using NotificationMasterAPI;

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
    public GenericFileSystem<AddressBookFolder> AddressBookFileSystem;

    public Lifestream(DalamudPluginInterface pluginInterface)
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
            EzCmd.Add("/lifestream", ProcessCommand, "Open plugin configuration");
            EzCmd.Add("/li", ProcessCommand, "automatically switch world to specified (matched by first letters) or return to home world if none specified, or teleport to aethernet destination if near aetheryte. Aethernet destination may be specified next to target world as well.");
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
            AddressBookFileSystem = new(Config.AddressBookFolders, "AddressBook");
						AddressBookFileSystem.Selector.OnAfterDrawLeafName += TabAddressBook.Selector_OnAfterDrawLeafName;
						AddressBookFileSystem.Selector.OnBeforeItemCreation += Selector_OnBeforeItemCreation;
						AddressBookFileSystem.Selector.OnBeforeCopy += Selector_OnBeforeCopy;
						AddressBookFileSystem.Selector.OnImportPopupOpen += Selector_OnImportPopupOpen;
            IPCProvider = new();
				});
    }

		private void Selector_OnImportPopupOpen(string clipboardText, ref string newName)
		{
        try
        {
            var item = EzConfig.DefaultSerializationFactory.Deserialize<AddressBookFolder>(clipboardText);
            if(item != null && item.ExportedName != null && !item.ExportedName.EqualsIgnoreCase("Default Book"))
            {
                newName = item.ExportedName;
            }
        }
        catch(Exception e) { }
		}

		private void Selector_OnBeforeCopy(AddressBookFolder original, ref AddressBookFolder copy)
		{
				copy.IsCopy = true;
        if(AddressBookFileSystem.FindLeaf(original, out var leaf))
        {
            copy.ExportedName = leaf.Name;
        }
		}

		private void Selector_OnBeforeItemCreation(ref AddressBookFolder item)
		{
        if (item.Entries == null)
        {
            item = null;
            Notify.Error($"Item contains invalid data");
        }
        item.IsDefault = false;
        item.GUID = Guid.NewGuid();
		}

		private void Toasts_ErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if (!Svc.ClientState.IsLoggedIn)
        {
            //430	60	8	0	False	Please wait and try logging in later.
            if (message.ExtractText().Trim() == Svc.Data.GetExcelSheet<LogMessage>().GetRow(430).Text.ExtractText().Trim())
            {
                PluginLog.Warning($"CharaSelectListMenuError encountered");
                EzThrottler.Throttle("CharaSelectListMenuError", 2.Minutes(), true);
            }
        }
    }

    internal void ProcessCommand(string command, string arguments)
    {
        if (arguments == "stop")
        {
            Notify.Info($"Discarding {TaskManager.NumQueuedTasks + (TaskManager.IsBusy?1:0)} tasks");
            TaskManager.Abort();
            followPath?.Stop();
        }
        else if(Utils.TryParseAddressBookEntry(arguments, out var entry))
        {
            ChatPrinter.Green($"[Lifestream] Address parsed: {entry.GetAddressString()}");
            entry.GoTo();
        }
        else
        {
            if (command.EqualsIgnoreCase("/lifestream") && arguments == "")
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
                if (DataStore.Worlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var w))
                {
                    TPAndChangeWorld(w, false, secondary);
                }
                else if(DataStore.DCWorlds.TryGetFirst(x => x.StartsWith(primary == "" ? Player.HomeWorld : primary, StringComparison.OrdinalIgnoreCase), out var dcw))
                {
                    TPAndChangeWorld(dcw, true, secondary);
                }
                else
                {
                    TaskTryTpToAethernetDestination.Enqueue(primary);
                }
            }
        }
    }

    internal void TPAndChangeWorld(string w, bool isDcTransfer = false, string secondaryTeleport = null, bool noSecondaryTeleport = false, WorldChangeAetheryte? gateway = null, bool? doNotify = null, bool? returnToGateway = null)
    {
        try
        {
            returnToGateway ??= P.Config.DCReturnToGateway;
            gateway ??= P.Config.WorldChangeAetheryte;
            doNotify ??= true;
            if (secondaryTeleport == null && P.Config.WorldVisitTPToAethernet && !P.Config.WorldVisitTPTarget.IsNullOrEmpty())
            {
                secondaryTeleport = P.Config.WorldVisitTPTarget;
            }
            if (isDcTransfer && !P.Config.AllowDcTransfer)
            {
                Notify.Error($"Data center transfers are not enabled in the configuration.");
                return;
            }
            if (TaskManager.IsBusy)
            {
                Notify.Error("Another task is in progress");
                return;
            }
            if (!Player.Available)
            {
                Notify.Error("No player");
                return;
            }
            if (w == Player.CurrentWorld)
            {
                Notify.Error("Already in this world");
                return;
            }
            /*if(ActionManager.Instance()->GetActionStatus(ActionType.Spell, 5) != 0)
            {
                Notify.Error("You are unable to teleport at this time");
                return;
            }*/
            if (Svc.Party.Length > 1 && !P.Config.LeavePartyBeforeWorldChange && !P.Config.LeavePartyBeforeWorldChange)
            {
                Notify.Warning("You must disband party in order to switch worlds");
            }
            if (!Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "TeleporterPlugin" && x.IsLoaded))
            {
                Notify.Warning("Teleporter plugin is not installed");
            }
            Notify.Info($"Destination: {w}");
            if (isDcTransfer)
            {
                var type = DCVType.Unknown;
                var homeDC = Player.Object.HomeWorld.GameData.DataCenter.Value.Name.ToString();
                var currentDC = Player.Object.CurrentWorld.GameData.DataCenter.Value.Name.ToString();
                var targetDC = Utils.GetDataCenter(w);
                if (currentDC == homeDC)
                {
                    type = DCVType.HomeToGuest;
                }
                else
                {
                    if (targetDC == homeDC)
                    {
                        type = DCVType.GuestToHome;
                    }
                    else
                    {
                        type = DCVType.GuestToGuest;
                    }
                }
                TaskRemoveAfkStatus.Enqueue();
                if (type != DCVType.Unknown)
                {
                    if (Config.TeleportToGatewayBeforeLogout && !(TerritoryInfo.Instance()->IsInSanctuary() || ExcelTerritoryHelper.IsSanctuary(Svc.ClientState.TerritoryType)) && !(currentDC == homeDC && Player.HomeWorld != Player.CurrentWorld))
                    {
                        TaskTpToAethernetDestination.Enqueue(gateway.Value);
                    }
                    if (Config.LeavePartyBeforeLogout && (Svc.Party.Length > 1 || Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance]))
                    {
                        TaskManager.EnqueueTask(new(WorldChange.LeaveAnyParty));
                    }
                }
                if (type == DCVType.HomeToGuest)
                {
                    if (!Player.IsInHomeWorld) TaskTPAndChangeWorld.Enqueue(Player.HomeWorld, gateway.Value, false);
                    TaskWaitUntilInHomeWorld.Enqueue();
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    TaskChangeDatacenter.Enqueue(w, Player.Name, Player.Object.HomeWorld.Id);
                    TaskSelectChara.Enqueue(Player.Name, Player.Object.HomeWorld.Id);
                    TaskWaitUntilInWorld.Enqueue(w);

                    if (gateway != null && returnToGateway == true) TaskReturnToGateway.Enqueue(gateway.Value);
                    if (doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {w}");
                    EnqueueSecondary();
                }
                else if (type == DCVType.GuestToHome)
                {
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    TaskReturnToHomeDC.Enqueue(Player.Name, Player.Object.HomeWorld.Id);
                    TaskSelectChara.Enqueue(Player.Name, Player.Object.HomeWorld.Id);
                    if (Player.HomeWorld != w)
                    {
                        TaskManager.EnqueueMulti([
                            new(WorldChange.WaitUntilNotBusy, TaskSettings.TimeoutInfinite),
                        new DelayTask(1000),
                        new(() => TaskTPAndChangeWorld.Enqueue(w, gateway.Value, true), $"TpAndChangeWorld {w} at {gateway.Value}"),
                        ]);
                    }
                    else
                    {
                        TaskWaitUntilInWorld.Enqueue(w);
                    }
                    if (gateway != null && returnToGateway == true) TaskReturnToGateway.Enqueue(gateway.Value);
                    if (doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {w}");
                    EnqueueSecondary();
                }
                else if (type == DCVType.GuestToGuest)
                {
                    TaskLogoutAndRelog.Enqueue(Player.NameWithWorld);
                    TaskReturnToHomeDC.Enqueue(Player.Name, Player.Object.HomeWorld.Id);
                    TaskChangeDatacenter.Enqueue(w, Player.Name, Player.Object.HomeWorld.Id);
                    TaskSelectChara.Enqueue(Player.Name, Player.Object.HomeWorld.Id);
                    TaskWaitUntilInWorld.Enqueue(w);
                    if (gateway != null && returnToGateway == true) TaskReturnToGateway.Enqueue(gateway.Value);
                    TaskDesktopNotification.Enqueue($"Arrived to {w}");
                    EnqueueSecondary();
                }
                else
                {
                    DuoLog.Error($"Error - unknown data center visit type");
                }
                Notify.Info($"Data center visit: {type}");
            }
            else
            {
                TaskRemoveAfkStatus.Enqueue();
                TaskTPAndChangeWorld.Enqueue(w, gateway.Value, false);
                if (doNotify == true) TaskDesktopNotification.Enqueue($"Arrived to {w}");
                EnqueueSecondary();
            }

            void EnqueueSecondary()
            {
                if (noSecondaryTeleport) return;
                if (!secondaryTeleport.IsNullOrEmpty())
                {
                    TaskManager.EnqueueMulti([
                        new(() => Player.Interactable),
                    new(() => TaskTryTpToAethernetDestination.Enqueue(secondaryTeleport))
                        ]);
                }
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

    void UpdateActiveAetheryte()
    {
        var a = Utils.GetValidAetheryte();
        if (a != null)
        {
            var pos2 = a.Position.ToVector2();
            foreach (var x in DataStore.Aetherytes)
            {
                if (x.Key.TerritoryType == Svc.ClientState.TerritoryType && Vector2.Distance(x.Key.Position, pos2) < 10)
                {
                    if (ActiveAetheryte == null)
                    {
                        Overlay.IsOpen = true;
                    }
                    ActiveAetheryte = x.Key;
                    return;
                }
                foreach (var l in x.Value)
                {
                    if (l.TerritoryType == Svc.ClientState.TerritoryType && Vector2.Distance(l.Position, pos2) < 10)
                    {
                        if (ActiveAetheryte == null)
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
    }
}