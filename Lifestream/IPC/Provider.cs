using ECommons.EzIpcManager;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.GUI;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Shortcuts;
using System.Linq;

namespace Lifestream.IPC;
public class Provider
{
    public Provider()
    {
        EzIPC.Init(this);
    }

    [EzIPC]
    public void ExecuteCommand(string arguments)
    {
        P.ProcessCommand("/li", arguments);
    }

    [EzIPC]
    public AddressBookEntryTuple BuildAddressBookEntry(string worldStr, string cityStr, string wardNum, string plotApartmentNum, bool isApartment, bool isSubdivision)
    {
        return Utils.BuildAddressBookEntry(worldStr, cityStr, wardNum, plotApartmentNum, isApartment, isSubdivision).AsTuple();
    }

    [EzIPC]
    public bool IsHere(AddressBookEntryTuple addressBookEntryTuple)
    {
        return Utils.IsHere(AddressBookEntry.FromTuple(addressBookEntryTuple));
    }

    [EzIPC]
    public bool IsQuickTravelAvailable(AddressBookEntryTuple addressBookEntryTuple)
    {
        return Utils.IsQuickTravelAvailable(AddressBookEntry.FromTuple(addressBookEntryTuple));
    }

    [EzIPC]
    public void GoToHousingAddress(AddressBookEntryTuple addressBookEntryTuple)
    {
        AddressBookEntry.FromTuple(addressBookEntryTuple).GoTo();
    }

    [EzIPC]
    public bool IsBusy()
    {
        return P.TaskManager.IsBusy || (P.followPath != null && P.followPath.Waypoints.Count > 0);
    }

    [EzIPC]
    public void Abort()
    {
        P.TaskManager.Abort();
        P.followPath?.Stop();
    }

    [EzIPC]
    public bool CanVisitSameDC(string world)
    {
        return P.DataStore.Worlds.Contains(world);
    }

    [EzIPC]
    public bool CanVisitCrossDC(string world)
    {
        return P.DataStore.DCWorlds.Contains(world);
    }

    [EzIPC]
    public void TPAndChangeWorld(string w, bool isDcTransfer, string secondaryTeleport, bool noSecondaryTeleport, int? gateway, bool? doNotify, bool? returnToGateway)
    {
        P.TPAndChangeWorld(w, isDcTransfer, secondaryTeleport, noSecondaryTeleport, (WorldChangeAetheryte?)gateway, doNotify, returnToGateway);
    }

    [EzIPC]
    public int? GetWorldChangeAetheryteByTerritoryType(uint territoryType)
    {
        return (int)Utils.GetWorldChangeAetheryteByTerritoryType(territoryType);
    }

    [EzIPC]
    public bool ChangeWorld(string world)
    {
        if(IsBusy()) return false;
        if(CanVisitCrossDC(world))
        {
            P.TPAndChangeWorld(world, true);
            return true;
        }
        else if(CanVisitSameDC(world))
        {
            P.TPAndChangeWorld(world, false);
            return true;
        }
        return false;
    }

    [EzIPC]
    public bool AethernetTeleport(string destination)
    {
        if(IsBusy()) return false;
        TaskTryTpToAethernetDestination.Enqueue(destination);
        return true;
    }

    [EzIPC]
    public bool Teleport(uint destination, byte subIndex)
    {
        return S.TeleportService.TeleportToAetheryte(destination, subIndex);
    }

    [EzIPC]
    public bool TeleportToFC()
    {
        if(!P.TaskManager.IsBusy)
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.FC);
            return true;
        }
        return false;
    }

    [EzIPC]
    public bool TeleportToHome()
    {
        if(!P.TaskManager.IsBusy)
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Home);
            return true;
        }
        return false;
    }

    [EzIPC]
    public bool TeleportToApartment()
    {
        if(!P.TaskManager.IsBusy)
        {
            TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Apartment);
            return true;
        }
        return false;
    }

    [EzIPC]
    public (HousePathData Private, HousePathData FC) GetHousePathData(ulong CID)
    {
        return (P.Config.HousePathDatas.FirstOrDefault(x => x.CID == CID && x.IsPrivate), P.Config.HousePathDatas.FirstOrDefault(x => x.CID == CID && !x.IsPrivate));
    }

    [EzIPC]
    public uint GetResidentialTerritory(ResidentialAetheryteKind r)
    {
        return r.GetResidentialTerritory();
    }

    [EzIPC]
    public Vector3? GetPlotEntrance(uint territory, int plot)
    {
        return Utils.GetPlotEntrance(territory, plot);
    }

    [EzIPC] 
    public void EnqueuePropertyShortcut(TaskPropertyShortcut.PropertyType type, HouseEnterMode? mode)
    {
        TaskPropertyShortcut.Enqueue(type, mode);
    }

    [EzIPC]
    public (ResidentialAetheryteKind Kind, int Ward, int Plot)? GetCurrentPlotInfo()
    {
        if(UIHouseReg.TryGetCurrentPlotInfo(out var kind, out var ward, out var plot))
        {
            return (kind, ward, plot);
        }
        return null;
    }

    [EzIPCEvent] public Action OnHouseEnterError;
}
