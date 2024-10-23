using ECommons.GameHelpers;
using Lifestream.Data;
using Lumina.Excel.GeneratedSheets;
using ResidentialAetheryte = Lifestream.Systems.Residential.ResidentialAetheryte;

namespace Lifestream.Tasks.Utility;
public static class TaskGeneratePath
{
    public static void Enqueue(int plotNum, PlotInfo info)
    {
        P.TaskManager.Enqueue(() => !P.VnavmeshManager.PathfindInProgress());
        P.TaskManager.Enqueue(() =>
        {
            DuoLog.Information($"Pathfind begin for {plotNum + 1} from aetheryte {Svc.Data.GetExcelSheet<HousingAethernet>().GetRow(info.AethernetID).PlaceName.Value.Name}");
            var task = P.VnavmeshManager.Pathfind(Player.Object.Position, info.Front, false);
            P.TaskManager.InsertMulti(
                new(() => task.IsCompleted),
                new(() =>
                {
                    if(!task.IsCompletedSuccessfully)
                    {
                        DuoLog.Error($"-- Pathfind failed");
                        return null;
                    }
                    else
                    {
                        DuoLog.Information($"-- Success for {plotNum + 1}, distance={Utils.CalculatePathDistance([Player.Object.Position, .. task.Result])}");
                        info.Path = [.. task.Result];
                        Utils.SaveGeneratedHousingData();
                        return true;
                    }
                })
                );
        });
    }

    public static void EnqueueValidate(int plotNum, PlotInfo info, ResidentialAetheryte aetheryte)
    {
        P.TaskManager.Enqueue(() => !P.VnavmeshManager.PathfindInProgress());
        P.TaskManager.Enqueue(() =>
        {
            var task = P.VnavmeshManager.Pathfind(Player.Object.Position, info.Front, false);
            P.TaskManager.InsertMulti(
                new(() => task.IsCompleted),
                new(() =>
                {
                    if(!task.IsCompletedSuccessfully)
                    {
                        DuoLog.Error($"-- Pathfind failed");
                        return null;
                    }
                    else
                    {
                        var distanceNew = Utils.CalculatePathDistance([.. task.Result]);
                        var distanceOld = Utils.CalculatePathDistance([.. info.Path]);
                        if(distanceNew < distanceOld)
                        {
                            DuoLog.Warning($"-- For plot {plotNum + 1}, old distance was {distanceOld} > {distanceNew} new distance, replacing path and aetheryte from {Svc.Data.GetExcelSheet<HousingAethernet>().GetRow(info.AethernetID)?.PlaceName?.Value?.Name}, please double-check path.");
                            info.Path = [.. task.Result];
                            info.AethernetID = aetheryte.ID;
                            Utils.SaveGeneratedHousingData();
                        }
                        return true;
                    }
                })
                );
        });
    }
}
