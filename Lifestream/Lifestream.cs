using Dalamud.Game;
using Dalamud.Utility;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ECommons.StringHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lifestream.Enums;
using Lifestream.GUI;
using Lifestream.Schedulers;
using Lifestream.Tasks;
using Lumina.Excel.GeneratedSheets;
using System.Security.Permissions;

namespace Lifestream
{
    public unsafe class Lifestream : IDalamudPlugin
    {
        public string Name => "Lifestream";
        internal static Lifestream P;
        internal Config Config;
        internal TaskManager TaskManager;
        internal DataStore DataStore;
        internal Memory Memory;
        internal Overlay Overlay;

        internal TinyAetheryte? ActiveAetheryte = null;

        internal uint Territory => Svc.ClientState.TerritoryType;

        public Lifestream(DalamudPluginInterface pluginInterface)
        {
            P = this;
            ECommonsMain.Init(pluginInterface, this, Module.DalamudReflector);
            new TickScheduler(delegate
            {
                Config = EzConfig.Init<Config>();
                EzConfigGui.Init(MainGui.Draw);
                Overlay = new();
                EzConfigGui.WindowSystem.AddWindow(Overlay);
                EzConfigGui.WindowSystem.AddWindow(new ProgressOverlay());
                EzCmd.Add("/lifestream", ProcessCommand, "Open plugin configuration");
                EzCmd.Add("/li", ProcessCommand, "automatically switch world to specified (matched by first letters) or return to home world if none specified");
                TaskManager = new()
                {
                    AbortOnTimeout = true
                };
                DataStore = new();
                ProperOnLogin.Register(() => P.DataStore.BuildWorlds());
                Svc.Framework.Update += Framework_Update;
                Memory = new();
                //EqualStrings.RegisterEquality("Guilde des aventuriers (Guildes des armuriers & forgeron...", "Guilde des aventuriers (Guildes des armuriers & forgerons/Maelstrom)");
                Svc.Toasts.ErrorToast += Toasts_ErrorToast;
            });
        }

        private void Toasts_ErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
        {
            if (!Svc.ClientState.IsLoggedIn)
            {
                //430	60	8	0	False	Please wait and try logging in later.
                if (message.ExtractText().Trim() == Svc.Data.GetExcelSheet<LogMessage>().GetRow(430).Text.ToDalamudString().ExtractText().Trim())
                {
                    PluginLog.Warning($"CharaSelectListMenuError encountered");
                    EzThrottler.Throttle("CharaSelectListMenuError", 2.Minutes(), true);
                }
            }
        }

        private void ProcessCommand(string command, string arguments)
        {
            if (arguments == "stop")
            {
                Notify.Info($"Discarding {TaskManager.NumQueuedTasks + (TaskManager.IsBusy?1:0)} tasks");
                TaskManager.Abort();
            }
            else
            {
                if (command.EqualsIgnoreCase("/lifestream") && arguments == "")
                {
                    EzConfigGui.Open();
                }
                else
                {
                    if (DataStore.Worlds.TryGetFirst(x => x.StartsWith(arguments == "" ? Player.HomeWorld : arguments, StringComparison.OrdinalIgnoreCase), out var w))
                    {
                        TPAndChangeWorld(w);
                    }
                    else if(DataStore.DCWorlds.TryGetFirst(x => x.StartsWith(arguments == "" ? Player.HomeWorld : arguments, StringComparison.OrdinalIgnoreCase), out var dcw))
                    {
                        TPAndChangeWorld(dcw, true);
                    }
                    else
                    {
                        Notify.Error($"Destination world not found");
                    }
                }
            }
        }

        private void TPAndChangeWorld(string w, bool isDcTransfer = false)
        {
            if(isDcTransfer && !P.Config.AllowDcTransfer)
            {
                Notify.Error($"Data center transfers are not enabled in the configuration.");
                return;
            }
            if (!Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "TeleporterPlugin" && x.IsLoaded))
            {
                Notify.Error("Teleporter plugin is not installed");
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
            if(w == Player.CurrentWorld)
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
            Notify.Info($"Destination: {w}");
            if (isDcTransfer)
            {
                var type = DCVType.Unknown;
                var homeDC = Player.Object.HomeWorld.GameData.DataCenter.Value.Name.ToString();
                var currentDC = Player.Object.CurrentWorld.GameData.DataCenter.Value.Name.ToString();
                var targetDC = Util.GetDataCenter(w);
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
                    if (Config.TeleportToGatewayBeforeLogout && !(TerritoryInfo.Instance()->IsInSanctuary() || ExcelTerritoryHelper.IsSanctuary(Svc.ClientState.TerritoryType)) && !(currentDC == homeDC && Player.HomeWorld != Player.CurrentWorld))
                    {
                        TaskTpToGateway.Enqueue();
                    }
                    if(Config.LeavePartyBeforeLogout && (Svc.Party.Length > 1 || Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance]))
                    {
                        P.TaskManager.Enqueue(WorldChange.LeaveAnyParty);
                    }
                }
                if(type == DCVType.HomeToGuest)
                {
                    if (!Player.IsInHomeWorld) TaskTPAndChangeWorld.Enqueue(Player.HomeWorld);
                    TaskWaitUntilInHomeWorld.Enqueue();
                    TaskLogoutAndRelog.Enqueue();
                    TaskChangeDatacenter.Enqueue(w, Player.Name);
                    TaskSelectChara.Enqueue(Player.Name);
                    TaskWaitUntilInWorld.Enqueue(w);
                    TaskDesktopNotification.Enqueue($"Arrived to {w}");
                }
                else if(type == DCVType.GuestToHome)
                {
                    TaskLogoutAndRelog.Enqueue();
                    TaskReturnToHomeDC.Enqueue(Player.Name);
                    TaskSelectChara.Enqueue(Player.Name);
                    if (Player.HomeWorld != w)
                    {
                        P.TaskManager.Enqueue(WorldChange.WaitUntilNotBusy, 60.Minutes());
                        P.TaskManager.DelayNext(1000);
                        P.TaskManager.Enqueue(() => TaskTPAndChangeWorld.Enqueue(w));
                    }
                    else
                    {
                        TaskWaitUntilInWorld.Enqueue(w);
                    }
                    TaskDesktopNotification.Enqueue($"Arrived to {w}");
                }
                else if(type == DCVType.GuestToGuest)
                {
                    TaskLogoutAndRelog.Enqueue();
                    TaskReturnToHomeDC.Enqueue(Player.Name);
                    TaskChangeDatacenter.Enqueue(w, Player.Name);
                    TaskSelectChara.Enqueue(Player.Name);
                    TaskWaitUntilInWorld.Enqueue(w);
                    TaskDesktopNotification.Enqueue($"Arrived to {w}");
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
                TaskTPAndChangeWorld.Enqueue(w);
                TaskDesktopNotification.Enqueue($"Arrived to {w}");
            }
        }

        private void Framework_Update(object framework)
        {
            YesAlreadyManager.Tick();
            if(Svc.ClientState.LocalPlayer != null && DataStore.Territories.Contains(Svc.ClientState.TerritoryType))
            {
                UpdateActiveAetheryte();
            }
            else
            {
                ActiveAetheryte = null;
            }
        }

        public void Dispose()
        {
            Svc.Framework.Update -= Framework_Update;
            Svc.Toasts.ErrorToast -= Toasts_ErrorToast;
            Memory.Dispose();
            ECommonsMain.Dispose();
            P = null;
        }

        void UpdateActiveAetheryte()
        {
            var a = Util.GetValidAetheryte();
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
}