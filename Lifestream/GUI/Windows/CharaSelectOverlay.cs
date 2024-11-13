using ECommons.ExcelServices;
using ECommons.SimpleGui;
using ECommons.UIHelpers.AddonMasterImplementations;
using Lifestream.Systems;
using Lifestream.Tasks.Login;
using System.Xml.Serialization;
using World = Lumina.Excel.Sheets.World;

namespace Lifestream.GUI.Windows;
public unsafe class CharaSelectOverlay : EzOverlayWindow
{
    public string CharaName = "";
    public uint CharaWorld = 0;
    private BackgroundWindow Modal;
    private bool NoLogin = false;
    public CharaSelectOverlay() : base("", HorizontalPosition.Middle, VerticalPosition.Middle)
    {
        IsOpen = false;
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse;
        RespectCloseHotkey = false;
        Modal = new(this);
        EzConfigGui.WindowSystem.AddWindow(Modal);
    }

    public void Open(string charaName, uint homeWorld)
    {
        CharaName = charaName;
        CharaWorld = homeWorld;
        WindowName = $"{CharaName}@{ExcelWorldHelper.GetName(CharaWorld)}";
        Modal.IsOpen = true;
        IsOpen = true;
    }

    public override void DrawAction()
    {
        var homeWorldData = ExcelWorldHelper.Get(CharaWorld);
        if(homeWorldData == null)
        {
            ImGuiEx.Text($"Error: for world {homeWorldData} no data found");
            return;
        }
        var worlds = Utils.GetVisitableWorldsFrom(homeWorldData.Value).OrderBy(x => x.Name.ToString()).ToArray();
        if(worlds.Length == 0)
        {
            ImGuiEx.Text($"No available destinations");
            return;
        }
        if(TryGetAddonMaster<AddonMaster._CharaSelectListMenu>(out var m) && !Utils.IsAddonVisible("SelectYesno") && !Utils.IsAddonVisible("SelectOk") && !Utils.IsAddonVisible("ContextMenu") && !Utils.IsAddonVisible("_CharaSelectWorldServer") && !Utils.IsAddonVisible("AddonContextSub"))
        {
            var chara = m.Characters.FirstOrDefault(x => x.Name == CharaName && x.HomeWorld == CharaWorld);
            if(chara == null)
            {
                ImGuiEx.Text($"Character not found: {CharaName}@{ExcelWorldHelper.GetName(CharaWorld)}");
                return;
            }
            ImGuiEx.LineCentered(() =>
            {
                ImGui.Checkbox("Do not log in after transfer", ref NoLogin);
            });
            var datacenters = worlds.Select(x => x.DataCenter).DistinctBy(x => x.RowId).OrderBy(x => x.Value.Region).ToArray();
            if(ImGui.BeginTable("LifestreamSelectWorld", datacenters.Length, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoSavedSettings))
            {
                foreach(var dc in datacenters)
                {
                    var modifier = "";
                    if(ExcelWorldHelper.Get(chara.HomeWorld)?.DataCenter.RowId == dc.RowId) modifier += "";
                    if(ExcelWorldHelper.Get(chara.CurrentWorld)?.DataCenter.RowId != dc.RowId) modifier += "";
                    ImGui.TableSetupColumn($"{modifier}{dc.Value.Name}");
                }
                ImGui.TableHeadersRow();
                ImGui.TableNextRow();
                var buttonSize = Vector2.Zero;
                foreach(var w in worlds)
                {
                    var newSize = ImGuiHelpers.GetButtonSize("" + w.Name.ToString());
                    if(newSize.X > buttonSize.X) buttonSize = newSize;
                }
                buttonSize += new Vector2(0, P.Config.ButtonHeightWorld);
                foreach(var dc in datacenters)
                {
                    ImGui.TableNextColumn();
                    foreach(var world in worlds)
                    {
                        if(world.DataCenter.RowId == dc.RowId)
                        {
                            var modifier = "";
                            if(CharaWorld == world.RowId) modifier += "";
                            if(ImGuiEx.Button(modifier + world.Name.ToString(), buttonSize, !Utils.IsBusy() && chara.CurrentWorld != world.RowId))
                            {
                                if(chara.IsVisitingAnotherDC)
                                {
                                    ReconnectToValidDC(chara.Name, chara.CurrentWorld, chara.HomeWorld, world, NoLogin);
                                }
                                else
                                {
                                    Command(chara.Name, chara.CurrentWorld, chara.HomeWorld, world, NoLogin);
                                }
                                NoLogin = false;
                                IsOpen = false;
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

    public static void ReconnectToValidDC(string charaName, uint currentWorld, uint homeWorld, World world, bool noLogin)
    {
        try
        {
            Utils.AssertCanTravel(charaName, homeWorld, currentWorld, world.RowId);
        }
        catch(Exception e)
        {
            e.Log();
            return;
        }
        P.TaskManager.Enqueue(TaskChangeCharacter.CloseCharaSelect);
        P.TaskManager.Enqueue(() => TaskChangeCharacter.ConnectToDc(ExcelWorldHelper.GetName(currentWorld), Utils.GetServiceAccount($"{charaName}@{ExcelWorldHelper.GetName(homeWorld)}")));
        P.TaskManager.Enqueue(() => Command(charaName, currentWorld, homeWorld, world, noLogin));
    }

    public static void Command(string charaName, uint currentWorld, uint homeWorld, World targetWorld, bool noLogin)
    {
        var charaCurrentWorld = ExcelWorldHelper.Get(currentWorld);
        var charaHomeWorld = ExcelWorldHelper.Get(homeWorld);
        try
        {
            Utils.AssertCanTravel(charaName, homeWorld, currentWorld, targetWorld.RowId);
        }
        catch(Exception e)
        {
            e.Log();
            return;
        }
        var isInHomeDc = charaCurrentWorld?.DataCenter.RowId == charaHomeWorld?.DataCenter.RowId;
        if(targetWorld.RowId == charaHomeWorld?.RowId)
        {
            //returning home
            if(isInHomeDc)
            {
                PluginLog.Information($"CharaSelectVisit: Return - HomeToHome (1)");
                CharaSelectVisit.HomeToHome(targetWorld.Name.ToString(), charaName, homeWorld, noLogin: noLogin);
            }
            else
            {
                PluginLog.Information($"CharaSelectVisit: Return - GuestToHome (2)");
                CharaSelectVisit.GuestToHome(targetWorld.Name.ToString(), charaName, homeWorld, noLogin: noLogin);
            }
        }
        else
        {
            if(targetWorld.DataCenter.RowId != charaCurrentWorld?.DataCenter.RowId)
            {
                //visiting another DC
                if(charaCurrentWorld?.RowId == charaHomeWorld?.RowId)
                {
                    PluginLog.Information($"CharaSelectVisit: Visit DC - HomeToGuest (3)");
                    CharaSelectVisit.HomeToGuest(targetWorld.Name.ToString(), charaName, homeWorld, noLogin: noLogin);
                }
                else
                {
                    PluginLog.Information($"CharaSelectVisit: Visit DC - GuestToGuest (5)");
                    CharaSelectVisit.GuestToGuest(targetWorld.Name.ToString(), charaName, homeWorld, noLogin: noLogin, useSameWorldReturnHome: isInHomeDc);
                }
            }
            else
            {
                //teleporting to the other world's same dc
                if(isInHomeDc || P.Config.UseGuestWorldTravel)
                {
                    //just log in and use world visit
                    PluginLog.Information($"CharaSelectVisit: Visit World - GuestToHome (6)");
                    CharaSelectVisit.GuestToHome(targetWorld.Name.ToString(), charaName, homeWorld, skipReturn: true, noLogin: noLogin);
                }
                else
                {
                    //special guest to guest sequence
                    PluginLog.Information($"CharaSelectVisit: Visit World - GuestToGuest (7)");
                    CharaSelectVisit.GuestToGuest(targetWorld.Name.ToString(), charaName, homeWorld, noLogin: noLogin);
                }
            }
        }
    }

    private class BackgroundWindow : Window
    {
        private CharaSelectOverlay ParentWindow;
        public BackgroundWindow(CharaSelectOverlay parentWindow) : base($"Lifestream CharaSelect background", ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse, true)
        {
            RespectCloseHotkey = false;
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
            if(!ParentWindow.IsOpen) IsOpen = false;
            CImGui.igBringWindowToDisplayBack(CImGui.igGetCurrentWindow());
        }

        public override void PostDraw()
        {
            ImGui.PopStyleColor();
        }
    }
}
