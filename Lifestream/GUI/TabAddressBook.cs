using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
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
				InputWardDetailDialog.Draw();
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

		static AddressBookEntry GetNewAddressBookEntry()
		{
				var entry = new AddressBookEntry();
				var h = HousingManager.Instance();
				if(h != null)
				{
						if(Svc.ClientState.TerritoryType.EqualsAny(Houses.Ingleside_Apartment, Houses.Kobai_Goten_Apartment, Houses.Lily_Hills_Apartment, Houses.Sultanas_Breath_Apartment, Houses.Topmast_Apartment))
						{
								entry.PropertyType = PropertyType.Apartment;
								entry.ApartmentSubdivision = h->GetCurrentDivision() == 2;
						}
						entry.Ward = h->GetCurrentWard() + 1;
						entry.Plot = h->GetCurrentPlot() + 1;
						entry.Ward.ValidateRange(1, 30);
						entry.Plot.ValidateRange(1, 60);
						if (Player.Available)
						{
								entry.World = (int)Player.Object.CurrentWorld.Id;
						}
						var ra = Utils.GetResidentialAetheryteByTerritoryType(Svc.ClientState.TerritoryType);
						if (ra != null)
						{
								entry.City = ra.Value;
						}
				}
				return entry;
		}

		static void DrawBook(AddressBookFolder book)
    {
				if (ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add New Address"))
				{
						var h = HousingManager.Instance();
						var entry = GetNewAddressBookEntry();
						book.Entries.Add(entry);
						InputWardDetailDialog.Entry = entry;
				}
				if (ImGui.BeginTable($"##addressbook", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
				{
						ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn("World");
						ImGui.TableSetupColumn("Ward");
						ImGui.TableSetupColumn("Plot");
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
								if (ImGui.Button($"{entry.Name.NullWhenEmpty() ?? entry.GetAutoName()}###entry{i}", bsize))
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
								if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
								{
										ImGui.OpenPopup($"ABMenu {i}");
								}
								if (ImGui.BeginPopup($"ABMenu {i}"))
								{
										if (ImGui.MenuItem("Copy to Clipboard"))
										{

										}
										if (entry.Alias != "")
										{
												ImGui.MenuItem($"Enable Alias: {entry.Alias}", null, ref entry.AliasEnabled);
										}
										if (ImGui.MenuItem("Edit..."))
										{
												InputWardDetailDialog.Entry = entry;
										}
										if (ImGui.MenuItem("Delete"))
										{
												if (ImGuiEx.Ctrl)
												{
														var rem = i;
														new TickScheduler(() => book.Entries.RemoveAt(rem));
												}
												else
												{
														Svc.Toasts.ShowError($"Hold CTRL and click to delete an entry");
												}
										}
										ImGuiEx.Tooltip($"Hold CTRL and click to delete");
										ImGui.EndPopup();
								}

								ImGui.PopStyleVar();
								ImGui.PopStyleColor();

								ImGui.TableNextColumn();

								ImGuiEx.TextV(ImGuiColors.DalamudGrey2, ExcelWorldHelper.GetName(entry.World));

								ImGui.TableNextColumn();
								if(entry.City.RenderIcon())
								{
										ImGuiEx.Tooltip($"{ResidentialNames.SafeSelect(entry.City)}");
										ImGui.SameLine(0, 1);
								}
								

								ImGuiEx.Text($"{entry.Ward.FancyDigits()}");

								ImGui.TableNextColumn();

								if (entry.PropertyType == PropertyType.House)
								{
										ImGuiEx.Text(Colors.TabGreen, Lang.SymbolPlot);
										ImGuiEx.Tooltip("Plot");
										ImGui.SameLine(0, 0);
										ImGuiEx.Text($"{entry.Plot.FancyDigits()}");
								}
								if (entry.PropertyType == PropertyType.Apartment)
								{
										if (!entry.ApartmentSubdivision)
										{
												ImGuiEx.Text(Colors.TabYellow, Lang.SymbolApartment);
												ImGuiEx.Tooltip("Apartment");
										}
										else
										{
												ImGuiEx.Text(Colors.TabYellow, Lang.SymbolSubdivision);
												ImGuiEx.Tooltip("Subdivision Apartment");
										}
										ImGui.SameLine(0, 0);
										ImGuiEx.Text($"{entry.Apartment.FancyDigits()}");
								}

								ImGui.PopID();
						}

						ImGui.EndTable();
				}
		}
}
