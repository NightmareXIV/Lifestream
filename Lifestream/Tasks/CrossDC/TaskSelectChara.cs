using ECommons.ExcelServices;
using Lifestream.Tasks.Login;

namespace Lifestream.Tasks.CrossDC;

internal class TaskSelectChara
{
    internal static unsafe void Enqueue(string charaName, uint charaHomeWorld, uint currentWorld)
    {
        P.TaskManager.Enqueue(() => TaskChangeCharacter.SelectCharacter(charaName, ExcelWorldHelper.GetName(charaHomeWorld), ExcelWorldHelper.GetName(currentWorld)));
        P.TaskManager.Enqueue(TaskChangeCharacter.ConfirmLogin);
    }
}
