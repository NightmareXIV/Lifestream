using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks
{
    internal class TaskSelectChara
    {
        internal static void Enqueue(string charaName)
        {
            P.TaskManager.Enqueue(() => DCChange.SelectCharacter(charaName));
            P.TaskManager.Enqueue(DCChange.SelectYesLogin, 1.Minutes());
        }
    }
}
