using ECommons.GameHelpers;
using Lifestream.Data;
using NightmareUI;
using NightmareUI.ImGuiElements;
using OtterGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI;
public static class TabTravelBan
{
    public static void Draw()
    {
        WorldSelector.Instance.DisplayCurrent = true;
        ImGui.PushFont(UiBuilder.IconFont);
        ImGuiEx.Text(EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGuiEx.TextWrapped(EColor.RedBright, "Be mindful that this function is meant to be the last chance to avoid unrecoverable mistakes. Using this function may break other plugins that rely on Lifestream. Blocking travel in a specific direction will block it only via Lifestream. You can still travel manually.");

        ImGuiEx.LineCentered(() =>
        {
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add new entry"))
            {
                var entry = new TravelBanInfo();
                if(Player.Available)
                {
                    entry.CharaName = Player.Name;
                    entry.CharaHomeWorld = (int)Player.Object.HomeWorld.Id;
                }
                P.Config.TravelBans.Add(entry);
            }
        });
        if(ImGui.BeginTable("Bantable", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("##enabled");
            ImGui.TableSetupColumn("Character name and world", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Travel source");
            ImGui.TableSetupColumn("Travel destination");
            ImGui.TableSetupColumn("##control");

            ImGui.TableHeadersRow();
            for(int i = 0; i < P.Config.TravelBans.Count; i++)
            {
                var entry = P.Config.TravelBans[i];
                ImGui.PushID(entry.ID);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Checkbox("##en", ref entry.IsEnabled);
                ImGui.TableNextColumn();
                ImGuiEx.InputWithRightButtonsArea(() =>
                {
                    ImGui.InputTextWithHint("##chara", "Character name", ref entry.CharaName, 30);
                }, () =>
                {
                    ImGuiEx.Text("@");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100);
                    WorldSelector.Instance.Draw(ref entry.CharaHomeWorld);
                });
                ImGui.TableNextColumn();

                ImGui.SetNextItemWidth(100);
                if(ImGui.BeginCombo("##from", $"{entry.BannedFrom.Count} worlds", ImGuiComboFlags.HeightLarge))
                {
                    Utils.DrawWorldSelector(entry.BannedFrom);
                    ImGui.EndCombo();
                }
                ImGui.TableNextColumn();

                ImGui.SetNextItemWidth(100);
                if(ImGui.BeginCombo("##to", $"{entry.BannedTo.Count} worlds", ImGuiComboFlags.HeightLarge))
                {
                    Utils.DrawWorldSelector(entry.BannedTo);
                    ImGui.EndCombo();
                }
                ImGui.TableNextColumn();

                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    new TickScheduler(() => P.Config.TravelBans.Remove(entry));
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }
}
