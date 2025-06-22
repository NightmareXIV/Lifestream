using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.GUI;
using static Lifestream.Paissa.PaissaData;

namespace Lifestream.Paissa;

public class PaissaImporter
{
    private static Guid CurrentDrag = Guid.Empty;
    private string ID;
    private string folderText = "No folder yet...";
    private bool buttonDisabled = false;
    private bool textToCopy = false;
    private DateTime disableEndTime;
    private const int BUTTON_DISABLE_TIME = 5; // in seconds
    private PaissaStatus status = PaissaStatus.Idle;
    public static Dictionary<string, PaissaAddressBookFolder> Folders = [];
    private Task<PaissaResult>? _importTask = null;

    public PaissaImporter(string id = "##paissa")
    {
        ID = id;
    }

    public void Draw()
    {
        ImGui.PushID(ID);

        if(buttonDisabled && DateTime.Now >= disableEndTime)
        {
            buttonDisabled = false;
            status = PaissaStatus.Idle;
        }
        var isDisabled = buttonDisabled;
        if(isDisabled) ImGui.BeginDisabled();

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Download, "Import from PaissaDB", enabled: Player.Available))
        {
            PluginLog.Debug("PaissaDB import process initiated!");
            buttonDisabled = true;
            disableEndTime = DateTime.Now.AddSeconds(BUTTON_DISABLE_TIME);
            status = PaissaStatus.Progress;

            _importTask = PaissaUtils.ImportFromPaissaDBAsync();
        }

        if(isDisabled) ImGui.EndDisabled();

        if(_importTask != null && _importTask.IsCompleted)
        {
            try
            {
                var result = _importTask.Result;
                folderText = result.FolderText;
                status = result.Status;
            }
            catch(Exception ex)
            {
                PluginLog.Error($"PaissaDB import failed: {ex}");
                status = PaissaStatus.Error;
            }

            _importTask = null;
        }

        ImGui.SameLine();

        ImGui.TextColored(PaissaUtils.GetStatusColorFromStatus(status), PaissaUtils.GetStatusStringFromStatus(status));

        if(Player.Available && Folders.TryGetValue(Player.CurrentWorld, out var book))
        {
            DrawPaissaBook(book);
        }

        ImGui.PopID();
    }

    public static void DrawPaissaBook(PaissaAddressBookFolder book)
    {
        if(book.Entries.Count == 0)
        {
            ImGuiEx.Text("No houses are currently available for bidding!");
        }
        else if(ImGui.BeginTable($"##addressbook", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Size");
            ImGui.TableSetupColumn("Bids");
            ImGui.TableSetupColumn("Allowed Tenants");
            ImGui.TableSetupColumn("World");
            ImGui.TableSetupColumn("Ward");
            ImGui.TableSetupColumn("Plot");
            List<(Vector2 RowPos, Action AcceptDraw)> MoveCommands = [];
            ImGui.TableHeadersRow();

            List<PaissaAddressBookEntry> entryArray;
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

                // Size

                ImGuiEx.Text($"{PaissaUtils.GetSizeString(entry.Size)}");
                ImGui.SameLine();
                ImGuiEx.Tooltip("Size");

                ImGui.TableNextColumn();

                // Bids

                ImGuiEx.Text($"{entry.Bids}");
                ImGui.SameLine();
                ImGuiEx.Tooltip("Bids");

                ImGui.TableNextColumn();

                // Allowed Tenants

                ImGuiEx.Text($"{PaissaUtils.GetAllowedTenantsStringFromPurchaseSystem(entry.AllowedTenants)}");
                ImGui.SameLine();
                ImGuiEx.Tooltip("Allowed Tenants");

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
                    ImGuiEx.Tooltip($"{TabAddressBook.ResidentialNames.SafeSelect(entry.City)}");
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
        }
    }
}
