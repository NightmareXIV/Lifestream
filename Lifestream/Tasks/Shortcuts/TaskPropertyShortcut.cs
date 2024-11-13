﻿using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lifestream.Data;
using Lifestream.Schedulers;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;
using Lumina.Excel.Sheets;

namespace Lifestream.Tasks.Shortcuts;
public static unsafe class TaskPropertyShortcut
{
    public static readonly SortedDictionary<uint, (uint Aethernet, Vector3[] Path)> InnData = new()
    {
        [1185] = (220, [new(-161.9f, -15.0f, 205.0f)]), //tul
        [MainCities.Old_Sharlayan] = (185, [new(-89.6f, 1.3f, 25.7f), new(-99.5f, 3.9f, 5.2f)]),
        [MainCities.The_Crystarium] = (152, [new(36.4f, 0.0f, 219.9f), new(47.1f, 1.7f, 223.5f), new(62.1f, 1.7f, 245.5f)]),
        [MainCities.Kugane] = (116, [new(-79.3f, 18.0f, -171.9f), new(-86.3f, 18.1f, -182.9f), new(-86.3f, 19.0f, -196.9f)]),
        [MainCities.Foundation] = (80, [new(84.2f, 24.0f, 20.0f), new(84.3f, 24.0f, 27.3f), new(78.4f, 24.0f, 30.4f), new(79.6f, 19.5f, 42.3f), new(92.0f, 15.0f, 41.9f), new(87.3f, 15.0f, 35.0f)]),
        [MainCities.New_Gridania] = (94, [new(40.0f, -18.8f, 102.8f), new(40.1f, -10.4f, 122.5f), new(35.0f, -8.2f, 128.3f), new(27.3f, -8.2f, 125.2f), new(27.9f, -8.0f, 100.4f)]),
        [MainCities.Uldah_Steps_of_Nald] = (33, [new(53.7f, 4.0f, -126.0f), new(44.3f, 8.0f, -122.3f), new(33.7f, 8.0f, -122.1f), new(30.4f, 8.0f, -114.4f), new(42.7f, 8.0f, -98.8f), new(31.5f, 7.0f, -82.0f),]),
        [MainCities.Limsa_Lominsa_Lower_Decks] = (41, [new(0.6f, 40.0f, 72.1f), new(1.6f, 39.5f, 16.5f), new(11.0f, 40.0f, 13.8f)])
    };

    public static uint[] InnNpc = [1000102, 1000974, 1001976, 1011193, 1018981, 1048375, 1037293, 1027231];

    public static void Enqueue(PropertyType propertyType = PropertyType.Auto, HouseEnterMode? mode = null, int? innIndex = null, bool? enterApartment = null, bool useSameWorld = false)
    {
        if(P.TaskManager.IsBusy)
        {
            DuoLog.Error($"Lifestream is busy");
            return;
        }
        if(!Player.Available) return;
        if(!useSameWorld)
        {
            if(!Player.IsInHomeWorld)
            {
                P.TPAndChangeWorld(Player.HomeWorld, !Player.IsInHomeDC, null, true, null, false, false);
            }
            P.TaskManager.Enqueue(() => Player.Interactable && Player.IsInHomeWorld && IsScreenReady());
        }
        P.TaskManager.Enqueue(() =>
        {
            if(propertyType == PropertyType.Auto)
            {
                foreach(var x in P.Config.PropertyPrio)
                {
                    if(x.Enabled)
                    {
                        if(ExecuteByPropertyType(x.Type, mode, innIndex, enterApartment)) break;
                    }
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
                    EnqueueGoToMyApartment(enterApartment);
                }
                else
                {
                    DuoLog.Error("Could not find apartment");
                }
            }
            else if(propertyType == PropertyType.Inn)
            {
                EnqueueGoToInn(innIndex);
            }
        }, "ReturnToHomeTask");
    }

    private static bool ExecuteByPropertyType(PropertyType type, HouseEnterMode? mode, int? innIndex, bool? enterApartment)
    {
        if(type == PropertyType.Home && GetPrivateHouseAetheryteID() != 0)
        {
            ExecuteTpAndPathfind(GetPrivateHouseAetheryteID(), Utils.GetPrivatePathData(), mode);
            return true;
        }
        else if(type == PropertyType.FC && GetFreeCompanyAetheryteID() != 0)
        {
            ExecuteTpAndPathfind(GetFreeCompanyAetheryteID(), Utils.GetFCPathData(), mode);
            return true;
        }
        else if(type == PropertyType.Apartment && GetApartmentAetheryteID().ID != 0)
        {
            EnqueueGoToMyApartment(enterApartment);
            return true;
        }
        else if(type == PropertyType.Inn)
        {
            EnqueueGoToInn(innIndex);
            return true;
        }
        return false;
    }

    private static void ExecuteTpAndPathfind(uint id, HousePathData data, HouseEnterMode? mode = null)
    {
        mode ??= data?.GetHouseEnterMode() ?? HouseEnterMode.None;
        PluginLog.Information($"id={id}, data={data}, mode={mode}, cnt={data?.PathToEntrance.Count}");
        P.TaskManager.BeginStack();
        P.TaskManager.Enqueue(() => WorldChange.ExecuteTPToAethernetDestination(id));
        P.TaskManager.Enqueue(() => !IsScreenReady());
        P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
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
                    if(!Utils.DismountIfNeeded()) return false;
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

    public static void EnqueueGoToInn(int? innIndex)
    {
        P.TaskManager.BeginStack();
        try
        {
            var id = innIndex == null ? GetInnTerritoryId() : InnData.Keys.ElementAt(innIndex.Value);
            PluginLog.Debug($"Inn territory: {ExcelTerritoryHelper.GetName(id)}");
            var data = InnData[id];
            var aetheryte = Svc.Data.GetExcelSheet<Aetheryte>().First(x => x.IsAetheryte && x.Territory.RowId == id);
            if((P.ActiveAetheryte == null || P.ActiveAetheryte.Value.ID != aetheryte.RowId) && (Utils.GetReachableMasterAetheryte() == null || id != Player.Territory))
            {
                P.TaskManager.Enqueue(() => WorldChange.ExecuteTPToAethernetDestination(aetheryte.RowId, 0));
                P.TaskManager.Enqueue(() => !IsScreenReady());
                P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            }
            TaskApproachAetheryteIfNeeded.Enqueue();
            var aethernetDest = Svc.Data.GetExcelSheet<Aetheryte>().GetRow(data.Aethernet).AethernetName.Value.Name.ExtractText();
            PluginLog.Debug($"Inn aethernet destination: {aethernetDest} at {aetheryte.AethernetName.Value.Name}");
            TaskTryTpToAethernetDestination.Enqueue(aethernetDest);
            P.TaskManager.Enqueue(() => !IsScreenReady());
            P.TaskManager.Enqueue(() => IsScreenReady() && Player.Interactable);
            P.TaskManager.Enqueue(TaskMoveToHouse.UseSprint);
            P.TaskManager.Enqueue(() => P.FollowPath.Move([.. data.Path], true));
            P.TaskManager.Enqueue(() => P.FollowPath.Waypoints.Count == 0);
            P.TaskManager.Enqueue(() =>
            {
                var obj = Svc.Objects.FirstOrDefault(x => x.DataId.EqualsAny(InnNpc) && x.ObjectKind == ObjectKind.EventNpc && x.IsTargetable && Vector3.Distance(x.Position, Player.Position) < 10f);
                if(obj == null) return false;
                if(!Utils.DismountIfNeeded()) return false;
                if(obj.IsTarget())
                {
                    if(EzThrottler.Throttle("InteractInnNpc", 1000))
                    {
                        TargetSystem.Instance()->InteractWithObject(obj.Struct(), false);
                        return true;
                    }
                }
                else
                {
                    if(EzThrottler.Throttle("Settarget"))
                    {
                        Svc.Targets.Target = obj;
                    }
                }
                return false;
            });
            P.TaskManager.Enqueue(() =>
            {
                if(TryGetAddonMaster<AddonMaster.Talk>(out var talk))
                {
                    talk.Click();
                }
                var obj = Svc.Objects.FirstOrDefault(x => x.DataId.EqualsAny(InnNpc) && x.ObjectKind == ObjectKind.EventNpc && x.IsTargetable && Vector3.Distance(x.Position, Player.Position) < 10f);
                if(obj == null) return false;
                if(obj.IsTarget() && TryGetAddonMaster<AddonMaster.SelectString>(out var m))
                {
                    if(m.Entries.Length > 2 && EzThrottler.Throttle("SelectRetireInn", 5000))
                    {
                        m.Entries[0].Select();
                        return true;
                    }
                }
                return false;
            });
            P.TaskManager.Enqueue(() =>
            {
                if(!IsScreenReady()) return true;
                if(TryGetAddonMaster<AddonMaster.Talk>(out var talk))
                {
                    talk.Click();
                }
                return false;
            });

            P.TaskManager.EnqueueStack();
        }
        catch(Exception ex)
        {
            ex.Log();
            P.TaskManager.DiscardStack();
        }
    }

    private static void EnqueueGoToMyApartment(bool? enterApartment)
    {
        enterApartment ??= P.Config.EnterMyApartment;
        var a = GetApartmentAetheryteID();
        var nextToMyApt = AgentHUD.Instance()->MapMarkers.Any(x => x.IconId.EqualsAny(60790u, 60792u) && Vector3.Distance(Player.Position, new(x.X, x.Y, x.Z)) < 50f) && Svc.Objects.Any(x => x.DataId == 2007402 && Vector3.Distance(x.Position, Player.Position) < 20f);
        P.TaskManager.BeginStack();
        if(!nextToMyApt)
        {
            P.TaskManager.Enqueue(() => WorldChange.ExecuteTPToAethernetDestination(a.ID, a.Sub));
        }
        if(enterApartment == true)
        {
            TaskApproachAndInteractWithApartmentEntrance.Enqueue(!nextToMyApt);
            P.TaskManager.Enqueue(TaskApproachAndInteractWithApartmentEntrance.GoToMyApartment);
        }
        P.TaskManager.InsertStack();
    }

    public static uint GetPrivateHouseAetheryteID()
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

    public static (uint ID, uint Sub) GetApartmentAetheryteID()
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

    public static uint GetFreeCompanyAetheryteID()
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

    private static uint GetInnTerritoryId()
    {
        if(P.Config.PreferredInn != 0)
        {
            var aetheryte = Svc.Data.GetExcelSheet<Aetheryte>().FirstOrNull(x => x.IsAetheryte && x.Territory.RowId == P.Config.PreferredInn);
            if(aetheryte != null && Svc.AetheryteList.Any(a => a.AetheryteId == aetheryte?.RowId))
            {
                return aetheryte.Value.Territory.RowId;
            }
        }
        return P.Config.WorldChangeAetheryte.GetTerritory();
    }

    public enum PropertyType
    {
        Auto, Home, FC, Apartment, Inn
    }
}
