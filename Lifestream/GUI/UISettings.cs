using Lumina.Excel.GeneratedSheets;

namespace Lifestream.GUI;

internal static class UISettings
{
    internal static void Draw()
    {
        ImGui.Checkbox("Enable overlay", ref P.Config.Enable);
        if (P.Config.Enable)
        {
            ImGui.Checkbox($"Display Aethernet menu", ref P.Config.ShowAethernet);
            ImGui.Checkbox($"Display world visit menu", ref P.Config.ShowWorldVisit);
            ImGui.Checkbox("Click aetheryte on map to teleport", ref P.Config.UseMapTeleport);
            ImGui.Checkbox($"Slow down aetheryte teleporting", ref P.Config.SlowTeleport);
            if (P.Config.SlowTeleport)
            {
                ImGui.SetNextItemWidth(200f);
                ImGui.DragInt("Teleport delay (ms)", ref P.Config.SlowTeleportThrottle);
            }
            ImGui.Checkbox("Fixed Lifestream position", ref P.Config.FixedPosition);
            if (P.Config.FixedPosition)
            {
                ImGui.SetNextItemWidth(200f);
                ImGuiEx.EnumCombo("Horizontal base position", ref P.Config.PosHorizontal);
                ImGui.SetNextItemWidth(200f);
                ImGuiEx.EnumCombo("Vertical base position", ref P.Config.PosVertical);
                ImGui.SetNextItemWidth(200f);
                ImGui.DragFloat2("Offset", ref P.Config.Offset);
            }
            ImGui.SetNextItemWidth(100f);
            ImGui.InputInt("Button left/right padding", ref P.Config.ButtonWidth);
            ImGui.SetNextItemWidth(100f);
            ImGui.InputInt("Aetheryte button top/bottom padding", ref P.Config.ButtonHeightAetheryte);
            ImGui.SetNextItemWidth(100f);
            ImGui.InputInt("World button top/bottom padding", ref P.Config.ButtonHeightWorld);
            //ImGui.Checkbox($"Allow closing Lifestream with ESC", ref P.Config.AllowClosingESC2);
            //ImGuiComponents.HelpMarker("To reopen, reenter the proximity of aetheryte");
            ImGui.Checkbox($"Hide Lifestream if common UI elements are open", ref P.Config.HideAddon);
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.EnumCombo($"Teleport world change gateway", ref P.Config.WorldChangeAetheryte, Lang.WorldChangeAetherytes);
            ImGui.Checkbox($"Attempt to walk to nearby aetheryte on world change command from greater distance if possible", ref P.Config.WalkToAetheryte);
            ImGui.Checkbox($"Add firmament location into Foundation aetheryte", ref P.Config.Firmament);
            ImGui.Checkbox($"Automatically leave non cross-world party upon changing world", ref P.Config.LeavePartyBeforeWorldChange);
            ImGui.Checkbox($"Allow travelling to another data center", ref P.Config.AllowDcTransfer);
            ImGui.Checkbox($"Leave party before switching data center", ref P.Config.LeavePartyBeforeLogout);
            ImGui.Checkbox($"Teleport to gateway before switching data center if not in sanctuary", ref P.Config.TeleportToGatewayBeforeLogout);
            ImGui.Checkbox($"Teleport to gateway after completing data center travel", ref P.Config.DCReturnToGateway);
            ImGui.Checkbox($"Teleport to specific aethernet destination after world/dc visit", ref P.Config.WorldVisitTPToAethernet);
            if (P.Config.WorldVisitTPToAethernet)
            {
                ImGui.SetNextItemWidth(250f);
                ImGui.InputText("Aethernet destination, as if you'd use in \"/li\" command", ref P.Config.WorldVisitTPTarget, 50);
                ImGui.Checkbox($"Only teleport from command but not from overlay", ref P.Config.WorldVisitTPOnlyCmd);
            }
            ImGui.Checkbox($"Hide progress bar", ref P.Config.NoProgressBar);
            ImGui.Checkbox($"Wait until game UI is ready before executing commands", ref P.Config.WaitForScreen);
            ImGuiEx.HelpMarker($"Enable this option if you are experiencing issues with Lifestream trying to use aetherytes or commands too early");
        }
        if (ImGui.CollapsingHeader("Hidden aetherytes"))
        {
            uint toRem = 0;
            foreach (var x in P.Config.Hidden)
            {
                ImGuiEx.Text($"{Svc.Data.GetExcelSheet<Aetheryte>().GetRow(x)?.AethernetName.Value?.Name.ToString() ?? x.ToString()}");
                ImGui.SameLine();
                if (ImGui.SmallButton($"Delete##{x}"))
                {
                    toRem = x;
                }
            }
            if (toRem > 0)
            {
                P.Config.Hidden.Remove(toRem);
            }
        }
    }
}
