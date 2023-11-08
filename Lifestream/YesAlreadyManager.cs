using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal static class YesAlreadyManager
    {
        internal static Version Version => Svc.PluginInterface.InstalledPlugins.FirstOrDefault(x => x.IsLoaded && x.InternalName == "YesAlready")?.Version;
        internal static readonly Version NewVersion = new("1.4.0.0");
        internal static bool Reenable = false;
        internal static HashSet<string> Data = null;

        internal static void GetData()
        {
            if (Data != null) return;
            if (Svc.PluginInterface.TryGetData<HashSet<string>>("YesAlready.StopRequests", out var data))
            {
                Data = data;
            }
        }

        internal static void DisableIfNeeded()
        {
            if(Version != null)
            {
                if(Version < NewVersion)
                {
                    if (DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, false, true))
                    {
                        PluginLog.Information("Disabling Yes Already (old)");
                        pl.GetStaticFoP("YesAlready.Service", "Configuration").SetFoP("Enabled", false);
                        Reenable = true;
                    }
                }
                else
                {
                    GetData();
                    if (Data != null)
                    {
                        PluginLog.Information("Disabling Yes Already (new)");
                        Data.Add(Svc.PluginInterface.InternalName);
                        Reenable = true;
                    }
                }
            }
        }

        internal static void EnableIfNeeded()
        {
            if (Reenable && Version != null)
            {
                if (Version < NewVersion)
                {
                    if (DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, false, true))
                    {
                        PluginLog.Information("Enabling Yes Already");
                        pl.GetStaticFoP("YesAlready.Service", "Configuration").SetFoP("Enabled", true);
                        Reenable = false;
                    }
                }
                else
                {
                    GetData();
                    if (Data != null)
                    {
                        PluginLog.Information("Enabling Yes Already (new)");
                        Data.Remove(Svc.PluginInterface.InternalName);
                        Reenable = false;
                    }
                }
            }
        }

        internal static bool IsEnabled()
        {
            if(Version != null)
            {
                if(Version < NewVersion)
                {
                    if (DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, false, true))
                    {
                        return pl.GetStaticFoP("YesAlready.Service", "Configuration").GetFoP<bool>("Enabled");
                    }
                }
                else
                {
                    GetData();
                    if (Data != null)
                    {
                        return !Data.Contains(Svc.PluginInterface.InternalName);
                    }
                }
            }
            
            return false;
        }

        internal static void Tick()
        {
            if (P.TaskManager.IsBusy)
            {
                if (IsEnabled())
                {
                    DisableIfNeeded();
                }
            }
            else
            {
                if (Reenable)
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
}
