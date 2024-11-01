using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;
using Lumina.Excel.GeneratedSheets;
using GrandCompany = ECommons.ExcelServices.GrandCompany;

namespace Lifestream.Tasks.Shortcuts;
public static unsafe class TaskGCShortcut
{
    public static readonly Dictionary<GrandCompany, Vector3[]> CompanyNPCPoints = new()
    {
        [GrandCompany.ImmortalFlames] = [new(-140.6f, 4.1f, -105.6f)],
        [GrandCompany.Maelstrom] = [new(93.0f, 40.3f, 75.6f)],
        [GrandCompany.TwinAdder] = [new(-67.2f, -0.5f, -7.8f)],
    };

    public static readonly Dictionary<GrandCompany, Vector3[]> CompanyChestPoints = new()
    {
        [GrandCompany.ImmortalFlames] = [new(-132.9f, 4.1f, -96.2f), new(-148.1f, 4.1f, -93.0f)],
        [GrandCompany.Maelstrom] = [new(90.4f, 40.2f, 65.1f)],
        [GrandCompany.TwinAdder] = [new(-76.8f, -0.5f, -1.1f)],
    };

    public static Dictionary<GrandCompany, CustomAlias> CompanyNPCCommands => new()
    {
        [GrandCompany.ImmortalFlames] = StaticAlias.UldahGC,
        [GrandCompany.Maelstrom] = StaticAlias.LimsaGC,
        [GrandCompany.TwinAdder] = StaticAlias.GridaniaGC,
    };

    public static Dictionary<GrandCompany, CustomAlias> CompanyChestCommands => new()
    {
        [GrandCompany.ImmortalFlames] = StaticAlias.UldahGCC,
        [GrandCompany.Maelstrom] = StaticAlias.LimsaGCC,
        [GrandCompany.TwinAdder] = StaticAlias.GridaniaGCC,
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

    public static void Enqueue(GrandCompany? companyNullable = null, bool isChest = false, bool returnHome = false, bool fcgc = false)
    {
        if(P.TaskManager.IsBusy)
        {
            DuoLog.Error($"Lifestream is busy, could not process request");
            return;
        }
        if(!Player.Available)
        {
            DuoLog.Error("Player not available");
            return;
        }
        companyNullable ??= fcgc ? (GrandCompany)InfoProxyFreeCompany.Instance()->GrandCompany : Player.GrandCompany;
        if(companyNullable == GrandCompany.Unemployed)
        {
            if(Svc.AetheryteList.Any(x => x.AetheryteId == (int)WorldChangeAetheryte.Uldah))
            {
                DuoLog.Warning($"Grand company not specified and player is unemployed. Teleporting to Immortal Flames.");
                companyNullable = GrandCompany.ImmortalFlames;
            }
            else if(Svc.AetheryteList.Any(x => x.AetheryteId == (int)WorldChangeAetheryte.Gridania))
            {
                DuoLog.Warning($"Grand company not specified and player is unemployed. Teleporting to Twin Adder.");
                companyNullable = GrandCompany.TwinAdder;
            }
            else if(Svc.AetheryteList.Any(x => x.AetheryteId == (int)WorldChangeAetheryte.Limsa))
            {
                DuoLog.Warning($"Grand company not specified and player is unemployed. Teleporting to Maelstrom.");
                companyNullable = GrandCompany.Maelstrom;
            }
            else
            {
                DuoLog.Error("Grand company not specified, player is unemployed and can not teleport to any destination.");
                return;
            }
        }
        if(returnHome)
        {
            if(!Player.IsInHomeWorld)
            {
                P.TPAndChangeWorld(Player.HomeWorld, !Player.IsInHomeDC, null, true, null, false, false);
            }
            P.TaskManager.Enqueue(() => Player.Interactable && Player.IsInHomeWorld && IsScreenReady());
        }
        var company = companyNullable.Value;
        var point = (isChest ? CompanyChestPoints : CompanyNPCPoints)[company];
        var moveCommand = (isChest ? CompanyChestCommands : CompanyNPCCommands)[company];
        P.TaskManager.Enqueue(EnqueueFromStart);

        void EnqueueFromStart()
        {
            if(Player.GrandCompany == company && InventoryManager.Instance()->GetInventoryItemCount(CompanyItem[company]) > 0)
            {
                P.TaskManager.Enqueue(() =>
                {
                    if(Player.IsAnimationLocked) return false;
                    if(EzThrottler.Throttle("GCUseTicket", 1000))
                    {
                        AgentInventoryContext.Instance()->UseItem(CompanyItem[company]);
                    }
                    if(Svc.Condition[ConditionFlag.Casting] || Player.Object.IsCasting) return true;
                    return false;
                });
                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51], "WaitUntilBetweenAreas");
                P.TaskManager.Enqueue(() => Player.Interactable && IsScreenReady() && Player.Territory == CompanyTerritory[company], "WaitUntilPlayerInteractable", TaskSettings.Timeout2M);
                P.TaskManager.Enqueue(Utils.WaitForScreen);
                P.TaskManager.Enqueue(TaskMoveToHouse.UseSprint);
                P.TaskManager.Enqueue(() => P.FollowPath.Move([.. point], true));
            }
            else
            {
                moveCommand.Enqueue(true);
            }
        }
    }

}
