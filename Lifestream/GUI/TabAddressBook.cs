using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Tasks.CrossDC;

namespace Lifestream.GUI;
public static unsafe class TabAddressBook
{
    public static Dictionary<ResidentialAetheryte, string> ResidentialNames = new()
    {
        [ResidentialAetheryte.Gridania] = "Lavender Beds",
        [ResidentialAetheryte.Limsa] = "Mist",
        [ResidentialAetheryte.Uldah] = "Goblet",
        [ResidentialAetheryte.Kugane] = "Shirogane",
        [ResidentialAetheryte.Foundation] = "Empyreum",
    };
    public static void Draw()
    {
        var selector = P.AddressBookFileSystem.Selector;
				selector.Draw(150f);
				ImGui.SameLine();
				if (P.Config.AddressBookFolders.Count == 0)
        {
            var book = new AddressBookFolder() { IsDefault = true };
						P.AddressBookFileSystem.Create(book, "Default Book", out _);
        }
        if (ImGui.BeginChild("Child"))
        {
            if (selector.Selected != null)
            {
                var book = selector.Selected;
                DrawBook(book);
            }
            else
            {
                if (P.Config.AddressBookFolders.TryGetFirst(x => x.IsDefault, out var value))
                {
                    selector.SelectByValue(value);
                }
                ImGuiEx.TextWrapped($"To begin, select an address book to use.");
            }
        }
        ImGui.EndChild();
    }

    static void DrawBook(AddressBookFolder book)
    {
				if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add New Address"))
				{
						var h = HousingManager.Instance();
						var entry = new AddressBookEntry();
						if (h != null)
						{
								entry.Ward = h->GetCurrentWard() + 1;
								entry.Plot = h->GetCurrentPlot() + 1;
								entry.World = (int)(Player.Object?.CurrentWorld.Id ?? 21);
								entry.City = Utils.GetResidentialAetheryteByTerritoryType(Svc.ClientState.TerritoryType) ?? ResidentialAetheryte.Uldah;
						}
						book.Entries.Add(entry);
				}
				if (ImGui.BeginTable($"##addressbook", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
				{
						ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn("Address");
						ImGui.TableSetupColumn("##control");
						ImGui.TableHeadersRow();

						for (int i = 0; i < book.Entries.Count; i++)
						{
								var entry = book.Entries[i];
								ImGui.PushID($"House{i}");
								ImGui.TableNextRow();
								ImGui.TableNextColumn();
								var bsize = ImGuiHelpers.GetButtonSize("A") with { X = ImGui.GetContentRegionAvail().X };
								ImGui.PushStyleColor(ImGuiCol.Button, 0);
								ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, Vector2.Zero);
								if (ImGui.Button($"{entry.Name}", bsize))
								{
										if (Player.Interactable && !P.TaskManager.IsBusy)
										{
												if (entry.PropertyType == PropertyType.House)
												{
														TaskTpAndGoToWard.Enqueue(ExcelWorldHelper.GetName(entry.World), entry.City, entry.Ward, entry.Plot - 1, false, default);
												}
												else if (entry.PropertyType == PropertyType.Apartment)
												{
														TaskTpAndGoToWard.Enqueue(ExcelWorldHelper.GetName(entry.World), entry.City, entry.Ward, entry.Apartment - 1, true, entry.ApartmentSubdivision);
												}
										}
								}
								ImGui.PopStyleVar();
								ImGui.PopStyleColor();
								ImGui.TableNextColumn();
								ImGuiEx.TextV($"{ExcelWorldHelper.GetName(entry.World)}, {ResidentialNames.SafeSelect(entry.City)}, w{entry.Ward}, " + (entry.PropertyType == PropertyType.House ? $"p{entry.Plot}" : $"a{entry.Apartment}"));
								ImGui.TableNextColumn();
								if (ImGuiEx.IconButton(FontAwesomeIcon.Edit))
								{
										ImGui.OpenPopup($"Edit{i}");
								}
								ImGui.SameLine();
								if (ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
								{
										var rem = i;
										new TickScheduler(() => book.Entries.RemoveAt(rem));
								}

								if (ImGui.BeginPopup($"Edit{i}"))
								{
										ImGui.SetNextItemWidth(200f);
										ImGui.InputTextWithHint("Name", "Entry name", ref entry.Name, 200);
										ImGui.SetNextItemWidth(200f);
										if (ImGui.BeginCombo($"World", ExcelWorldHelper.GetName(entry.World), ImGuiComboFlags.HeightLarge))
										{
												foreach (var x in ExcelWorldHelper.GetDataCenters())
												{
														ImGuiEx.Text($"{x.Name}");
														foreach (var w in ExcelWorldHelper.GetPublicWorlds(x.RowId))
														{
																ImGuiEx.Spacing();
																if (ImGui.Selectable($"{w.Name}", entry.World == w.RowId)) entry.World = (int)w.RowId;
																if (entry.World == w.RowId && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
														}
												}
												ImGui.EndCombo();
										}
										ImGui.SetNextItemWidth(200f);
										ImGuiEx.EnumCombo($"Residential district", ref entry.City, ResidentialNames);
										ImGui.SetNextItemWidth(200f);
										ImGui.InputInt($"Ward", ref entry.Ward.ValidateRange(1, 30));
										ImGui.SetNextItemWidth(200f);
										ImGuiEx.EnumCombo("Property type", ref entry.PropertyType);
										if (entry.PropertyType == PropertyType.House)
										{
												ImGui.SetNextItemWidth(200f);
												ImGui.InputInt($"Plot", ref entry.Plot.ValidateRange(1, 60));
										}
										else
										{
												ImGui.SetNextItemWidth(100f);
												ImGui.InputInt($"Room", ref entry.Apartment.ValidateRange(1, int.MaxValue));
												ImGui.SameLine();
												ImGui.Checkbox("Subdivision", ref entry.ApartmentSubdivision);
										}
										ImGui.EndPopup();
								}

								ImGui.PopID();
						}

						ImGui.EndTable();
				}
		}
}
