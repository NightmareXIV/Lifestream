using Lifestream.GUI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Services;
public static class Service
{
    public static SelectWorldWindow SelectWorldWindow { get; private set; }
    public static TeleportService TeleportService { get; private set; }
    //public static NetworkDebugger NetworkDebugger { get; private set; }
    public static InstanceHandler InstanceHandler { get; private set; }
    //public static ContextMenuManager ContextMenuManager { get; private set; }
}
