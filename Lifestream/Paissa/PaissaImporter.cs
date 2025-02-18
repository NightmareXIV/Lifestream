using ECommons.Configuration;
using ECommons.GameHelpers;
using JetBrains.Annotations;
using Lifestream.Data;
using Lifestream.GUI;
using OtterGui;
using static Lifestream.Paissa.PaissaData;

namespace Lifestream.Paissa;

public class PaissaImporter
{
    private string ID;
    private string folderText = "No folder yet...";
    private bool buttonDisabled = false;
    private bool textToCopy = false;
    private DateTime disableEndTime;
    private const int BUTTON_DISABLE_TIME = 5; // in seconds
    private PaissaStatus status = PaissaStatus.Idle;
    public static Dictionary<string, AddressBookFolder> Folders = [];

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

        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Download, "Import from PaissaDB", enabled:Player.Available))
        {
            PluginLog.Debug("PaissaDB import process initiated!");
            buttonDisabled = true;
            disableEndTime = DateTime.Now.AddSeconds(BUTTON_DISABLE_TIME);
            status = PaissaStatus.Progress;

            _ = ImportFromPaissaDBAsync();
        }

        if(isDisabled) ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.TextColored(PaissaUtils.GetStatusColorFromStatus(status), PaissaUtils.GetStatusStringFromStatus(status));

        if(Player.Available && Folders.TryGetValue(Player.CurrentWorld, out var book))
        {
            TabAddressBook.DrawBook(book, true);
        }

        ImGui.PopID();
    }

    private async Task ImportFromPaissaDBAsync()
    {
        try
        {
            var responseData = await PaissaUtils.GetListingsForHomeWorldAsync((int)Player.HomeWorldId);

            if(responseData.StartsWith("Error") || responseData.StartsWith("Exception"))
            {
                PluginLog.Error($"Error retrieving data: {responseData}");
                folderText = "Error: Unable to retrieve listings. See log for details.";
                status = PaissaStatus.Error;
                return;
            }

            var responseObject = EzConfig.DefaultSerializationFactory.Deserialize<PaissaResponse>(responseData);
            if(responseObject == null)
            {
                PluginLog.Error("Failed to deserialize PaissaResponse.");
                folderText = "Error: Invalid response format.";
                status = PaissaStatus.Error;
                return;
            }

            var newFolder = PaissaUtils.GetAddressBookFolderFromPaissaResponse(responseObject);

            new TickScheduler(() => 
            {
                if(Player.Available)
                {
                    PaissaImporter.Folders[Player.CurrentWorld] = newFolder;
                }
            });
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Exception in import task: {ex.Message}");
            folderText = $"Error: {ex.Message}";
            status = PaissaStatus.Error;
        }
    }
}
