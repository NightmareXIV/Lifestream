using ECommons.Funding;
using ECommons.Reflection;

namespace Lifestream.GUI;

internal static unsafe class MainGui
{
    internal static void Draw()
    {
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("LifestreamTabs", PatreonBanner.Text,
            ("Address Book", TabAddressBook.Draw, null, true),
            ("House Registration", UIHouseReg.Draw, null, true),
            ("Custom Alias", TabCustomAlias.Draw, null, true),
            ("Settings", UISettings.Draw, null, true),
            ("Service accounts", UIServiceAccount.Draw, null, true),
            ("Debug", UIDebug.Draw, ImGuiColors.DalamudGrey3, true)
            );
    }
}
