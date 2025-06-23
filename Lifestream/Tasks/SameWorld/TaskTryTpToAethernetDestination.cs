using ECommons.Automation.NeoTaskManager;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Lifestream.Schedulers;
using Lifestream.Systems.Legacy;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonAirShipExploration;
using static System.Net.Mime.MediaTypeNames;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskTryTpToAethernetDestination
{
    public static void Enqueue(string targetName, bool allowPartial = false, bool allowTpFallback = false)
    {
        TaskManagerTask[] waiters = [new(WorldChange.WaitUntilMasterAetheryteExists), new FrameDelayTask(10), new(process)];
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        if(P.ActiveAetheryte != null)
        {
            P.TaskManager.Enqueue(process);
        }
        else if(S.Data.CustomAethernet.ActiveAetheryte != null)
        {
            if(Utils.TryFindEqualsOrContains(S.Data.CustomAethernet.ZoneInfo[P.Territory].Aetherytes, x => x.Name, targetName, out var x))
            {
                if(x.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    TaskAethernetTeleport.Enqueue(x.Name);
                }
            }
        }
        else if(S.Data.ResidentialAethernet.ActiveAetheryte != null)
        {
            if(Utils.TryFindEqualsOrContains(S.Data.ResidentialAethernet.ZoneInfo[S.Data.ResidentialAethernet.ActiveAetheryte.Value.TerritoryType].Aetherytes, x => x.Name, targetName, out var x))
            {
                if(x.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    TaskAethernetTeleport.Enqueue(x.Name);
                }
            }
        }
        else
        {
            P.TaskManager.Enqueue(() =>
            {
                if(P.ActiveAetheryte == null && Utils.GetReachableWorldChangeAetheryte() != null)
                {
                    P.TaskManager.InsertMulti([
                        new FrameDelayTask(10),
                        new(WorldChange.TargetReachableWorldChangeAetheryte),
                        new(WorldChange.LockOn),
                        new(WorldChange.EnableAutomove),
                        new(WorldChange.WaitUntilMasterAetheryteExists),
                        new(WorldChange.DisableAutomove),
                        ..waiters
                        ]);
                }
                else if(P.ActiveAetheryte == null)
                {
                    if(allowPartial && processPartial())
                    {
                        return;
                    }
                    if(allowTpFallback)
                    {
                        P.TaskManager.InsertStack(() =>
                        {
                            if(!Utils.EnqueueTeleport(targetName, null))
                            {
                                DuoLog.Error("Destination could not be found");
                            }
                        });
                    }
                    else
                    {
                        DuoLog.Error("Destination could not be found");
                    }
                }
                return;
            }, $"ConditionalLockonTask");

        }

        bool processPartial()
        {
            PluginLog.Debug($"Processing partial command");
            foreach(var x in S.Data.DataStore.Aetherytes)
            {
                foreach(var a in (TinyAetheryte[])[x.Key, .. x.Value])
                {
                    if(a.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        ChatPrinter.Green($"[Lifestream] Destination: {ExcelTerritoryHelper.GetName(x.Key.TerritoryType)} - {a.Name}");
                        P.TaskManager.BeginStack();
                        try
                        {
                            TaskAetheryteAethernetTeleport.Enqueue(x.Key.ID, a.ID);
                        }
                        catch(Exception e)
                        {
                            e.Log();
                        }
                        P.TaskManager.InsertStack();
                        return true;
                    }
                }
            }
            return false;
        }

        void process()
        {
            var master = Utils.GetMaster();
            {
                if(P.ActiveAetheryte != master)
                {
                    var name = master.Name;
                    if(name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName) || C.Renames.TryGetValue(master.ID, out var value) && value.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                    {
                        P.TaskManager.BeginStack();
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(master);
                        P.TaskManager.InsertStack();
                        return;
                    }
                }
            }

            foreach(var x in S.Data.DataStore.Aetherytes[master])
            {
                if(P.ActiveAetheryte != x)
                {
                    var name = x.Name;
                    if(name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName) || C.Renames.TryGetValue(x.ID, out var value) && value.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                    {
                        P.TaskManager.BeginStack();
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(x);
                        P.TaskManager.InsertStack();
                        return;
                    }
                }
            }

            if(P.ActiveAetheryte.Value.ID == 70 && C.Firmament)
            {
                var name = "Firmament";
                if(name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                {
                    P.TaskManager.InsertStack(() =>
                    {
                        TaskRemoveAfkStatus.Enqueue();
                        TaskFirmanentTeleport.Enqueue();
                    });
                    return;
                }
            }

            if(allowPartial && processPartial())
            {
                return;
            }

            if(allowTpFallback)
            {
                P.TaskManager.InsertStack(() =>
                {
                    if(!Utils.EnqueueTeleport(targetName, null))
                    {
                        DuoLog.Error("Destination could not be found");
                    }
                });
                return;
            }

            Notify.Error($"No destination {targetName} found");
            return;
        }
    }
}
