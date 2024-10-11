using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Lifestream.Services;
public unsafe class TeleportService
{
    private TeleportService() { }

    public bool TeleportToAetheryte(uint id, uint sub = 0)
    {
        if(!Player.Interactable)
        {
            InternalLog.Warning("Can't teleport - no player");
            return false;
        }
        if(ActionManager.Instance()->GetActionStatus(ActionType.Action, 5) != 0)
        {
            InternalLog.Warning("Can't execute teleport action");
            return false;
        }
        if(Player.IsAnimationLocked)
        {
            InternalLog.Warning("Can't teleport - animation locked");
            return false;
        }
        foreach(var x in Svc.AetheryteList)
        {
            if(x.AetheryteId == id && x.SubIndex == sub)
            {
                Telepo.Instance()->Teleport(id, (byte)sub);
                return true;
            }
        }
        InternalLog.Warning("Could not find teleport destination");
        return false;
    }
}
