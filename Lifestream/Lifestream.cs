using Dalamud.Game;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ECommons.StringHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lifestream.GUI;
using Lifestream.Tasks;
using Lumina.Excel.GeneratedSheets;

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
            ECommonsMain.Init(pluginInterface, this);
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
                EqualStrings.RegisterEquality("Guilde des aventuriers (Guildes des armuriers & forgeron...", "Guilde des aventuriers (Guildes des armuriers & forgerons/Maelstrom)");
            });
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
                    else
                    {
                        Notify.Error($"Destination world not found");
                    }
                }
            }
        }

        private void TPAndChangeWorld(string w)
        {
            if (!Svc.PluginInterface.PluginInternalNames.Contains("TeleporterPlugin"))
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
            if(ActionManager.Instance()->GetActionStatus(ActionType.Spell, 5) != 0)
            {
                Notify.Error("You are unable to teleport at this time");
                return;
            }
            if (Svc.Party.Length > 1 && !P.Config.LeavePartyBeforeWorldChange)
            {
                Notify.Warning("You must disband party in order to switch worlds");
            }
            Notify.Info($"Destination: {w}");
            TaskTPAndChangeWorld.Enqueue(w);
        }

        private void Framework_Update(Framework framework)
        {
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