using Lifestream.GUI.Windows;

namespace Lifestream.Services;
public static class Service
{
    public static SelectWorldWindow SelectWorldWindow { get; private set; }
    public static TeleportService TeleportService { get; private set; }
    //public static NetworkDebugger NetworkDebugger { get; private set; }
    public static InstanceHandler InstanceHandler { get; private set; }
    public static ContextMenuManager ContextMenuManager { get; private set; }
    public static AddressBookFileSystemManager AddressBookFileSystemManager { get; private set; }
    public static CustomAliasFileSystemManager CustomAliasFileSystemManager { get; private set; }
}
