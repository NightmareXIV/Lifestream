using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lifestream.Data;
using Lifestream.Enums;
using SortMode = Lifestream.Data.SortMode;

namespace Lifestream.GUI;
public static unsafe class TabAddressBook
{
    public static readonly Dictionary<ResidentialAetheryteKind, string> ResidentialNames = new()
    {
        [ResidentialAetheryteKind.Gridania] = "Lavender Beds",
        [ResidentialAetheryteKind.Limsa] = "Mist",
        [ResidentialAetheryteKind.Uldah] = "Goblet",
        [ResidentialAetheryteKind.Kugane] = "Shirogane",
        [ResidentialAetheryteKind.Foundation] = "Empyreum",
    };

    public static readonly Dictionary<SortMode, string> SortModeNames = new()
    {
        [SortMode.Manual] = "Manual (drag and drop)",
        [SortMode.Name] = "Name (A-Z)",
        [SortMode.NameReversed] = "Name (Z-A)",
        [SortMode.World] = "World (A-Z)",
        [SortMode.WorldReversed] = "World (Z-A)",
        [SortMode.Plot] = "Plot (1-9)",
        [SortMode.PlotReversed] = "Plot (9-1)",
        [SortMode.Ward] = "Ward (1-9)",
        [SortMode.WardReversed] = "Ward (9-1)",
    };
    private static Guid CurrentDrag = Guid.Empty;

    public static void Draw()
    {
        InputWardDetailDialog.Draw();
        var selector = S.AddressBookFileSystemManager.FileSystem.Selector;
        selector.Draw(150f.Scale());
        ImGui.SameLine();
        if(C.AddressBookFolders.Count == 0)
        {
            var book = new AddressBookFolder() { IsDefault = true };
            S.AddressBookFileSystemManager.FileSystem.Create(book, "Default Book", out _);
        }
        if(ImGui.BeginChild("Child"))
        {
            if(selector.Selected != null)
            {
                var book = selector.Selected;
                DrawBook(book);
            }
            else
            {
                if(C.AddressBookFolders.TryGetFirst(x => x.IsDefault, out var value))
                {
                    selector.SelectByValue(value);
                }
                ImGuiEx.TextWrapped($"To begin, select an address book to use.");
            }
        }
        ImGui.EndChild();
    }

    private static AddressBookEntry GetNewAddressBookEntry()
    {
        var entry = new AddressBookEntry();
        var h = HousingManager.Instance();
        if(h != null)
        {
            entry.Ward = h->GetCurrentWard() + 1;
            if(P.Territory.EqualsAny(Houses.Ingleside_Apartment, Houses.Kobai_Goten_Apartment, Houses.Lily_Hills_Apartment, Houses.Sultanas_Breath_Apartment, Houses.Topmast_Apartment))
            {
                entry.PropertyType = PropertyType.Apartment;
                entry.ApartmentSubdivision = h->GetCurrentDivision() == 2;
                entry.Apartment = h->GetCurrentRoom();
                entry.Apartment.ValidateRange(1, 9999);
            }
            else
            {
                entry.Plot = h->GetCurrentPlot() + 1;
                entry.Ward.ValidateRange(1, 30);
                entry.Plot.ValidateRange(1, 60);
            }
            if(Player.Available)
            {
                entry.World = (int)Player.Object.CurrentWorld.RowId;
            }
            var ra = Utils.GetResidentialAetheryteByTerritoryType(P.Territory);
            if(ra != null)
            {
                entry.City = ra.Value;
            }
        }
        return entry;
    }

    public static void DrawBook(AddressBookFolder book, bool readOnly = false)
    {
        if(!readOnly)
        {
            ImGuiEx.LineCentered(() =>
            {
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "Add New"))
                {
                    var h = HousingManager.Instance();
                    var entry = GetNewAddressBookEntry();
                    book.Entries.Add(entry);
                    InputWardDetailDialog.Entry = entry;
                }
                ImGui.SameLine();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "Paste"))
                {
                    try
                    {
                        var entry = EzConfig.DefaultSerializationFactory.Deserialize<AddressBookEntry>(Paste());
                        if(entry != null)
                        {
                            if(!entry.IsValid(out var error))
                            {
                                Notify.Error($"Could not paste from clipboard:\n{error}");
                            }
                            else
                            {
                                book.Entries.Add(entry);
                            }
                        }
                        else
                        {
                            Notify.Error($"Could not paste from clipboard");
                        }
                    }
                    catch(Exception e)
                    {
                        if(Utils.TryParseAddressBookEntry(Paste(), out var entry))
                        {
                            book.Entries.Add(entry);
                        }
                        else
                        {
                            Notify.Error($"Could not paste from clipboard:\n{e.Message}");
                        }
                    }
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(100f.Scale());
                ImGuiEx.EnumCombo("##sort", ref book.SortMode, SortModeNames);
                ImGuiEx.Tooltip($"Select sort mode for this address book");
                ImGui.SameLine();
                if(ImGui.Checkbox($"Default", ref book.IsDefault))
                {
                    if(book.IsDefault)
                    {
                        C.AddressBookFolders.Where(z => z != book).Each(z => z.IsDefault = false);
                    }
                }
                ImGuiEx.Tooltip($"Default book automatically opens when you open plugin first time in a game session.");
            });
        }

        if(ImGui.BeginTable($"##addressbook", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("World");
            ImGui.TableSetupColumn("Ward");
            ImGui.TableSetupColumn("Plot");
            List<(Vector2 RowPos, Action AcceptDraw)> MoveCommands = [];
            ImGui.TableHeadersRow();

            List<AddressBookEntry> entryArray;
            if(book.SortMode.EqualsAny(SortMode.Name, SortMode.NameReversed)) entryArray = [.. book.Entries.OrderBy(x => x.Name.NullWhenEmpty() ?? x.GetAutoName()).ThenBy(x => x.Ward).ThenBy(x => x.Plot)];
            else if(book.SortMode.EqualsAny(SortMode.World, SortMode.WorldReversed)) entryArray = [.. book.Entries.OrderBy(x => ExcelWorldHelper.GetName(x.World)).ThenBy(x => x.Name.NullWhenEmpty() ?? x.GetAutoName())];
            else if(book.SortMode.EqualsAny(SortMode.Ward, SortMode.WardReversed)) entryArray = [.. book.Entries.OrderBy(x => x.Ward).ThenBy(x => x.Plot).ThenBy(x => x.Name.NullWhenEmpty() ?? x.GetAutoName())];
            else if(book.SortMode.EqualsAny(SortMode.Plot, SortMode.PlotReversed)) entryArray = [.. book.Entries.OrderBy(x => x.Plot).ThenBy(x => x.Ward).ThenBy(x => x.Name.NullWhenEmpty() ?? x.GetAutoName())];
            else entryArray = [.. book.Entries];
            if(book.SortMode.EqualsAny(SortMode.PlotReversed, SortMode.NameReversed, SortMode.WardReversed, SortMode.WorldReversed))
            {
                entryArray.Reverse();
            }

            for(var i = 0; i < entryArray.Count; i++)
            {
                var entry = entryArray[i];
                ImGui.PushID($"House{entry.GUID}");
                ImGui.TableNextRow();
                if(CurrentDrag == entry.GUID)
                {
                    var color = GradientColor.Get(EColor.Green, EColor.Green with { W = EColor.Green.W / 4 }, 500).ToUint();
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, color);
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, color);
                }
                ImGui.TableNextColumn();
                var rowPos = ImGui.GetCursorPos();
                var bsize = ImGuiHelpers.GetButtonSize("A") with { X = ImGui.GetContentRegionAvail().X };
                ImGui.PushStyleColor(ImGuiCol.Button, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, Vector2.Zero);
                var col = entry.IsHere();
                if(col) ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
                if(ImGui.Button($"{entry.Name.NullWhenEmpty() ?? entry.GetAutoName()}###entry", bsize))
                {
                    if(Player.Interactable && !P.TaskManager.IsBusy)
                    {
                        entry.GoTo();
                    }
                }
                if(col) ImGui.PopStyleColor();
                if(ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"ABMenu {entry.GUID}");
                }
                if(ImGui.BeginPopup($"ABMenu {entry.GUID}"))
                {
                    if(ImGui.MenuItem("Copy chat-friendly name to clipboard"))
                    {
                        Copy(entry.GetAddressString());
                    }
                    ImGui.Separator();
                    if(ImGui.MenuItem("Export to Clipboard"))
                    {
                        Copy(EzConfig.DefaultSerializationFactory.Serialize(entry, false));
                    }
                    if(entry.Alias != "")
                    {
                        ImGui.MenuItem($"Enable Alias: {entry.Alias}", null, ref entry.AliasEnabled);
                    }
                    if(ImGui.MenuItem("Edit..."))
                    {
                        InputWardDetailDialog.Entry = entry;
                    }
                    if(ImGui.MenuItem("Delete"))
                    {
                        if(ImGuiEx.Ctrl)
                        {
                            new TickScheduler(() => book.Entries.Remove(entry));
                        }
                        else
                        {
                            Svc.Toasts.ShowError($"Hold CTRL and click to delete an entry");
                        }
                    }
                    ImGuiEx.Tooltip($"Hold CTRL and click to delete");
                    ImGui.EndPopup();
                }
                if(ImGui.BeginDragDropSource())
                {
                    ImGuiDragDrop.SetDragDropPayload("MoveRule", entry.GUID);
                    CurrentDrag = entry.GUID;
                    InternalLog.Verbose($"DragDropSource = {entry.GUID}");
                    if(book.SortMode == SortMode.Manual)
                    {
                        ImGui.SetTooltip("Reorder or move to other folder");
                    }
                    else
                    {
                        ImGui.SetTooltip("Move to other folder");
                    }
                    ImGui.EndDragDropSource();
                }
                else if(CurrentDrag == entry.GUID)
                {
                    InternalLog.Verbose($"Current drag reset!");
                    CurrentDrag = Guid.Empty;
                }

                if(entry.IsQuickTravelAvailable())
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    var size = ImGui.CalcTextSize(FontAwesomeIcon.BoltLightning.ToIconString());
                    ImGui.SameLine(0, 0);
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - size.X - ImGui.GetStyle().FramePadding.X);
                    ImGuiEx.Text(ImGuiColors.DalamudYellow, FontAwesomeIcon.BoltLightning.ToIconString());
                    ImGui.PopFont();
                }
                else if(entry.AliasEnabled)
                {
                    var size = ImGui.CalcTextSize(entry.Alias);
                    ImGui.SameLine(0, 0);
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() - size.X - ImGui.GetStyle().FramePadding.X);
                    ImGuiEx.Text(ImGuiColors.DalamudGrey3, entry.Alias);
                }

                var moveItemIndex = i;
                MoveCommands.Add((rowPos, () =>
                {
                    if(book.SortMode == SortMode.Manual)
                    {
                        if(ImGui.BeginDragDropTarget())
                        {
                            if(ImGuiDragDrop.AcceptDragDropPayload("MoveRule", out Guid payload, ImGuiDragDropFlags.AcceptBeforeDelivery | ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
                            {
                                MoveItemToPosition(book.Entries, (x) => x.GUID == payload, moveItemIndex);
                            }
                            ImGui.EndDragDropTarget();
                        }
                    }
                }
                ));

                ImGui.PopStyleVar();
                ImGui.PopStyleColor();

                ImGui.TableNextColumn();

                var wcol = ImGuiColors.DalamudGrey;
                if(Player.Available && Player.Object.CurrentWorld.ValueNullable?.DataCenter.RowId == ExcelWorldHelper.Get((uint)entry.World)?.DataCenter.RowId)
                {
                    wcol = ImGuiColors.DalamudGrey;
                }
                else
                {
                    if(!S.Data.DataStore.DCWorlds.Contains(ExcelWorldHelper.GetName(entry.World))) wcol = ImGuiColors.DalamudGrey3;
                }
                if(Player.Available && Player.Object.CurrentWorld.RowId == entry.World) wcol = new Vector4(0.9f, 0.9f, 0.9f, 1f);

                ImGuiEx.TextV(wcol, ExcelWorldHelper.GetName(entry.World));

                ImGui.TableNextColumn();
                if(entry.City.RenderIcon())
                {
                    ImGuiEx.Tooltip($"{ResidentialNames.SafeSelect(entry.City)}");
                    ImGui.SameLine(0, 1);
                }


                ImGuiEx.Text($"{entry.Ward.FancyDigits()}");

                ImGui.TableNextColumn();

                if(entry.PropertyType == PropertyType.House)
                {
                    ImGuiEx.Text(Colors.TabGreen, Lang.SymbolPlot);
                    ImGuiEx.Tooltip("Plot");
                    ImGui.SameLine(0, 0);
                    ImGuiEx.Text($"{entry.Plot.FancyDigits()}");
                }
                if(entry.PropertyType == PropertyType.Apartment)
                {
                    if(!entry.ApartmentSubdivision)
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

            if(!readOnly)
            {
                foreach(var x in MoveCommands)
                {
                    ImGui.SetCursorPos(x.RowPos);
                    ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGuiHelpers.GetButtonSize(" ").Y));
                    x.AcceptDraw();
                }
            }
        }
    }
}
