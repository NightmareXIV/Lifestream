using ECommons.ExcelServices;
using Lifestream.GUI.Windows;

namespace Lifestream.IPC;
public static unsafe class IpcUtils
{
    public static bool InitiateTravelFromCharaSelectScreenInternal(string charaName, string charaHomeWorld, string destination, bool noLogin)
    {
        var homeWorldData = ExcelWorldHelper.Get(charaHomeWorld) ?? throw new NullReferenceException($"Invalid home world specified: {charaHomeWorld}");
        if(CharaSelectOverlay.TryGetValidCharaSelectListMenu(out var m))
        {
            var chara = m.Characters.FirstOrDefault(x => x.Name == charaName && x.HomeWorld == homeWorldData.RowId);
            if(chara == null)
            {
                //PluginLog.Error($"Character not found: {charaName}@{charaHomeWorld}");
                return false;
            }

            var worlds = Utils.GetVisitableWorldsFrom(homeWorldData).ToArray();
            if(worlds.TryGetFirst(x => x.Name == destination, out var destinationWorldData))
            {
                if(chara.IsVisitingAnotherDC)
                {
                    CharaSelectOverlay.ReconnectToValidDC(chara.Name, chara.CurrentWorld, chara.HomeWorld, destinationWorldData, noLogin);
                    return true;
                }
                else
                {
                    CharaSelectOverlay.Command(chara.Name, chara.CurrentWorld, chara.HomeWorld, destinationWorldData, noLogin);
                    return true;
                }
            }
            else
            {
                //PluginLog.Error($"Destination {destination} is unavailable for specified character");
                return false;
            }
        }
        else
        {
            //PluginLog.Error($"Can not initiate travel from current state");
            return false;
        }
    }

    public static bool InitiateLoginFromCharaSelectScreenInternal(string charaName, string charaHomeWorld)
    {
        var homeWorldData = ExcelWorldHelper.Get(charaHomeWorld) ?? throw new NullReferenceException($"Invalid home world specified: {charaHomeWorld}");
        if(CharaSelectOverlay.TryGetValidCharaSelectListMenu(out var m))
        {
            var chara = m.Characters.FirstOrDefault(x => x.Name == charaName && x.HomeWorld == homeWorldData.RowId);
            if(chara == null)
            {
                //PluginLog.Error($"Character not found: {charaName}@{charaHomeWorld}");
                return false;
            }

            var worlds = Utils.GetVisitableWorldsFrom(homeWorldData).ToArray();
            if(chara.IsVisitingAnotherDC)
            {
                CharaSelectOverlay.ReconnectToValidDC(chara.Name, chara.CurrentWorld, chara.HomeWorld, null, false);
                return true;
            }
            else
            {
                CharaSelectOverlay.Command(chara.Name, chara.CurrentWorld, chara.HomeWorld, null, false);
                return true;
            }
        }
        else
        {
            //PluginLog.Error($"Can not initiate travel from current state");
            return false;
        }
    }
}