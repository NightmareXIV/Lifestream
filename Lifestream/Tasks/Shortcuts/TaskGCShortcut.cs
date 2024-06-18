using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lifestream.Enums;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrandCompany = ECommons.ExcelServices.GrandCompany;

namespace Lifestream.Tasks.Shortcuts;
public unsafe static class TaskGCShortcut
{
    public static readonly Dictionary<GrandCompany, Vector3> CompanyNPCPoints = new()
    {
        [GrandCompany.ImmortalFlames] = new(-140.6f, 4.1f, -105.6f),
        [GrandCompany.Maelstrom] = new(93.0f, 40.3f, 75.6f),
        [GrandCompany.TwinAdder] = new(-67.2f, -0.5f, -7.8f),
    };

    public static readonly Dictionary<GrandCompany, Vector3> CompanyChestPoints = new()
    {
        [GrandCompany.ImmortalFlames] = new(-148.1f, 4.1f, -93.0f),
        [GrandCompany.Maelstrom] = new(90.5f, 40.2f, 63.0f),
        [GrandCompany.TwinAdder] = new(-76.8f, -0.5f, -1.1f),
    };

    public static readonly Dictionary<GrandCompany, uint> CompanyTerritory = new()
    {
        [GrandCompany.ImmortalFlames] = MainCities.Uldah_Steps_of_Nald,
        [GrandCompany.Maelstrom] = MainCities.Limsa_Lominsa_Upper_Decks,
        [GrandCompany.TwinAdder] = MainCities.New_Gridania,
    };

    public static readonly Dictionary<GrandCompany, uint> CompanyAetheryte = new()
    {
        [GrandCompany.ImmortalFlames] = (uint)WorldChangeAetheryte.Uldah,
        [GrandCompany.Maelstrom] = (uint)WorldChangeAetheryte.Limsa,
        [GrandCompany.TwinAdder] = (uint)WorldChangeAetheryte.Gridania,
    };

    //21069	Maelstrom aetheryte ticket
    //21070	Twin Adder aetheryte ticket
    //21071	Immortal Flames aetheryte ticket
    public static readonly Dictionary<GrandCompany, uint> CompanyItem = new()
    {
        [GrandCompany.ImmortalFlames] = 21071,
        [GrandCompany.Maelstrom] = 21069,
        [GrandCompany.TwinAdder] = 21070,
    };

    public static void Enqueue(GrandCompany? companyNullable = null, bool isChest = false)
    {
        if (P.TaskManager.IsBusy)
        {
            DuoLog.Error($"Lifestream is busy, could not process request");
            return;
        }
        companyNullable ??= Player.GrandCompany;
        if(companyNullable == GrandCompany.Unemployed)
        {
            DuoLog.Error($"Grand company not specified and player is unemployed");
            return;
        }
        var company = companyNullable.Value;
        var point = (isChest ? CompanyChestPoints : CompanyNPCPoints)[company];
        if(Player.Territory == CompanyTerritory[company])
        {
            P.TaskManager.Enqueue(P.VnavmeshManager.IsReady);
            var task = P.VnavmeshManager.Pathfind(Player.Position, point, false);
            P.TaskManager.Enqueue(() =>
            {
                if (!task.IsCompleted) return false;
                var path = task.Result;
                if (Utils.CalculatePathDistance([.. path]) > 150f)
                {
                    EnqueueFromStart();
                }
                else
                {
                    P.TaskManager.Enqueue(TaskMoveToHouse.UseSprint);
                    P.TaskManager.Enqueue(() => P.FollowPath.Move([.. path], true));
                }
                return true;
            }, "Build path and check");
        }
        else
        {
            EnqueueFromStart();
        }

        void EnqueueFromStart()
        {
            if (Player.GrandCompany == company && InventoryManager.Instance()->GetInventoryItemCount(CompanyItem[company]) > 0)
            {
                P.TaskManager.Enqueue(() =>
                {
                    if (Player.IsAnimationLocked) return false;
                    if (EzThrottler.Throttle("GCUseTicket", 1000))
                    {
                        AgentInventoryContext.Instance()->UseItem(CompanyItem[company]);
                    }
                    if (Svc.Condition[ConditionFlag.Casting] || Player.Object.IsCasting) return true;
                    return false;
                });
                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
                P.TaskManager.Enqueue(() => Player.Interactable && IsScreenReady() && Player.Territory == CompanyTerritory[company], "WaitUntilPlayerInteractable", TaskSettings.Timeout2M);
                EnqueueNavigation();
            }
            else
            {
                if (company == GrandCompany.Maelstrom)
                {
                    if (Utils.GetReachableWorldChangeAetheryte() == null || Player.Territory != MainCities.Limsa_Lominsa_Lower_Decks)
                    {
                        TaskTpAndWaitForArrival.Enqueue(CompanyAetheryte[company]);
                    }
                    TaskTryTpToAethernetDestination.Enqueue(Svc.Data.GetExcelSheet<Aetheryte>().GetRow(41).AethernetName.Value.Name.ExtractText());
                    P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
                    P.TaskManager.Enqueue(() => Player.Interactable && IsScreenReady() && Player.Territory == MainCities.Limsa_Lominsa_Upper_Decks, "WaitUntilPlayerInteractable", TaskSettings.Timeout2M);
                }
                else
                {
                    TaskTpAndWaitForArrival.Enqueue(CompanyAetheryte[company]);
                }
                EnqueueNavigation();
            }
        }

        void EnqueueNavigation()
        {
            P.TaskManager.Enqueue(Utils.WaitForScreen);
            P.TaskManager.Enqueue(P.VnavmeshManager.IsReady);
            P.TaskManager.Enqueue(() =>
            {
                var task = P.VnavmeshManager.Pathfind(Player.Position, point, false);
                P.TaskManager.Enqueue(() =>
                {
                    if (!task.IsCompleted) return false;
                    var path = task.Result;
                    P.TaskManager.Enqueue(TaskMoveToHouse.UseSprint);
                    P.TaskManager.Enqueue(() => P.FollowPath.Move([.. path], true));
                    return true;
                }, "Build path");
            }, "Master navmesh task");
        }
    }

}
