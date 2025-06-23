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
        public static DataStore DataStore;
        public static ResidentialAethernet ResidentialAethernet;
        public static CustomAethernet CustomAethernet;
    }

    public static class Gui
    {
        public static SelectWorldWindow SelectWorldWindow;
        public static Overlay Overlay;
        public static ProgressOverlay ProgressOverlay;
    }

    public static class Ipc
    {
        public static IPCProvider IPCProvider;
        public static TextAdvanceIPC TextAdvanceIPC;
        public static VnavmeshIPC VnavmeshIPC;
        public static WotsitManager WotsitManager;
        public static SplatoonManager SplatoonManager;
    }

    public static Memory Memory;
    public static TeleportService TeleportService;
    public static InstanceHandler InstanceHandler;
    public static ContextMenuManager ContextMenuManager;
    public static AddressBookFileSystemManager AddressBookFileSystemManager;
    public static CustomAliasFileSystemManager CustomAliasFileSystemManager;
    public static HttpClientProvider HttpClientProvider;
    public static DtrManager DtrManager;
    public static MapHanderService MapHanderService;
    public static SearchHelperOverlay SearchHelperOverlay;
}
