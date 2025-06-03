using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.EzEventManager;

namespace Lifestream.Services;
public class DtrManager : IDisposable
{
    public static Dictionary<int, SeString> InstanceNumbers = new()
    {
        [1] = "\ue0b1",
        [2] = "\ue0b2",
        [3] = "\ue0b3",
        [4] = "\ue0b4",
        [5] = "\ue0b5",
        [6] = "\ue0b6",
        [7] = "\ue0b7",
        [8] = "\ue0b8",
        [9] = "\ue0b9",
    };
    public static string Name = "LifestreamInstance";
    public IDtrBarEntry Entry;
    private DtrManager()
    {
        Entry = Svc.DtrBar.Get(Name);
        Entry.Shown = false;
        new EzTerritoryChanged(OnTerritoryChanged);
        Refresh();
    }

    public void Refresh() => OnTerritoryChanged(Svc.ClientState.TerritoryType);

    private void OnTerritoryChanged(ushort obj)
    {
        Entry.Shown = false;
        if(C.EnableDtrBar && S.InstanceHandler.GetInstance() > 0)
        {
            var str = InstanceNumbers.SafeSelect(obj);
            if(str != null)
            {
                Entry.Text = str;
                Entry.Tooltip = $"You are in instance {obj}";
                Entry.Shown = true;
            }
        }
    }

    public void Dispose()
    {
        Entry.Remove();
        Entry = null;
    }
}
