using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lifestream.Data;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.Sheets;

namespace Lifestream.Tasks.Shortcuts;
public static unsafe class TaskMBShortcut
{
    public static void Enqueue()
    {
        StaticAlias.UldahMarketboard.Enqueue();
    }
}
