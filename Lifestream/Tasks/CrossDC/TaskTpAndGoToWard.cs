
using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.AtkReaders;
using Lifestream.Enums;
using Lifestream.Systems;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;
using ResidentialAetheryteKind = Lifestream.Enums.ResidentialAetheryteKind;

namespace Lifestream.Tasks.CrossDC;
public static unsafe class TaskTpAndGoToWard
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="world"></param>
    /// <param name="residentialArtheryte"></param>
    /// <param name="ward">Starts with 0</param>
    /// <param name="plot">Starts with 0</param>
    /// <param name="isApartment"></param>
    /// <param name="isApartmentSubdivision"></param>
    public static void Enqueue(string world, ResidentialAetheryteKind residentialArtheryte, int ward, int plot, bool isApartment, bool isApartmentSubdivision)
    {
        var gateway = DetermineGatewayAetheryte(residentialArtheryte);
        if(Player.CurrentWorld != world)
        {
            if(P.DataStore.Worlds.TryGetFirst(x => x.StartsWith(world == "" ? Player.HomeWorld : world, StringComparison.OrdinalIgnoreCase), out var w))
            {
                P.TPAndChangeWorld(w, false, null, true, gateway, false, gateway != null);
            }
            else if(P.DataStore.DCWorlds.TryGetFirst(x => x.StartsWith(world == "" ? Player.HomeWorld : world, StringComparison.OrdinalIgnoreCase), out var dcw))
            {
                P.TPAndChangeWorld(dcw, true, null, true, gateway, false, gateway != null);
            }
        }
        P.TaskManager.Enqueue(TaskReturnToGateway.WaitUntilInteractable);
        if(P.Config.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(() =>
        {
            if(Svc.ClientState.TerritoryType != residentialArtheryte.GetTerritory())
            {
                TaskTpToResidentialAetheryte.Insert(residentialArtheryte);
            }
        }, "TaskTpToResidentialAetheryteIfNeeded");
        P.TaskManager.Enqueue(() => Utils.GetReachableAetheryte(x => x.ObjectKind == ObjectKind.Aetheryte) != null, "WaitUntilReachableAetheryteExists");
        TaskApproachAetheryteIfNeeded.Enqueue();
        TaskGoToResidentialDistrict.Enqueue(ward);
        EnqueueFromResidentialAetheryte(residentialArtheryte, plot, isApartment, isApartmentSubdivision, true);

    }

    public static void EnqueueFromResidentialAetheryte(ResidentialAetheryteKind residentialArtheryte, int plot, bool isApartment, bool isApartmentSubdivision, bool fromStart)
    {
        if(isApartment)
        {
            var target = P.ResidentialAethernet.ZoneInfo.SafeSelect(residentialArtheryte.GetResidentialTerritory());
            if(target != null && target.Aetherytes.TryGetFirst(x => (isApartmentSubdivision ? ResidentialAethernet.ApartmentSubdivisionAetherytes : ResidentialAethernet.ApartmentAetherytes).Contains(x.ID), out var aetheryte))
            {
                TaskApproachHousingAetheryte.Enqueue();
                TaskAethernetTeleport.Enqueue(aetheryte.Name);
                TaskApproachAndInteractWithApartmentEntrance.Enqueue(true);
                P.TaskManager.Enqueue(SelectGoToSpecifiedApartment);
                P.TaskManager.Enqueue(() => SelectApartment(plot), $"SelectApartment {plot}");
                if(!P.Config.AddressApartmentNoEntry) P.TaskManager.Enqueue(ConfirmApartmentEnterYesno);
            }
        }
        else
        {
            if(P.ResidentialAethernet.HousingData.Data.TryGetValue(residentialArtheryte.GetResidentialTerritory(), out var plotInfos))
            {
                var info = plotInfos.SafeSelect(plot);
                if(info != null)
                {
                    var aetheryte = P.ResidentialAethernet.ZoneInfo.SafeSelect(residentialArtheryte.GetResidentialTerritory())?.Aetherytes.FirstOrDefault(x => x.ID == info.AethernetID);
                    if(fromStart)
                    {
                        if(!ResidentialAethernet.StartingAetherytes.Contains(info.AethernetID))
                        {
                            TaskApproachHousingAetheryte.Enqueue();
                            if(aetheryte != null)
                            {
                                TaskAethernetTeleport.Enqueue(aetheryte.Value.Name);
                                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
                                P.TaskManager.Enqueue(Utils.WaitForScreen);
                            }
                        }
                        if(!P.Config.AddressNoPathing) TaskMoveToHouse.Enqueue(info, ResidentialAethernet.StartingAetherytes.Contains(info.AethernetID));
                    }
                    else
                    {
                        if(info.AethernetID != P.ResidentialAethernet.ActiveAetheryte.Value.ID)
                        {
                            if(aetheryte != null)
                            {
                                TaskAethernetTeleport.Enqueue(aetheryte.Value.Name);
                                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
                                P.TaskManager.Enqueue(Utils.WaitForScreen);
                            }
                        }
                        if(!P.Config.AddressNoPathing)
                        {
                            TaskMoveToHouse.Enqueue(info, false);
                        }
                    }
                }
            }
        }
    }

    public static unsafe bool SelectGoToSpecifiedApartment()
    {
        return Utils.TrySelectSpecificEntry(Lang.GoToSpecifiedApartment, () => EzThrottler.Throttle("SelectStringApartment"));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="apartmentNum">Starts with 0</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static unsafe bool? SelectApartment(int apartmentNum)
    {
        var section = (int)((apartmentNum) / 15);
        if(TryGetAddonByName<AtkUnitBase>("MansionSelectRoom", out var addon) && IsAddonReady(addon))
        {
            var reader = new ReaderMansionSelectRoom(addon);
            if(reader.IsLoaded)
            {
                if(reader.Section == section)
                {
                    if(EzThrottler.Throttle("ApartmentSelectRoom", 5000))
                    {
                        var target = apartmentNum - section * 15;
                        if(target < 0 || target > 14) throw new InvalidOperationException($"Apartment number was out of range: was {target}, section {section}");
                        if(target >= reader.SectionRoomsCount)
                        {
                            DuoLog.Error($"Could not find apartment {apartmentNum + 1} ({target} in section {section})");
                            return null;
                        }
                        var roomInfo = reader.Rooms.SafeSelect(target);
                        if(roomInfo.Owner == "" || roomInfo.AccessState == 1)
                        {
                            DuoLog.Error($"Apartment {apartmentNum + 1} is vacant, could not enter.");
                            return null;
                        }
                        Callback.Fire(addon, true, 0, target);
                        return true;
                    }
                }
                else
                {
                    if(section < 0 || section >= reader.ExistingSectionsCount)
                    {
                        DuoLog.Error($"Could not find apartment {apartmentNum + 1} (section {section} does not exist)");
                        return null;
                    }
                    if(EzThrottler.Throttle("EnterApartmentRool", 5000))
                    {
                        Callback.Fire(addon, true, 1, section);
                        return false;
                    }
                }
            }
        }
        return false;
    }

    public static unsafe bool ConfirmApartmentEnterYesno()
    {
        var addon = (AddonSelectYesno*)Utils.GetSpecificYesno(true, Lang.EnterApartmenr);
        if(addon != null && addon->YesButton->IsEnabled)
        {
            if(EzThrottler.Throttle($"ConfirmApartmentEnter", 5000))
            {
                new SelectYesnoMaster(addon).Yes();
                return true;
            }
        }
        return false;
    }



    public static WorldChangeAetheryte? DetermineGatewayAetheryte(ResidentialAetheryteKind targetZone)
    {
        if(targetZone == ResidentialAetheryteKind.Uldah) return WorldChangeAetheryte.Uldah;
        if(targetZone == ResidentialAetheryteKind.Gridania) return WorldChangeAetheryte.Gridania;
        if(targetZone == ResidentialAetheryteKind.Limsa) return WorldChangeAetheryte.Limsa;
        return null;
    }
}
