using ECommons.Configuration;
using ECommons.GameHelpers;
using JetBrains.Annotations;
using Lifestream.Data;
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

        if(ImGui.Button("Import from PaissaDB"))
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

        // Display folder object in text field to copy and import for testing
        if(textToCopy == false) ImGui.BeginDisabled();
        var textBuffer = new byte[2048];
        Encoding.UTF8.GetBytes(folderText, 0, folderText.Length, textBuffer, 0);
        ImGui.InputText("", textBuffer, (uint)textBuffer.Length);
        if(textToCopy == false) ImGui.EndDisabled();

        ImGui.SameLine();

        if(ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Copy.ToIconString(), new Vector2(30, 25), "Copy to clipboard.", !textToCopy, true))
        {
            try
            {
                Copy(folderText);
                PluginLog.Debug("Text copied to clipboard: " + folderText);
            }
            catch(Exception ex)
            {
                PluginLog.Error($"Failed to copy text to clipboard: {ex.Message}");
            }
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

            /*
            
             ADD NEW FOLDER TO LIST HERE

            */

            folderText = EzConfig.DefaultSerializationFactory.Serialize(newFolder, false);
            PluginLog.Debug("Folder serialized successfully.");
            status = PaissaStatus.Success;
            textToCopy = true;
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Exception in import task: {ex.Message}");
            folderText = $"Error: {ex.Message}";
            status = PaissaStatus.Error;
        }
    }
}
