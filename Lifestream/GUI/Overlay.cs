using ECommons.GameHelpers;
using Lifestream.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI
{
    internal class Overlay : Window
    {
        public Overlay() : base("Lifestream Overlay", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize, true)
        {
            IsOpen = true;
        }

        Vector2 bWidth = new(10, 10);
        Vector2 ButtonSizeAetheryte => bWidth + new Vector2(P.Config.ButtonWidth, P.Config.ButtonHeightAetheryte);
        Vector2 ButtonSizeWorld => bWidth + new Vector2(P.Config.ButtonWidth, P.Config.ButtonHeightWorld);
        Vector2 WSize = new(200, 200);

        public override void PreDraw()
        {
            if (P.Config.FixedPosition)
            {
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(GetBasePosX(), GetBasePosY()) + P.Config.Offset);
            }
        }

        float GetBasePosX()
        {
            if (P.Config.PosHorizontal == BasePositionHorizontal.Middle)
            {
                return ImGuiHelpers.MainViewport.Size.X / 2 - WSize.X / 2;
            }
            else if (P.Config.PosHorizontal == BasePositionHorizontal.Right)
            {
                return ImGuiHelpers.MainViewport.Size.X - WSize.X;
            }
            else
            {
                return 0;
            }
        }

        float GetBasePosY()
        {
            if (P.Config.PosVertical == BasePositionVertical.Middle)
            {
                return ImGuiHelpers.MainViewport.Size.Y / 2 - WSize.Y / 2;
            }
            else if (P.Config.PosVertical == BasePositionVertical.Bottom)
            {
                return ImGuiHelpers.MainViewport.Size.Y - WSize.Y;
            }
            else
            {
                return 0;
            }
        }

        public override void Draw()
        {
            RespectCloseHotkey = P.Config.AllowClosingESC2;
            var master = Util.GetMaster();
            if (P.ActiveAetheryte.Value.IsWorldChangeAetheryte())
            {
                if (ImGui.BeginTable("LifestreamTable", 2, ImGuiTableFlags.SizingStretchSame))
                {
                    ImGui.TableNextColumn();
                    DrawAethernet();
                    ImGui.TableNextColumn();
                    var cWorld = Svc.ClientState.LocalPlayer?.CurrentWorld.GameData.Name.ToString();
                    foreach (var x in P.DataStore.Worlds)
                    {
                        ResizeButton(x);
                        var isHomeWorld = x == Player.HomeWorld;
                        var d = x == cWorld || Util.IsDisallowedToChangeWorld();
                        if (d) ImGui.BeginDisabled();
                        if (ImGui.Button((isHomeWorld?(Lang.Symbols.HomeWorld+" "):"")+x, ButtonSizeWorld))
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
                if (ImGui.Button(master.Name, ButtonSizeAetheryte))
                {
                    TaskAethernetTeleport.Enqueue(master);
                }
                if (md) ImGui.EndDisabled();
                foreach (var x in P.DataStore.Aetherytes[master])
                {
                    ResizeButton(x.Name);
                    var d = P.ActiveAetheryte == x;
                    if (d) ImGui.BeginDisabled();
                    if (ImGui.Button(x.Name, ButtonSizeAetheryte))
                    {
                        TaskAethernetTeleport.Enqueue(x);
                    }
                    if (d) ImGui.EndDisabled();
                }
            }
            WSize = ImGui.GetWindowSize();
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
            if (!P.Config.Enable) return false;
            var ret = Util.CanUseAetheryte();
            if (!ret)
            {
                bWidth = new(10, 10);
            }
            return ret && !Util.IsAddonsVisible(Util.Addons);
        }
    }
}
