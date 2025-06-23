using ECommons.EzIpcManager;
using ECommons.GameHelpers;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.GUI;
using Lifestream.GUI.Windows;
using Lifestream.Tasks;
using Lifestream.Tasks.Login;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.Sheets;

namespace Lifestream.IPC;
public class IPCProvider
{
    private IPCProvider()
    {
        EzIPC.Init(this, reducedLogging: true);
    }

    [EzIPC]
    public IDalamudPlugin Instance()
    {
        return P;
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
        return S.Data.DataStore.Worlds.Contains(world);
    }

    [EzIPC]
    public bool CanVisitCrossDC(string world)
    {
        return S.Data.DataStore.DCWorlds.Contains(world);
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

    /// <summary>
    /// Requests Lifestream to change world of current character to a different one.
    /// </summary>
    /// <param name="worldId"></param>
    /// <returns></returns>
    [EzIPC]
    public bool ChangeWorldById(uint worldId)
    {
        if(Svc.Data.GetExcelSheet<World>().TryGetRow(worldId, out var sheet))
        {
            return ChangeWorld(sheet.Name.GetText());
        }
        return false;
    }

    /// <summary>
    /// Requests aethernet teleport to be executed by name, if possible. Must be within an aetheryte or aetheryte shard range.
    /// </summary>
    /// <param name="destination"></param>
    /// <returns></returns>
    [EzIPC]
    public bool AethernetTeleport(string destination)
    {
        if(IsBusy()) return false;
        TaskTryTpToAethernetDestination.Enqueue(destination);
        return true;
    }

    /// <summary>
    /// Requests aethernet teleport to be executed by Place Name ID from <see cref="PlaceName"/> sheet, if possible. Must be within an aetheryte or aetheryte shard range. 
    /// </summary>
    /// <param name="placeNameRowId"></param>
    /// <returns></returns>
    [EzIPC]
    public bool AethernetTeleportByPlaceNameId(uint placeNameRowId)
    {
        if(Svc.Data.GetExcelSheet<PlaceName>().TryGetRow(placeNameRowId, out var row))
        {
            return AethernetTeleport(row.Name.GetText());
        }
        return false;
    }

    /// <summary>
    /// Requests aethernet teleport to be executed by ID from <see cref="Aetheryte"/> sheet, if possible. Must be within an aetheryte or aetheryte shard range. 
    /// </summary>
    /// <param name="aethernetSheetRowId"></param>
    /// <returns></returns>
    [EzIPC]
    public bool AethernetTeleportById(uint aethernetSheetRowId)
    {
        var name = Utils.GetAethernetNameWithOverrides(aethernetSheetRowId);
        if(name == null) return false;
        return AethernetTeleport(name);
    }

    /// <summary>
    /// Requests aethernet teleport to be executed by ID from <see cref="HousingAethernet"/> sheet, if possible. Must be within an aetheryte shard range. 
    /// </summary>
    /// <returns></returns>
    [EzIPC]
    public bool HousingAethernetTeleportById(uint housingAethernetSheetRow)
    {
        if(Svc.Data.GetExcelSheet<HousingAethernet>().TryGetRow(housingAethernetSheetRow, out var row))
        {
            return AethernetTeleport(row.PlaceName.Value.Name.GetText());
        }
        return false;
    }

    /// <summary>
    /// Requests aethernet teleport to Firmament. Must be within a Foundation aetheryte range. 
    /// </summary>
    /// <returns></returns>
    [EzIPC]
    public bool AethernetTeleportToFirmament()
    {
        return AethernetTeleport(Utils.GetAethernetNameWithOverrides(TaskAetheryteAethernetTeleport.FirmamentAethernetId));
    }

    /// <summary>
    /// Retrieves active aetheryte/aetheryte shard ID if present
    /// </summary>
    /// <returns></returns>
    [EzIPC]
    public uint GetActiveAetheryte()
    {
        if(P.ActiveAetheryte != null)
        {
            return P.ActiveAetheryte.Value.ID;
        }
        return 0;
    }

    /// <summary>
    /// Retrieves active custom aetheryte ID if present
    /// </summary>
    /// <returns></returns>
    [EzIPC]
    public uint GetActiveCustomAetheryte()
    {
        if(S.Data.CustomAethernet.ActiveAetheryte != null)
        {
            return S.Data.CustomAethernet.ActiveAetheryte.Value.ID;
        }
        return 0;
    }

    /// <summary>
    /// Retrieves active housing aetheryte shard ID if present
    /// </summary>
    /// <returns></returns>
    [EzIPC]
    public uint GetActiveResidentialAetheryte()
    {
        if(S.Data.ResidentialAethernet.ActiveAetheryte != null)
        {
            return S.Data.ResidentialAethernet.ActiveAetheryte.Value.ID;
        }
        return 0;
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
        return (Utils.GetHousePathDatas().FirstOrDefault(x => x.CID == CID && x.IsPrivate), Utils.GetHousePathDatas().FirstOrDefault(x => x.CID == CID && !x.IsPrivate));
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
    public void EnterApartment(bool enter)
    {
        TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Apartment, null, null, enter);
    }

    [EzIPC]
    public void EnqueueInnShortcut(int? innIndex)
    {
        TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Inn, default, innIndex);
    }

    [EzIPC]
    public void EnqueueLocalInnShortcut(int? innIndex)
    {
        TaskPropertyShortcut.Enqueue(TaskPropertyShortcut.PropertyType.Inn, default, innIndex, useSameWorld: true);
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

    [EzIPC]
    public bool CanChangeInstance()
    {
        return S.InstanceHandler.CanChangeInstance();
    }

    [EzIPC]
    public int GetNumberOfInstances()
    {
        return S.InstanceHandler.InstancesInitizliaed(out var ret) ? ret : 0;
    }

    [EzIPC]
    public void ChangeInstance(int number)
    {
        TaskRemoveAfkStatus.Enqueue();
        TaskChangeInstance.Enqueue(number);
    }

    [EzIPC]
    public int GetCurrentInstance()
    {
        return S.InstanceHandler.GetInstance();
    }

    [EzIPC]
    public bool? HasApartment()
    {
        if(Player.Object.HomeWorld.RowId != Player.Object.CurrentWorld.RowId) return null;
        return TaskPropertyShortcut.GetApartmentAetheryteID().ID != 0;
    }

    [EzIPC]
    public bool? HasPrivateHouse()
    {
        if(Player.Object.HomeWorld.RowId != Player.Object.CurrentWorld.RowId) return null;
        return TaskPropertyShortcut.GetPrivateHouseAetheryteID() != 0;
    }

    [EzIPC]
    public bool? HasFreeCompanyHouse()
    {
        if(Player.Object.HomeWorld.RowId != Player.Object.CurrentWorld.RowId) return null;
        return TaskPropertyShortcut.GetFreeCompanyAetheryteID() != 0;
    }

    [EzIPC]
    public void Move(List<Vector3> path)
    {
        P.FollowPath.Move(path, true);
    }

    [EzIPC]
    public bool CanMoveToWorkshop()
    {
        var data = Utils.GetFCPathData();
        if(data == null) return false;
        var plotDataAvailable = UIHouseReg.TryGetCurrentPlotInfo(out var kind, out var ward, out var plot);
        if(plotDataAvailable)
        {
            return data.PathToWorkshop.Count > 0 && data.ResidentialDistrict == kind && data.Ward == ward && data.Plot == plot;
        }
        return false;
    }

    [EzIPC]
    public void MoveToWorkshop()
    {
        if(IsBusy()) return;
        var data = Utils.GetFCPathData();
        if(data == null) return;
        var plotDataAvailable = UIHouseReg.TryGetCurrentPlotInfo(out var kind, out var ward, out var plot);
        if(plotDataAvailable && data.PathToWorkshop.Count > 0 && data.PathToWorkshop.Count > 0 && data.ResidentialDistrict == kind && data.Ward == ward && data.Plot == plot)
        {
            P.FollowPath.Move(data.PathToWorkshop, true);
        }
    }

    [EzIPC]
    public uint GetRealTerritoryType()
    {
        return P.Territory;
    }

    [EzIPC]
    public bool CanAutoLogin() => Utils.CanAutoLogin();

    [EzIPC]
    public bool ConnectAndOpenCharaSelect(string charaName, string charaHomeWorld)
    {
        if(IsBusy())
        {
            return false;
        }
        return TaskConnectAndOpenCharaSelect.Enqueue(charaName, charaHomeWorld);
    }

    [EzIPC]
    public bool InitiateTravelFromCharaSelectScreen(string charaName, string charaHomeWorld, string destination, bool noLogin)
    {
        if(IsBusy())
        {
            return false;
        }
        return IpcUtils.InitiateTravelFromCharaSelectScreenInternal(charaName, charaHomeWorld, destination, noLogin);
    }

    [EzIPC]
    public bool CanInitiateTravelFromCharaSelectList()
    {
        return CharaSelectOverlay.TryGetValidCharaSelectListMenu(out var m);
    }

    [EzIPC]
    public bool ConnectAndTravel(string charaName, string charaHomeWorld, string destination, bool noLogin)
    {
        if(IsBusy() || !CanAutoLogin())
        {
            return false;
        }
        ConnectAndOpenCharaSelect(charaName, charaHomeWorld);
        P.TaskManager.Enqueue(() => IpcUtils.InitiateTravelFromCharaSelectScreenInternal(charaName, charaHomeWorld, destination, noLogin));
        return true;
    }

    [EzIPCEvent] public System.Action OnHouseEnterError;
}
