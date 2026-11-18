using ECommons.Automation;
using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Game;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.CrossWorld;
using Lifestream.Tasks.Shortcuts;
using System;
using System.Collections.Generic;
using System.Text;
using Callback = ECommons.Automation.Callback;
using HouseEnterMode = Lifestream.Data.HouseEnterMode;

namespace Lifestream.Tasks.SameWorld;

public unsafe static class TaskThroughFriendTeleport
{
    public static EstateTeleportationList? LastEstateTeleportationList = null;
    public static bool RequestEstateTeleportationList = false;
    public static void Enqueue(string friendName, ulong cid, bool allowWorldTravel, HouseTeleportTarget? teleportType)
    {
        if(P.TaskManager.IsBusy)
        {
            DuoLog.Error($"Lifestream is busy");
            return;
        }
        if(!Player.Available) return;
        P.TaskManager.Enqueue(() =>
        {
            var canUseFriendList = ActionManager.Instance()->GetActionStatus(ActionType.MainCommand, 13) == 0;
            if(canUseFriendList)
            {
                var friend = InfoProxyFriendList.Instance()->GetEntryByContentId(cid);
                if(friend != null)
                {
                    if(friend->HomeWorld != Player.CurrentWorldId)
                    {
                        if(allowWorldTravel)
                        {
                            P.TaskManager.InsertStack($"{nameof(TaskThroughFriendTeleport)}: {nameof(Enqueue)}", () =>
                            {
                                TaskChangeWorld.Enqueue(ExcelWorldHelper.GetName(friend->HomeWorld));
                                Enqueue(friendName, cid, false, teleportType);
                            });
                            return true;
                        }
                        else
                        {
                            Notify.Error("Specified player is located in different world.");
                            return true;
                        }
                    }
                    else
                    {
                        RequestEstateTeleportationList = true;
                        LastEstateTeleportationList = null;
                        AgentFriendlist.Instance()->OpenFriendEstateTeleportation(cid);
                        P.TaskManager.Insert(() =>
                        {
                            if(LastEstateTeleportationList != null)
                            {
                                if(TryGetAddonByName<AtkUnitBase>("TeleportHousingFriend", out var addon) && addon->IsReady())
                                {
                                    void wait()
                                    {
                                        P.TaskManager.InsertStack($"{nameof(TaskThroughFriendTeleport)}: {nameof(Enqueue)}: {nameof(wait)}", () =>
                                        {
                                            P.TaskManager.Enqueue(() => !IsScreenReady(), "IsScreenNotReady");
                                            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable, "IsScreenReady and Interactable", new(timeLimitMS: 15.Minutes()));
                                        });
                                    }
                                    if(teleportType.EqualsAny(null, HouseTeleportTarget.Private_Estate_Hall) && (long)LastEstateTeleportationList.Value.PrivateHouseId.Id != -1)
                                    {
                                        Callback.Fire(addon, true, 1);
                                        Notify.Success($"Teleporting to Private house.");

                                        var data = Utils.GetCustomPathData(Utils.GetResidentialAetheryteByTerritoryType(LastEstateTeleportationList.Value.PrivateHouseId.TerritoryTypeId).Value, LastEstateTeleportationList.Value.PrivateHouseId.WardIndex, LastEstateTeleportationList.Value.PrivateHouseId.PlotIndex);
                                        PluginLog.Information($"Query path data for {Utils.GetResidentialAetheryteByTerritoryType(LastEstateTeleportationList.Value.PrivateHouseId.TerritoryTypeId).Value}, w{LastEstateTeleportationList.Value.PrivateHouseId.WardIndex}, p{LastEstateTeleportationList.Value.PrivateHouseId.PlotIndex}");
                                        HouseEnterMode mode = data?.GetHouseEnterMode() ?? HouseEnterMode.None;
                                        if(data != null)
                                        {
                                            if((mode == HouseEnterMode.Enter_house || mode == HouseEnterMode.Enter_workshop) && LastEstateTeleportationList.Value.PrivateFlag == 0)
                                            {
                                                mode = HouseEnterMode.Walk_to_door;
                                            }
                                            TaskPropertyShortcut.ExecuteTpAndPathfind(0, 0, data, mode, false);
                                        }

                                        wait();
                                        return true;
                                    }
                                    else if(teleportType.EqualsAny(null, HouseTeleportTarget.Apartment) && (long)LastEstateTeleportationList.Value.ApartmentHouseId.Id != -1)
                                    {
                                        Callback.Fire(addon, true, 2);
                                        if(LastEstateTeleportationList.Value.ApartmentFlag == 1)
                                        {
                                            Notify.Success($"Teleporting to Apartment.");
                                            P.TaskManager.InsertStack($"{nameof(TaskThroughFriendTeleport)}: {nameof(Enqueue)} (Apartment)", () =>
                                            {
                                                TaskApproachAndInteractWithApartmentEntrance.Enqueue(false);
                                                P.TaskManager.Enqueue(TaskTpAndGoToWard.SelectGoToSpecifiedApartment);
                                                P.TaskManager.Enqueue(() => TaskTpAndGoToWard.SelectApartment(LastEstateTeleportationList.Value.ApartmentHouseId.RoomNumber-1), $"SelectApartment {LastEstateTeleportationList.Value.ApartmentHouseId.RoomNumber-1}");
                                                if(!C.AddressApartmentNoEntry) P.TaskManager.Enqueue(TaskTpAndGoToWard.ConfirmApartmentEnterYesno);
                                            });
                                        }
                                        else
                                        {
                                            Notify.Warning("Teleporting to Apartment. Will not enter, as access has been denied.");
                                        }
                                        wait();
                                        return true;
                                    }
                                    else if(teleportType.EqualsAny(null, HouseTeleportTarget.Free_Company_Estate_Hall) && (long)LastEstateTeleportationList.Value.FreeCompanyHouseId.Id != -1)
                                    {
                                        Callback.Fire(addon, true, 0);
                                        Notify.Success($"Teleporting to Free company.");

                                        var data = Utils.GetCustomPathData(Utils.GetResidentialAetheryteByTerritoryType(LastEstateTeleportationList.Value.FreeCompanyHouseId.TerritoryTypeId).Value, LastEstateTeleportationList.Value.FreeCompanyHouseId.WardIndex, LastEstateTeleportationList.Value.FreeCompanyHouseId.PlotIndex);
                                        HouseEnterMode mode = data?.GetHouseEnterMode() ?? HouseEnterMode.None;
                                        if(data != null)
                                        {
                                            if((mode == HouseEnterMode.Enter_house || mode == HouseEnterMode.Enter_workshop) && LastEstateTeleportationList.Value.FreeCompanyFlag == 0)
                                            {
                                                mode = HouseEnterMode.Walk_to_door;
                                            }
                                            TaskPropertyShortcut.ExecuteTpAndPathfind(0, 0, data, mode, false);
                                        }

                                        wait();
                                        return true;
                                    }
                                    else
                                    {
                                        Callback.Fire(addon, true, -1);
                                        List<HouseTeleportTarget> avail = [];
                                        if((long)LastEstateTeleportationList.Value.ApartmentHouseId.Id != -1) avail.Add(HouseTeleportTarget.Apartment);
                                        if((long)LastEstateTeleportationList.Value.PrivateHouseId.Id != -1) avail.Add(HouseTeleportTarget.Private_Estate_Hall);
                                        if((long)LastEstateTeleportationList.Value.FreeCompanyHouseId.Id != -1) avail.Add(HouseTeleportTarget.Free_Company_Estate_Hall);
                                        if(teleportType == null || avail.Count == 0)
                                        {
                                            Notify.Error($"No friend's property found.");
                                        }
                                        else
                                        {
                                            Notify.Error($"You have requested to teleport to {teleportType.ToString().Replace("_", " ")}. Available teleports: {avail.Select(x => x.ToString().Replace("_", " ")).Print()}");
                                        }
                                        return true;
                                    }
                                }
                            }
                            return false;
                        });
                        return true;
                    }
                }
                else
                {
                    Notify.Error($"Could not find requested friend {friendName}");
                    return true;
                }
            }
            return false;
        });
    }
}
