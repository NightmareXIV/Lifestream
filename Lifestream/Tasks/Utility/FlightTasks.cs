using ECommons.Automation;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.Utility;
public static unsafe class FlightTasks
{
    public static bool? FlyIfCan()
    {
        if(Svc.Condition[ConditionFlag.InFlight])
        {
            return true;
        }
        if(Utils.CanFly())
        {
            if(ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 2) == 0 && EzThrottler.Throttle("Jump", 100))
            {
                Chat.ExecuteGeneralAction(2);
            }
        }
        else
        {
            return null;
        }
        return false;
    }
}
