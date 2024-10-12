using ECommons.ExcelServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Schedulers;
using Lifestream.Tasks.Login;

namespace Lifestream.Tasks.CrossDC;

internal class TaskSelectChara
{
    internal static unsafe void Enqueue(string charaName, uint charaWorld)
    {
        P.TaskManager.Enqueue(() => TaskChangeCharacter.SelectCharacter(charaName, ExcelWorldHelper.GetName(charaWorld)));
        P.TaskManager.Enqueue(TaskChangeCharacter.ConfirmLogin);
    }
}
