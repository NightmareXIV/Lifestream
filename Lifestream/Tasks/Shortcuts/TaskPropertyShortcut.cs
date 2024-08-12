using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lifestream.Data;
using Lifestream.Schedulers;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.SameWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.Shortcuts;
public unsafe static class TaskPropertyShortcut
{
    public static void Enqueue(PropertyType propertyType = PropertyType.Auto, HouseEnterMode? mode = null)
    {
        if(P.TaskManager.IsBusy)
        {
            DuoLog.Error($"Lifestream is busy");
            return;
        }
        if(!Player.Available) return;
        if(!Player.IsInHomeWorld)
        {
            P.TPAndChangeWorld(Player.HomeWorld, !Player.IsInHomeDC, null, true, null, false, false);
        }
        P.TaskManager.Enqueue(() => Player.Interactable && Player.IsInHomeWorld && IsScreenReady());
        P.TaskManager.Enqueue(() =>
        {
            if(propertyType == PropertyType.Auto)
            {
                if(GetPrivateHouseAetheryteID() != 0)
                {
                    ExecuteTpAndPathfind(GetPrivateHouseAetheryteID(), Utils.GetPrivatePathData(), mode);
                }
                else if(GetFreeCompanyAetheryteID() != 0)
                {
                    P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination(GetFreeCompanyAetheryteID()));
                }
                else if(GetApartmentAetheryteID().ID != 0)
                {
                    ExecuteTpAndPathfind(GetFreeCompanyAetheryteID(), Utils.GetFCPathData(), mode);
                }
                else
                {
                    DuoLog.Error($"Could not find private or free company house or apartment");
                }
            }
            else if(propertyType == PropertyType.Home)
            {
                if(GetPrivateHouseAetheryteID() != 0)
                {
                    ExecuteTpAndPathfind(GetPrivateHouseAetheryteID(), Utils.GetPrivatePathData(), mode);
                }
                else
                {
                    DuoLog.Error("Could not find private house");
                }
            }
            else if(propertyType == PropertyType.FC)
            {
                if(GetFreeCompanyAetheryteID() != 0)
                {
                    ExecuteTpAndPathfind(GetFreeCompanyAetheryteID(), Utils.GetFCPathData(), mode);
                }
                else
                {
                    DuoLog.Error("Could not find free company house");
                }
            }
            else if(propertyType == PropertyType.Apartment)
            {
                if(GetApartmentAetheryteID().ID != 0)
                {
                    EnqueueGoToMyApartment();
                }
                else
                {
                    DuoLog.Error("Could not find apartment");
                }
            }
        }, "ReturnToHomeTask");
    }

    private static void ExecuteTpAndPathfind(uint id, HousePathData data, HouseEnterMode? mode = null)
    {
        mode ??= data?.GetHouseEnterMode() ?? HouseEnterMode.None;
        PluginLog.Information($"id={id}, data={data}, mode={mode}, cnt={data?.PathToEntrance.Count}");
        P.TaskManager.BeginStack();
        P.TaskManager.Enqueue(() => WorldChange.ExecuteTPToAethernetDestination(id));
        P.TaskManager.Enqueue(() => !IsScreenReady());
        P.TaskManager.Enqueue(() => IsScreenReady()  && Player.Interactable);
        if(data != null && data.PathToEntrance.Count != 0 && mode.EqualsAny(HouseEnterMode.Walk_to_door, HouseEnterMode.Enter_house))
        {
            P.TaskManager.Enqueue(() =>
            {
                if(Vector3.Distance(Player.Position, Utils.GetPlotEntrance(data.ResidentialDistrict.GetResidentialTerritory(), data.Plot).Value) > 10f)
                {
                    P.IPCProvider.OnHouseEnterError();
                    throw new InvalidOperationException("Could not validate your position. Check if your house registration is correct and if it is, please report this error to developer");
                }
            });
            P.TaskManager.Enqueue(() => P.FollowPath.Move(data.PathToEntrance, true));
            P.TaskManager.Enqueue(() => P.FollowPath.Waypoints.Count == 0);
            if(mode == HouseEnterMode.Enter_house)
            {
                P.TaskManager.Enqueue(() =>
                {
                    var e = Utils.GetNearestEntrance(out var dist);
                    if(e != null && dist < 10f)
                    {
                        if(e.IsTarget())
                        {
                            if(EzThrottler.Throttle("InteractWithEntrance", 2000))
                            {
                                TargetSystem.Instance()->InteractWithObject(e.Struct(), false);
                                return true;
                            }
                        }
                        else
                        {
                            Svc.Targets.Target = e;
                            EzThrottler.Throttle("InteractWithEntrance", 200, true);
                            return false;
                        }
                    }
                    return false;
                });
                P.TaskManager.Enqueue(ConfirmHouseEntrance);
            }
        }
        P.TaskManager.InsertStack();
    }

    private static void EnqueueGoToMyApartment()
    {
        var a = GetApartmentAetheryteID();
        P.TaskManager.BeginStack();
        P.TaskManager.Enqueue(() => WorldChange.ExecuteTPToAethernetDestination(a.ID, a.Sub));
        if(P.Config.EnterMyApartment)
        {
            TaskApproachAndInteractWithApartmentEntrance.Enqueue();
            P.TaskManager.Enqueue(TaskApproachAndInteractWithApartmentEntrance.GoToMyApartment);
        }
        P.TaskManager.InsertStack();
    }

    private static uint GetPrivateHouseAetheryteID()
    {
        foreach(var x in Svc.AetheryteList)
        {
            if(!x.IsApartment && !x.IsSharedHouse && x.AetheryteId.EqualsAny<uint>(59, 60, 61, 97, 165))
            {
                return x.AetheryteId;
            }
        }
        return 0;
    }

    private static (uint ID, uint Sub) GetApartmentAetheryteID()
    {
        foreach(var x in Svc.AetheryteList)
        {
            if(x.IsApartment && !x.IsSharedHouse && x.AetheryteId.EqualsAny<uint>(59, 60, 61, 97, 165))
            {
                return (x.AetheryteId, x.SubIndex);
            }
        }
        return (0, 0);
    }

    private static uint GetFreeCompanyAetheryteID()
    {
        foreach(var x in Svc.AetheryteList)
        {
            if(!x.IsApartment && !x.IsSharedHouse && x.AetheryteId.EqualsAny<uint>(56, 57, 58, 96, 164))
            {
                return x.AetheryteId;
            }
        }
        return 0;
    }

    private static bool ConfirmHouseEntrance()
    {
        var addon = Utils.GetSpecificYesno(Lang.ConfirmHouseEntrance);
        if(addon != null)
        {
            if(IsAddonReady(addon) && EzThrottler.Throttle("SelectYesno"))
            {
                new AddonMaster.SelectYesno((nint)addon).Yes();
                return true;
            }
        }
        return false;
    }

    public enum PropertyType
    {
        Auto, Home, FC, Apartment
    }
}
