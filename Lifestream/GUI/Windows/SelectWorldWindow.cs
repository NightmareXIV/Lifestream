using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.SimpleGui;

namespace Lifestream.GUI.Windows;
public class SelectWorldWindow : Window
{
    private SelectWorldWindow() : base("Lifestream: Select World", ImGuiWindowFlags.AlwaysAutoResize)
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        var worlds = P.DataStore.DCWorlds.Concat(P.DataStore.Worlds).Select(x => ExcelWorldHelper.Get(x)).OrderBy(x => x.Name.ToString());
        if(!worlds.Any())
        {
            ImGuiEx.Text($"No available destinations");
            return;
        }
        var datacenters = worlds.Select(x => x.DataCenter).DistinctBy(x => x.Row).OrderBy(x => x.Value.Region).ToArray();
        if(ImGui.BeginTable("LifestreamSelectWorld", datacenters.Length, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuter))
        {
            foreach(var dc in datacenters)
            {
                var modifier = "";
                if(Player.Object?.HomeWorld.GameData.DataCenter.Row == dc.Row) modifier += "";
                if(Player.Object?.CurrentWorld.GameData.DataCenter.Row != dc.Row) modifier += "";
                ImGui.TableSetupColumn($"{modifier}{dc.Value.Name}");
            }
            ImGui.TableHeadersRow();
            ImGui.TableNextRow();
            var buttonSize = Vector2.Zero;
            foreach(var w in worlds)
            {
                var newSize = ImGuiHelpers.GetButtonSize("" + w.Name);
                if(newSize.X > buttonSize.X) buttonSize = newSize;
            }
            buttonSize += new Vector2(0, P.Config.ButtonHeightWorld);
            foreach(var dc in datacenters)
            {
                ImGui.TableNextColumn();
                foreach(var world in worlds)
                {
                    if(world.DataCenter.Row == dc.Row)
                    {
                        var modifier = "";
                        if(Player.Object?.HomeWorld.Id == world.RowId) modifier += "";
                        if(ImGuiEx.Button(modifier + world.Name, buttonSize, !Utils.IsBusy() && Player.Interactable && Player.Object?.CurrentWorld.Id != world.RowId))
                        {
                            P.ProcessCommand("/li", world.Name);
                        }
                    }
                }
            }
            ImGui.EndTable();
        }
    }
}
