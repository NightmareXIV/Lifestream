using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lifestream.Tasks.SameWorld;

namespace Lifestream.Services;
public unsafe class InstanceHandler : IDisposable
{
    private InstanceHandler()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "SelectString", OnPostUpdate);
        var gv = CSFramework.Instance()->GameVersionString;
        if(gv != null && gv != P.Config.GameVersion)
        {
            PluginLog.Information($"New game version detected, new {gv}, old {P.Config.GameVersion}");
            P.Config.GameVersion = gv;
            P.Config.PublicInstances = [];
        }
    }

    public bool CanChangeInstance()
    {
        return P.Config.ShowInstanceSwitcher && !Utils.IsDisallowedToUseAethernet() && !P.TaskManager.IsBusy && !IsOccupied() && S.InstanceHandler.GetInstance() != 0 && TaskChangeInstance.GetAetheryte() != null;
    }

    private void OnPostUpdate(AddonEvent type, AddonArgs args)
    {
        if(
            UIState.Instance()->PublicInstance.IsInstancedArea()
            && Svc.Targets.Target?.ObjectKind == ObjectKind.Aetheryte
            && Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
            && TryGetAddonMaster<AddonMaster.SelectString>(out var m)
            && m.IsAddonReady
            && (m.Entries.Any(x => x.Text.ContainsAny(Lang.TravelToInstancedArea)) || m.Text == Lang.ToReduceCongestion)
            )
        {
            var inst = *P.Memory.MaxInstances;
            if(inst < 2 || inst > 9)
            {
                if(EzThrottler.Throttle("InstanceWarning", 5000)) PluginLog.Warning($"Instance count is wrong, received {inst}, please report to developer");
            }
            else
            {
                if(P.Config.PublicInstances.TryGetValue(P.Territory, out var value) && value == inst)
                {
                    //
                }
                else
                {
                    P.Config.PublicInstances[P.Territory] = inst;
                    EzConfig.Save();
                }
            }
        }
    }

    public int GetInstance()
    {
        return (int)UIState.Instance()->PublicInstance.InstanceId;
    }

    public bool InstancesInitizliaed(out int maxInstances)
    {
        return P.Config.PublicInstances.TryGetValue(P.Territory, out maxInstances);
    }

    public void Dispose()
    {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostUpdate, "SelectString", OnPostUpdate);
    }
}
