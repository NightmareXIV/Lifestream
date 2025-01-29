using Lifestream.GUI.Windows;
using Lifestream.IPC;

namespace Lifestream.Services;
public static class Service
{
    public static SelectWorldWindow SelectWorldWindow;
    public static TeleportService TeleportService;
    //public static NetworkDebugger NetworkDebugger;
    public static InstanceHandler InstanceHandler;
    public static ContextMenuManager ContextMenuManager;
    public static AddressBookFileSystemManager AddressBookFileSystemManager;
    public static CustomAliasFileSystemManager CustomAliasFileSystemManager;
    public static TextAdvanceIPC TextAdvanceIPC;
    public static HttpClientProvider HttpClientProvider;
    public static DtrManager DtrManager;
    public static WotsitManager WotsitManager;
}
