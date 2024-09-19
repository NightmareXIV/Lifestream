using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI.Windows;
public unsafe class CharaSelectOverlay : EzOverlayWindow
{
    public string CharaName = "";
    public uint CharaWorld = 0;
    private BackgroundWindow Modal;
    public CharaSelectOverlay() : base("", HorizontalPosition.Middle, VerticalPosition.Middle)
    {
        this.IsOpen = false;
        this.Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse;
        this.RespectCloseHotkey = false;
        Modal = new(this);
        EzConfigGui.WindowSystem.AddWindow(Modal);
    }

    public void Open(string charaName, uint homeWorld)
    {
        CharaName = charaName;
        CharaWorld = homeWorld;
        this.WindowName = $"{CharaName}@{ExcelWorldHelper.GetName(CharaWorld)}";
        this.Modal.IsOpen = true;
        this.IsOpen = true;
    }

    public override void DrawAction()
    {
        var homeWorldData = ExcelWorldHelper.Get(CharaWorld);
        if(homeWorldData == null)
        {
            ImGuiEx.Text($"Error: for world {homeWorldData} no data found");
            return;
        }
        var worlds = Utils.GetVisitableWorldsFrom(homeWorldData).OrderBy(x => x.Name.ToString());
        if(!worlds.Any())
        {
            ImGuiEx.Text($"No available destinations");
            return;
        }
        if(TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m) && !Utils.IsAddonVisible("SelectYesno") && !Utils.IsAddonVisible("SelectOk") && !Utils.IsAddonVisible("ContextMenu") && !Utils.IsAddonVisible("_CharaSelectWorldServer") && !Utils.IsAddonVisible("AddonContextSub"))
        {
            var chara = m.Characters.FirstOrDefault(x => x.Name ==  CharaName && x.HomeWorld == CharaWorld);
            if (chara == null)
            {
                ImGuiEx.Text($"Character not found: {CharaName}@{ExcelWorldHelper.GetName(CharaWorld)}");
                return;
            }
            var datacenters = worlds.Select(x => x.DataCenter).DistinctBy(x => x.Row).OrderBy(x => x.Value.Region).ToArray();
            if(ImGui.BeginTable("LifestreamSelectWorld", datacenters.Length, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoSavedSettings))
            {
                foreach(var dc in datacenters)
                {
                    var modifier = "";
                    if(ExcelWorldHelper.Get(chara.HomeWorld).DataCenter.Row == dc.Row) modifier += "";
                    if(ExcelWorldHelper.Get(chara.CurrentWorld).DataCenter.Row != dc.Row) modifier += "";
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
                            if(CharaWorld == world.RowId) modifier += "";
                            if(ImGuiEx.Button(modifier + world.Name, buttonSize, !Utils.IsBusy()))
                            {
                                //P.ProcessCommand("/li", world.Name);
                            }
                        }
                    }
                }
                ImGui.EndTable();
            }
        }
        else
        {
            ImGuiEx.Text("Unable to display world selection.");
        }
    }

    private class BackgroundWindow : Window
    {
        CharaSelectOverlay ParentWindow;
        public BackgroundWindow(CharaSelectOverlay parentWindow) : base($"Lifestream CharaSelect background",  ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse, true)
        {
            this.RespectCloseHotkey = false;
            ParentWindow = parentWindow;
        }

        public override void PreDraw()
        {
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(Vector2.Zero);
            ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGuiEx.Vector4FromRGBA(0x000000AA));
        }

        public override void Draw()
        {
            if(!ParentWindow.IsOpen) this.IsOpen = false;
            CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
        }

        public override void PostDraw()
        {
            ImGui.PopStyleColor();
        }
    }
}
