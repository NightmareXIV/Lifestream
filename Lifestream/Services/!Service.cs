using Lifestream.Game;
using Lifestream.GUI;
using Lifestream.GUI.Windows;
using Lifestream.IPC;
using Lifestream.Systems.Custom;
using Lifestream.Systems.Legacy;
using Lifestream.Systems.Residential;
using static ECommons.Singletons.SingletonServiceManager;

namespace Lifestream.Services;

public static class Service
{
    [Priority(-1)]
    public static class Data
    {
        public static DataStore DataStore { get; set; }
        public static ResidentialAethernet ResidentialAethernet { get; private set; }
        public static CustomAethernet CustomAethernet { get; private set; }
    }

    public static class Gui
    {
        public static SelectWorldWindow SelectWorldWindow { get; private set; }
        public static Overlay Overlay { get; private set; }
        public static ProgressOverlay ProgressOverlay { get; private set; }
        public static BadgeOverlayManager BadgeOverlayManager { get; set; }
    }

    public static class Ipc
    {
        public static IPCProvider IPCProvider { get; private set; }
        public static TextAdvanceIPC TextAdvanceIPC { get; private set; }
        public static VnavmeshIPC VnavmeshIPC { get; private set; }
        public static WotsitManager WotsitManager { get; private set; }
        public static SplatoonManager SplatoonManager { get; private set; }
    }

    public static Memory Memory { get; private set; }
    public static TeleportService TeleportService { get; private set; }
    public static InstanceHandler InstanceHandler { get; private set; }
    public static ContextMenuManager ContextMenuManager { get; private set; }
    public static AddressBookFileSystemManager AddressBookFileSystemManager { get; private set; }
    public static CustomAliasFileSystemManager CustomAliasFileSystemManager { get; private set; }
    public static HttpClientProvider HttpClientProvider { get; private set; }
    public static DtrManager DtrManager { get; private set; }
    public static MapHanderService MapHanderService { get; private set; }
    public static SearchHelperOverlay SearchHelperOverlay { get; private set; }
}
