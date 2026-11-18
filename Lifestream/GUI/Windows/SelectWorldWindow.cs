using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Lumina.Excel.Sheets;
using World = Lumina.Excel.Sheets.World;

namespace Lifestream.GUI.Windows;
#nullable enable
public class SelectWorldWindow : Window
{
    public string? Chara;

    private SelectWorldWindow() : base("Lifestream: Select World", ImGuiWindowFlags.AlwaysAutoResize)
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    [Obsolete("Use Open() method", true)]
    public new bool IsOpen { get; set; }

    public void Open(string? chara = null)
    {
        Chara = chara;
        base.IsOpen = true;
    }

    public override void Draw()
    {
        World? homeWorld;
        World?[] worlds;
        if(Chara != null && Chara.Contains("@"))
        {
            homeWorld = ExcelWorldHelper.Get(Chara.Split("@")[1]);
            ImGuiEx.LineCentered("CharaInfo", () => ImGuiEx.Text(ImGuiColors.ParsedOrange, $"Relogging to {Chara} and travelling to:"));
            var b = S.Data.DataStore.BuildWorlds(homeWorld?.DataCenter.RowId ?? 0, homeWorld?.RowId ?? 0);
            worlds = b.dcWorlds.Concat(b.worlds).Select(x => ExcelWorldHelper.Get(x)).OrderBy(x => x?.Name.ToString()).ToArray();
        }
        else
        {
            homeWorld = Player.Object?.HomeWorld.ValueNullable;
            worlds = S.Data.DataStore.DCWorlds.Concat(S.Data.DataStore.Worlds).Select(x => ExcelWorldHelper.Get(x)).OrderBy(x => x?.Name.ToString()).ToArray();
        }
        if(!worlds.Any())
        {
            ImGuiEx.Text($"No available destinations");
            return;
        }
        var datacenters = worlds.Select(x => x?.Unknown0).Where(x => x != null).DistinctBy(x => x).OrderBy(x => WorldDCGroupType.Get(x.Value).Region.RowId).ToArray();
        if(ImGui.BeginTable("LifestreamSelectWorld", datacenters.Length, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuter))
        {
            foreach(var dc in datacenters)
            {
                var modifier = "";
                if(homeWorld?.DataCenter.ValueNullable?.RowId == dc) modifier += "";
                if(Player.Object?.CurrentWorld.ValueNullable?.DataCenter.RowId != dc) modifier += "";
                ImGui.TableSetupColumn($"{modifier}{WorldDCGroupType.GetRef(dc.Value).ValueNullable?.Name}");
            }
            ImGui.TableHeadersRow();
            ImGui.TableNextRow();
            var buttonSize = Vector2.Zero;
            foreach(var w in worlds)
            {
                var newSize = ImGuiHelpers.GetButtonSize("" + w?.Name.ToString());
                if(newSize.X > buttonSize.X) buttonSize = newSize;
            }
            buttonSize += new Vector2(0, C.ButtonHeightWorld);
            foreach(var dc in datacenters)
            {
                ImGui.TableNextColumn();
                foreach(var world in worlds)
                {
                    if(world?.Unknown0 == dc)
                    {
                        var modifier = "";
                        if(homeWorld?.RowId == world?.RowId) modifier += "";
                        if(Chara == null)
                        {
                            if(ImGuiEx.Button(modifier + world?.Name.ToString(), buttonSize, !Utils.IsBusy() && Player.Interactable && Player.Object?.CurrentWorld.RowId != world?.RowId))
                            {
                                P.ProcessCommand("/li", world?.Name.ToString());
                            }
                        }
                        else
                        {
                            if(ImGuiEx.Button(modifier + world?.Name.ToString(), buttonSize, (!Utils.IsBusy() && Player.Interactable) || Utils.CanAutoLogin()))
                            {
                                S.Ipc.IPCProvider.ChangeCharacterAndTravel(Chara.Split("@")[0], Chara.Split("@")[1], world?.Name.ToString());
                                base.IsOpen = false;
                            }
                        }
                    }
                }
            }
            ImGui.EndTable();
        }
    }
}
