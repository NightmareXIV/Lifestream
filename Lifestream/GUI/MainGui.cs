namespace Lifestream.GUI;

internal unsafe static class MainGui
{
    internal static void Draw()
    {
        KoFiButton.DrawRight();
        ImGuiEx.EzTabBar("LifestreamTabs",
            ("Settings", UISettings.Draw, null, true),
            ("Service accounts", UIServiceAccount.Draw, null, true),
            InternalLog.ImGuiTab(),
            ("Debug", UIDebug.Draw, ImGuiColors.DalamudGrey3, true)

            );
    }
}
