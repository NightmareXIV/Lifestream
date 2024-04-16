using ECommons.GameHelpers;
using Lifestream.Enums;
using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.SameWorld;
public static class TaskTpToResidentialAetheryte
{
    public static void Insert(ResidentialAetheryte target)
    {
        P.TaskManager.Insert(() => Player.Interactable && Svc.ClientState.TerritoryType == target.GetTerritory(), "WaitUntilPlayerInteractable", new(timeLimitMS: 120000));
        P.TaskManager.Insert(WorldChange.WaitUntilNotBusy, new(timeLimitMS: 120000));
        P.TaskManager.Insert(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
        P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination(target.GetTerritory()));
        if (P.Config.WaitForScreen) P.TaskManager.Insert(Utils.WaitForScreen);
    }
}
