using ECommons.Automation.NeoTaskManager.Tasks;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;

internal static class TaskTryTpToAethernetDestination
{
    public static void Enqueue(string targetName)
    {
        if(P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        if(P.ActiveAetheryte != null)
        {
            P.TaskManager.Enqueue(Process);
        }
        else if(P.CustomAethernet.ActiveAetheryte != null)
        {
            foreach(var x in P.CustomAethernet.ZoneInfo[Svc.ClientState.TerritoryType])
            {
                if(x.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    TaskAethernetTeleport.Enqueue(x.Name);
                    break;
                }
            }
        }
        else if(P.ResidentialAethernet.ActiveAetheryte != null)
        {
            foreach(var x in P.ResidentialAethernet.ZoneInfo[P.ResidentialAethernet.ActiveAetheryte.Value.TerritoryType].Aetherytes)
            {
                if(x.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    TaskAethernetTeleport.Enqueue(x.Name);
                    break;
                }
            }
        }
        else
        {
            P.TaskManager.Enqueue(() =>
            {
                if(P.ActiveAetheryte == null && Utils.GetReachableWorldChangeAetheryte() != null)
                {
                    P.TaskManager.InsertMulti(
                        new FrameDelayTask(10),
                        new(WorldChange.TargetReachableAetheryte),
                        new(WorldChange.LockOn),
                        new(WorldChange.EnableAutomove),
                        new(WorldChange.WaitUntilMasterAetheryteExists),
                        new(WorldChange.DisableAutomove)
                        );
                }
            }, "ConditionalLockonTask");
            P.TaskManager.Enqueue(WorldChange.WaitUntilMasterAetheryteExists);
            P.TaskManager.EnqueueDelay(10, true);
            P.TaskManager.Enqueue(Process);
        }

        void Process()
        {
            var master = Utils.GetMaster();
            {
                if(P.ActiveAetheryte != master)
                {
                    var name = master.Name;
                    if(name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName) || P.Config.Renames.TryGetValue(master.ID, out var value) && value.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                    {
                        P.TaskManager.BeginStack();
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(master);
                        P.TaskManager.InsertStack();
                        return;
                    }
                }
            }

            foreach(var x in P.DataStore.Aetherytes[master])
            {
                if(P.ActiveAetheryte != x)
                {
                    var name = x.Name;
                    if(name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName) || P.Config.Renames.TryGetValue(x.ID, out var value) && value.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                    {
                        P.TaskManager.BeginStack();
                        TaskRemoveAfkStatus.Enqueue();
                        TaskAethernetTeleport.Enqueue(x);
                        P.TaskManager.InsertStack();
                        return;
                    }
                }
            }

            if(P.ActiveAetheryte.Value.ID == 70 && P.Config.Firmament)
            {
                var name = "Firmament";
                if(name.ContainsAny(StringComparison.OrdinalIgnoreCase, targetName))
                {
                    P.TaskManager.BeginStack();
                    TaskRemoveAfkStatus.Enqueue();
                    TaskFirmanentTeleport.Enqueue();
                    P.TaskManager.InsertStack();
                    return;
                }
            }
            Notify.Error($"No destination {targetName} found");
            return;
        }
    }
}
