using ECommons.Funding;

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
            ("Utility", TabUtility.Draw, null, true),
            ("Settings", UISettings.Draw, null, true),
            ("Help", DrawHelp, null, true),
            ("Debug", UIDebug.Draw, ImGuiColors.DalamudGrey3, true)
            );
    }

    private static void DrawHelp()
    {
        ImGuiEx.TextWrapped(Lang.Help);
    }
}
