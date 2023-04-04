using Lifestream.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal class Overlay : Window
    {
        public Overlay() : base("Lifestream Overlay",  ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize, true)
        {
            this.IsOpen = true;
            this.RespectCloseHotkey = false;
            this.ShowCloseButton = false;
        }

        Vector2 bWidth = new(10, 10);
        Vector2 ButtonSize => bWidth * 1.2f;

        public override void Draw()
        {
            var master = P.ActiveAetheryte.Value.Ref.IsAetheryte ? P.ActiveAetheryte.Value : P.DataStore.GetMaster(P.ActiveAetheryte.Value.Ref);
            if (P.ActiveAetheryte.Value.IsWorldChangeAetheryte())
            {
                if(ImGui.BeginTable("LifestreamTable", 2, ImGuiTableFlags.SizingStretchSame))
                {
                    ImGui.TableNextColumn();
                    DrawAethernet();
                    ImGui.TableNextColumn();
                    var cWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.GameData.Name.ToString();
                    foreach (var x in P.DataStore.Worlds)
                    {
                        ResizeButton(x);
                        var d = x == cWorld || Svc.Condition[ConditionFlag.WaitingToVisitOtherWorld];
                        if (d) ImGui.BeginDisabled();
                        if (ImGui.Button(x, ButtonSize))
                        {
                            TaskChangeWorld.Enqueue(x);
                        }
                        if (d) ImGui.EndDisabled();
                    }
                    ImGui.EndTable();
                }
            }
            else
            {
                DrawAethernet();
            }
            
            void DrawAethernet()
            {
                ResizeButton(master.Name);
                var md = P.ActiveAetheryte == master;
                if (md) ImGui.BeginDisabled();
                if (ImGui.Button(master.Name, ButtonSize))
                {
                    TaskAethernetTeleport.Enqueue(master);
                }
                if(md) ImGui.EndDisabled();
                foreach (var x in P.DataStore.Aetherytes[master])
                {
                    ResizeButton(x.Name);
                    var d = P.ActiveAetheryte == x;
                    if (d) ImGui.BeginDisabled();
                    if (ImGui.Button(x.Name, ButtonSize))
                    {
                        TaskAethernetTeleport.Enqueue(x);
                    }
                    if(d) ImGui.EndDisabled();
                }
            }
        }

        void ResizeButton(string t)
        {
            var s = ImGuiHelpers.GetButtonSize(t);
            if (bWidth.X < s.X)
            {
                bWidth = s;
            }
        }

        public override bool DrawConditions()
        {
            var ret = P.DataStore.Territories.Contains(P.Territory) && P.ActiveAetheryte != null && !P.TaskManager.IsBusy && !IsOccupied();
            if (!ret)
            {
                bWidth = new(10, 10);
            }
            return ret;
        }
    }
}
