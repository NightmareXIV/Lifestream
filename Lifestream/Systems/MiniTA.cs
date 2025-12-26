using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifestream.Systems;

public static class MiniTA
{
    public static void Tick()
    {
        if(TryGetAddonMaster<AddonMaster.SelectOk>(out var m) && m.IsAddonReady)
        {
            if(m.Text.ContainsAny(StringComparison.OrdinalIgnoreCase, Lang.RemainingSubTime))
            {
                if(EzThrottler.Throttle("RSTOk", 200)) m.Ok();
            }
        }
    }
}
