using ECommons.Configuration;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
