using ECommons.ExcelServices;
using ECommons.SimpleGui;
using ECommons.UIHelpers.AddonMasterImplementations;
using Lifestream.Systems;
using World = Lumina.Excel.GeneratedSheets.World;

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
        var worlds = Utils.GetVisitableWorldsFrom(homeWorldData).OrderBy(x => x.Name.ToString()).ToArray();
        if(worlds.Length == 0)
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
                            if(ImGuiEx.Button(modifier + world.Name, buttonSize, !Utils.IsBusy() && chara.CurrentWorld != world.RowId))
                            {
                                Command(chara.Name, chara.CurrentWorld, chara.HomeWorld, world);
                                this.IsOpen = false;
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

    public static void Command(string charaName, uint currentWorld, uint homeWorld, World world)
    {
        var charaCurrentWorld = ExcelWorldHelper.Get(currentWorld);
        var charaHomeWorld = ExcelWorldHelper.Get(homeWorld);
        var isInHomeDc = charaCurrentWorld.DataCenter.Row == charaHomeWorld.DataCenter.Row;
        if(world.RowId == charaHomeWorld.RowId)
        {
            //returning home
            if(isInHomeDc)
            {
                CharaSelectVisit.HomeToHome(world.Name, charaName, homeWorld);
            }
            else
            {
                CharaSelectVisit.GuestToHome(world.Name, charaName, homeWorld);
            }
        }
        else
        {
            if(world.DataCenter.Row != charaCurrentWorld.DataCenter.Row)
            {
                //visiting another DC
                if(charaCurrentWorld.RowId == charaHomeWorld.RowId)
                {
                    CharaSelectVisit.HomeToGuest(world.Name, charaName, homeWorld);
                }
                else
                {
                    CharaSelectVisit.GuestToGuest(world.Name, charaName, homeWorld);
                }
            }
            else
            {
                //teleporting to the other world's same dc
                if(isInHomeDc)
                {
                    //just log in and use world visit
                    CharaSelectVisit.GuestToHome(world.Name, charaName, homeWorld, skipReturn: true);
                }
                else
                {
                    //special guest to guest sequence
                    CharaSelectVisit.GuestToGuest(world.Name, charaName, homeWorld);
                }
            }
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
