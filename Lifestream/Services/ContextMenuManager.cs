using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using ECommons.ExcelServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Services;
public class ContextMenuManager : IDisposable
{
    private ContextMenuManager()
    {
        Svc.ContextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
    }

    public void Dispose()
    {
        Svc.ContextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
    }

    private void ContextMenu_OnMenuOpened(IMenuOpenedArgs args)
    {
        if(TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m))
        {
            if(args.Target is MenuTargetDefault target && m.Characters.TryGetFirst(x => x.IsSelected, out var chara))
            {
                args.AddMenuItem(ConstructMenuItemFor(chara.Name, chara.HomeWorld, chara.CurrentWorld));
            }
        }
    }

    private MenuItem ConstructMenuItemFor(string name, uint homeWorld, uint currentWorld)
    {
        var ret = new MenuItem()
        {
            Name = "Lifestream",
            Prefix = (SeIconChar)'',
            OnClicked = (args) =>
            {
                //P.CharaSelectOverlay.Open(name, homeWorld);
                args.OpenSubmenu(new List<MenuItem>() { new() { Name = "Data Center 1", OnClicked = (x) => x.OpenSubmenu(ExcelWorldHelper.GetPublicWorlds(1).Select(x => new MenuItem() { Name = x.Name.ToString() }).ToList()) } }); 
            }
        };
        return ret;
    }
}
