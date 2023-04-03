using ECommons.Automation;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.SimpleGui;
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

        internal TinyAetheryte? ActiveAetheryte
        {
            get
            {
                if(UI.DebugAetheryte != null)
                {
                    return UI.DebugAetheryte;
                }
                return null;
            }
        }

        internal uint Territory
        {
            get
            {
                if(UI.DebugTerritory != 0)
                {
                    return UI.DebugTerritory;
                }
                return Svc.ClientState.TerritoryType;
            }
        }

        public Lifestream(DalamudPluginInterface pluginInterface)
        {
            P = this;
            ECommonsMain.Init(pluginInterface, this);
            new TickScheduler(delegate
            {
                Config = EzConfig.Init<Config>();
                EzConfigGui.Init(UI.Draw);
                EzConfigGui.WindowSystem.AddWindow(new Overlay());
                EzCmd.Add("/lifestream", EzConfigGui.Open);
                TaskManager = new()
                {
                };
                DataStore = new();
                ProperOnLogin.Register(delegate
                {
                    DataStore.BuildWorlds();
                }, true);
            });
        }

        public void Dispose()
        {
            ECommonsMain.Dispose();
            P = null;
        }
    }
}