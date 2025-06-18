using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Lifestream.Services;
public unsafe class TeleportService
{
    private TeleportService() { }

    public bool TeleportToAetheryte(uint id, uint sub = 0, bool wait = false)
    {
        if(!CanTeleport(out var err))
        {
            InternalLog.Warning(err);
            return false;
        }
        foreach(var x in Svc.AetheryteList)
        {
            if(x.AetheryteId == id && x.SubIndex == sub)
            {
                Telepo.Instance()->Teleport(id, (byte)sub);
                if(wait)
                {
                    P.TaskManager.InsertMulti(
                        new(() => !IsScreenReady()),
                        new(() => IsScreenReady() && Player.Interactable)
                        );
                }
                return true;
            }
        }
        InternalLog.Warning($"Could not find teleport destination for {id}");
        return false;
    }

    public bool CanTeleport(out string error)
    {
        error = null;
        if(!Player.Interactable)
        {
            error = ("Can't teleport - no player");
            return false;
        }
        if(ActionManager.Instance()->GetActionStatus(ActionType.Action, 5) != 0)
        {
            error = ("Can't execute teleport action");
            return false;
        }
        if(Player.IsAnimationLocked)
        {
            error = ("Can't teleport - animation locked");
            return false;
        }
        return true;
    }
}
