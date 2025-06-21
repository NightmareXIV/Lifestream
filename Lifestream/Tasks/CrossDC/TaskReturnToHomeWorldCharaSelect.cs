using ECommons.Automation.UIInput;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.CrossDC;
public static unsafe class TaskReturnToHomeWorldCharaSelect
{
    public static void Enqueue(string charaName, uint charaWorld, uint currentLoginWorld)
    {
        PluginLog.Debug($"Beginning returning home process.");
        P.TaskManager.Enqueue(() => DCChange.OpenContextMenuForChara(charaName, charaWorld, currentLoginWorld), nameof(DCChange.OpenContextMenuForChara), TaskSettings.Timeout5M);
        P.TaskManager.Enqueue(DCChange.SelectReturnToHomeWorld);
        P.TaskManager.Enqueue(ConfirmReturnToHomeWorld, TaskSettings.Timeout2M);
        P.TaskManager.Enqueue(DCChange.SelectOk, TaskSettings.TimeoutInfinite);
        P.TaskManager.Enqueue(() =>
        {
            return !(TryGetAddonByName<AtkUnitBase>("NowLoading", out var nl) && nl->IsVisible) && TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m) && m.IsAddonReady;
        });
    }

    public static bool? ConfirmReturnToHomeWorld()
    {
        if(TryGetAddonByName<AtkUnitBase>("LobbyWKTCheckHome", out var addon) && IsAddonReady(addon))
        {
            if(addon->GetButtonNodeById(3)->IsEnabled)
            {
                if(DCChange.DCThrottle && EzThrottler.Throttle("ConfirmHomeWorldVisit", 5000))
                {
                    PluginLog.Debug($"[DCChange] Confirming home world transfer");
                    addon->GetButtonNodeById(3)->ClickAddonButton(addon);
                    return true;
                }
            }
            else
            {
                DCChange.DCRethrottle();
            }
        }
        else
        {
            DCChange.DCRethrottle();
        }
        return false;
    }
}
