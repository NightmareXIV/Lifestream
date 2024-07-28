using ECommons.EzSharedDataManager;

namespace Lifestream.IPC;

internal static class YesAlreadyManager
{
    internal static bool Reenable = false;
    internal static HashSet<string> Data = null;

    internal static void GetData()
    {
        if(Data != null) return;
        if(EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var data))
        {
            Data = data;
        }
    }

    internal static void DisableIfNeeded()
    {
        GetData();
        if(Data != null)
        {
            PluginLog.Information("Disabling Yes Already (new)");
            Data.Add(Svc.PluginInterface.InternalName);
            Reenable = true;
        }
    }

    internal static void EnableIfNeeded()
    {
        if(Reenable)
        {
            GetData();
            if(Data != null)
            {
                PluginLog.Information("Enabling Yes Already (new)");
                Data.Remove(Svc.PluginInterface.InternalName);
                Reenable = false;
            }
        }
    }

    internal static bool IsEnabled()
    {
        GetData();
        if(Data != null)
        {
            return !Data.Contains(Svc.PluginInterface.InternalName);
        }
        return false;
    }

    internal static void Tick()
    {
        if(P.TaskManager.IsBusy)
        {
            if(IsEnabled())
            {
                DisableIfNeeded();
            }
        }
        else
        {
            if(Reenable)
            {
                EnableIfNeeded();
            }
        }
    }

    internal static bool? WaitForYesAlreadyDisabledTask()
    {
        return !IsEnabled();
    }
}
