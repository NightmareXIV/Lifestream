using ECommons.Funding;
using ECommons.Reflection;

namespace Lifestream.GUI;

internal static unsafe class MainGui
{
    internal static void Draw()
    {
        /*if(!Utils.IsTeleporterInstalled())
        {
            if(ImGui.BeginTable("Notify", 1, ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, EColor.RedDark.ToUint());
                ImGui.TableNextColumn();
                ImGuiEx.TextWrapped(EColor.White, $"You do not have \"Teleporter\" plugin installed or enabled. For correct Lifestream plugin operation, you need to install \"Teleporter\" plugin from official Dalamud repo. Click here to open plugin installer.");
                if (ImGuiEx.HoveredAndClicked())
                {
                    Svc.PluginInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.AllPlugins, "TeleporterPlugin");
                }
                ImGui.EndTable();
            }
        }*/
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("LifestreamTabs", PatreonBanner.Text,
            ("Address Book", TabAddressBook.Draw, null, true),
            ("House Registration", UIHouseReg.Draw, null, true),
            ("Settings", UISettings.Draw, null, true),
            ("Service accounts", UIServiceAccount.Draw, null, true),
            ("Debug", UIDebug.Draw, ImGuiColors.DalamudGrey3, true)
            );
    }
}
