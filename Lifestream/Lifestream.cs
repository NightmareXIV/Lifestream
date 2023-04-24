using Dalamud.Game;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.MathHelpers;
using ECommons.SimpleGui;
using ECommons.StringHelpers;
using Lifestream.GUI;
using Lumina.Excel.GeneratedSheets;

namespace Lifestream
{
    public class Lifestream : IDalamudPlugin
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
                EzCmd.Add("/lifestream", EzConfigGui.Open);
                TaskManager = new()
                {
                };
                DataStore = new();
                ProperOnLogin.Register(DataStore.BuildWorlds);
                Svc.Framework.Update += Framework_Update;
                Memory = new();
                EqualStrings.RegisterEquality("Guilde des aventuriers (Guildes des armuriers & forgeron...", "Guilde des aventuriers (Guildes des armuriers & forgerons/Maelstrom)");
            });
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