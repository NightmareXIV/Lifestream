using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.CrossDC
{
    internal class TaskSelectChara
    {
        internal unsafe static void Enqueue(string charaName, uint charaWorld)
        {
            P.TaskManager.Enqueue(() => TryGetAddonByName<AtkUnitBase>("_CharaSelectListMenu", out var addon) && IsAddonReady(addon), 60.Minutes(), "Wait until chara list available");
            P.TaskManager.Enqueue(() => DCChange.SelectCharacter(charaName, charaWorld), nameof(DCChange.SelectCharacter));
            P.TaskManager.Enqueue(DCChange.SelectYesLogin, 60.Minutes());
        }
    }
}
