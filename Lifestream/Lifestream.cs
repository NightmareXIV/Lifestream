using ECommons.Automation;
using ECommons.Configuration;
using ECommons.SimpleGui;
using Lumina.Excel.GeneratedSheets;

namespace Lifestream
{
    public class Lifestream : IDalamudPlugin
    {
        public string Name => throw new NotImplementedException();
        internal static Lifestream P;
        internal Config Config;
        internal TaskManager TaskManager;
        internal DataStore DataStore;

        internal Aetheryte NearAetheryte
        {
            get
            {
                if(UI.)
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
            });
        }

        public void Dispose()
        {
            ECommonsMain.Dispose();
            P = null;
        }
    }
}