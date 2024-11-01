using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.NeoTaskManager.Tasks;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lifestream.Data;
using Lifestream.Tasks.SameWorld;
using Lifestream.Tasks.Utility;

namespace Lifestream.Tasks.Shortcuts;
public static unsafe class TaskISShortcut
{
    public enum IslandNPC : uint
    {
        Baldin = 1043621,
        TactfulTaskmaster = 1043078,
        ExcitableExplorer = 1043081,
        EnterprisingExporter = 1043464,
        HorrendousHoarder = 1043463,
        FelicitousFurball = 1043473,
        CreatureComforter = 1043466,
        ProduceProducer = 1043465,
    }

    public static class IslandTerritories
    {
        public const uint Moraby = 135;
        public const uint Island = 1055;
    }

    public static readonly Dictionary<IslandNPC, Vector3> NPCPoints = new()
    {
        [IslandNPC.Baldin] = new(173.05f, 14.10f, 668.42f),
        [IslandNPC.TactfulTaskmaster] = new(-278.37f, 40.00f, 229.82f),
        [IslandNPC.ExcitableExplorer] = new(-265.44f, 40.00f, 234.52f),
        [IslandNPC.EnterprisingExporter] = new(-265.72f, 41.01f, 210.44f),
        [IslandNPC.HorrendousHoarder] = new(-265.72f, 41.01f, 210.44f),
        [IslandNPC.FelicitousFurball] = new(-272.06f, 41.00f, 212.32f),
        [IslandNPC.CreatureComforter] = new(-268.60f, 55.20f, 134.44f),
        [IslandNPC.ProduceProducer] = new(-258.16f, 55.20f, 135.01f),
    };

    public static void Enqueue(IslandNPC? npcNullable = null, bool returnHome = true)
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
        if(returnHome)
        {
            if(!Player.IsInHomeWorld)
            {
                P.TPAndChangeWorld(Player.HomeWorld, !Player.IsInHomeDC, null, true, null, false, false);
            }
            P.TaskManager.Enqueue(() => Player.Interactable && Player.IsInHomeWorld && IsScreenReady());
        }
        var point = npcNullable == null ? NPCPoints[IslandNPC.TactfulTaskmaster] : NPCPoints[npcNullable.Value];
        P.TaskManager.Enqueue(() =>
        {
            if(Player.Territory == IslandTerritories.Island)
            {
                EnqueueNavToNPC(point);
            }
            else
            {
                TravelToIsland();
            }
        });

        IGameObject baldin() => Svc.Objects.FirstOrDefault(x => x.DataId == (uint)IslandNPC.Baldin);

        void TravelToIsland()
        {
            StaticAlias.IslandSanctuary.Enqueue(true);
            P.TaskManager.EnqueueTask(NeoTasks.ApproachObjectViaAutomove(baldin, 6.5f));
            P.TaskManager.EnqueueTask(NeoTasks.InteractWithObject(baldin));
            P.TaskManager.Enqueue(TalkWithBaldin);
            P.TaskManager.Enqueue(ConfirmIslandTravel);
            P.TaskManager.Enqueue(() => Player.Interactable && IsScreenReady() && Player.Territory == IslandTerritories.Island, "WaitUntilPlayerInteractableOnIsland", TaskSettings.Timeout2M);
            P.TaskManager.Enqueue(() => EnqueueNavToNPC(point));
        }

        bool TalkWithBaldin()
        {
            if(TryGetAddonMaster<AddonMaster.Talk>(out var talk))
                talk.Click();
            return Utils.TrySelectSpecificEntry(Lang.TravelToMyIsland, () => EzThrottler.Throttle(nameof(TalkWithBaldin)));
        }

        bool ConfirmIslandTravel()
        {
            var addon = (AddonSelectYesno*)Utils.GetSpecificYesno(true, Lang.TravelToYourIsland);
            if(addon != null && addon->YesButton->IsEnabled)
            {
                if(EzThrottler.Throttle(nameof(ConfirmIslandTravel), 5000))
                {
                    new AddonMaster.SelectYesno(addon).Yes();
                    return true;
                }
            }
            return false;
        }

        void EnqueueNavToNPC(Vector3 point)
        {
            P.TaskManager.Enqueue(Utils.WaitForScreen);
            P.TaskManager.Enqueue(() =>
            {
                if(!(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "vnavmesh" && x.IsLoaded)))
                {
                    PluginLog.Warning($"Navmesh not found, will not continue navigation");
                    return null;
                }
                else
                {
                    return true;
                }
            });
            P.TaskManager.Enqueue(P.VnavmeshManager.IsReady);
            P.TaskManager.Enqueue(() =>
            {
                var task = P.VnavmeshManager.Pathfind(Player.Position, point, false);
                P.TaskManager.Enqueue(() =>
                {
                    if(!task.IsCompleted) return false;
                    var path = task.Result;
                    P.TaskManager.Enqueue(TaskMoveToHouse.UseSprint);
                    P.TaskManager.Enqueue(() => P.FollowPath.Move([.. path], true));
                    return true;
                }, "Build path");
            }, "Master navmesh task");
        }
    }
}
