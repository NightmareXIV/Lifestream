using ECommons.Funding;
using ECommons.SimpleGui;

namespace Lifestream.GUI;

internal class MainGui : ConfigWindow
{
    private TitleBarButton CensorButton;
    public MainGui()
    {
        CensorButton = new()
        {
            Icon = !C.CensorConfig.Enabled ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash,
            Click = x =>
            {
                if(x == ImGuiMouseButton.Left)
                {
                    C.CensorConfig.Enabled = !C.CensorConfig.Enabled;
                }

                CensorButton.Icon = !C.CensorConfig.Enabled ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
            },
            IconOffset = new(1, 1),
            ShowTooltip = () => ImGuiEx.Tooltip("Censor character names.")
        };
        TitleBarButtons.Add(CensorButton);
    }

    public override void Draw()
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
