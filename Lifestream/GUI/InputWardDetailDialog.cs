using ECommons.Configuration;
using Lifestream.Data;
using NightmareUI.ImGuiElements;

namespace Lifestream.GUI;
public static class InputWardDetailDialog
{
    public static AddressBookEntry Entry = null;
    public static bool Open = false;
    public static void Draw()
    {
        if(Entry != null)
        {
            if(!ImGui.IsPopupOpen($"###ABEEditModal"))
            {
                Open = true;
                ImGui.OpenPopup($"###ABEEditModal");
            }
            if(ImGui.BeginPopupModal($"Editing {Entry.Name}###ABEEditModal", ref Open, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if(ImGui.BeginTable($"ABEEditTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableSetupColumn("Edit1", ImGuiTableColumnFlags.WidthFixed, 150);
                    ImGui.TableSetupColumn("Edit2", ImGuiTableColumnFlags.WidthFixed, 250);

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Name:");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputTextWithHint($"##name", Entry.GetAutoName(), ref Entry.Name, 150);

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Alias:");
                    ImGuiEx.HelpMarker($"If you enable and set alias, you will be able to use it in a \"li\" command: \"/li alias\". Aliases are case-insensitive.");
                    ImGui.TableNextColumn();
                    ImGui.Checkbox($"##alias", ref Entry.AliasEnabled);
                    if(Entry.AliasEnabled)
                    {
                        ImGui.SameLine();
                        ImGuiEx.InputWithRightButtonsArea(() => ImGui.InputText($"##aliasname", ref Entry.Alias, 150), () =>
                        {
                            AddressBookEntry existing = null;
                            if(Entry.Alias != "" && C.AddressBookFolders.Any(b => b.Entries.TryGetFirst(a => a != Entry && a.AliasEnabled && a.Alias.EqualsIgnoreCase(Entry.Alias), out existing)))
                            {
                                ImGuiEx.HelpMarker($"Alias conflict found: this alias already set for {existing?.Name.NullWhenEmpty() ?? existing?.GetAutoName()}", EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                            }
                        });
                    }

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"World:");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    WorldSelector.Instance.Draw(ref Entry.World);

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Residential District:");
                    ImGui.TableNextColumn();
                    if(Entry.City.RenderIcon()) ImGui.SameLine(0, 1);
                    ImGuiEx.SetNextItemFullWidth();
                    Utils.ResidentialAetheryteEnumSelector($"##resdis", ref Entry.City);

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Ward:");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    ImGui.InputInt($"##ward", ref Entry.Ward.ValidateRange(1, 30));

                    ImGui.TableNextColumn();
                    ImGuiEx.TextV($"Property Type:");
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    ImGuiEx.EnumRadio(ref Entry.PropertyType, true);

                    if(Entry.PropertyType == Enums.PropertyType.Apartment)
                    {
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV($"");
                        ImGui.TableNextColumn();
                        ImGui.Checkbox("Subdivision", ref Entry.ApartmentSubdivision);

                        ImGui.TableNextColumn();
                        ImGuiEx.TextV($"Room:");
                        ImGui.TableNextColumn();
                        ImGuiEx.SetNextItemFullWidth();
                        ImGui.InputInt($"##room", ref Entry.Apartment.ValidateRange(1, 99999));
                    }

                    if(Entry.PropertyType == Enums.PropertyType.House)
                    {
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV($"Plot:");
                        ImGui.TableNextColumn();
                        ImGuiEx.SetNextItemFullWidth();
                        ImGui.InputInt($"##plot", ref Entry.Plot.ValidateRange(1, 60));
                    }

                    ImGui.EndTable();
                }
                ImGuiEx.LineCentered(() =>
                {
                    if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Save, "Save and close"))
                    {
                        Open = false;
                        EzConfig.Save();
                    }
                });
                ImGui.EndPopup();
            }
        }
        if(!Open) Entry = null;
    }
}
