using ECommons.Configuration;
using ECommons.GameHelpers;
using Lifestream.Data;

namespace Lifestream.Paissa
{
    public class PaissaImporter
    {
        private static PaissaImporter? _instance;
        public static PaissaImporter Instance {
            get
            {
                _instance ??= new();
                return _instance;
            }
        }

        private string ID;
        private string folderText = "No folder yet...";
        private bool buttonDisabled = false;
        private DateTime disableEndTime;
        private PaissaStatus status = PaissaStatus.Idle;

        public PaissaImporter(string id = "##paissa")
        {
            ID = id;
        }

        public void Draw()
        {
            ImGui.PushID(ID);

            if (buttonDisabled && DateTime.Now >= disableEndTime)
            {
                buttonDisabled = false;
                status = PaissaStatus.Idle;
            }
            bool isDisabled = buttonDisabled;
            if (isDisabled) ImGui.BeginDisabled();

            if (ImGui.Button("Import from PaissaDB"))
            {
                PluginLog.Debug("PaissaDB import process initiated!");
                buttonDisabled = true;
                disableEndTime = DateTime.Now.AddSeconds(5);
                status = PaissaStatus.Progress;

                _ = ImportFromPaissaDBAsync();
            }

            if (isDisabled) ImGui.EndDisabled();

            ImGui.SameLine();

            ImGui.TextColored(PaissaUtils.GetStatusColorFromStatus(status), PaissaUtils.GetStatusStringFromStatus(status));

            // Display folder object in text field to copy and import for testing
            byte[] textBuffer = new byte[2048];
            Encoding.UTF8.GetBytes(folderText, 0, folderText.Length, textBuffer, 0);
            ImGui.InputText("", textBuffer, (uint)textBuffer.Length);

            ImGui.PopID();
        }

        private async Task ImportFromPaissaDBAsync()
        {
            try
            {
                var responseData = await PaissaUtils.GetListingsForHomeWorldAsync((int)Player.HomeWorldId);

                if (responseData.StartsWith("Error") || responseData.StartsWith("Exception"))
                {
                    PluginLog.Error($"Error retrieving data: {responseData}");
                    folderText = "Error: Unable to retrieve listings. See log for details.";
                    status = PaissaStatus.Error;
                    return;
                }

                PaissaResponse responseObject = EzConfig.DefaultSerializationFactory.Deserialize<PaissaResponse>(responseData);
                if (responseObject == null)
                {
                    PluginLog.Error("Failed to deserialize PaissaResponse.");
                    folderText = "Error: Invalid response format.";
                    status = PaissaStatus.Error;
                    return;
                }

                AddressBookFolder newFolder = PaissaUtils.GetAddressBookFolderFromPaissaResponse(responseObject);

                /*
                
                 ADD NEW FOLDER TO LIST HERE

                */

                folderText = EzConfig.DefaultSerializationFactory.Serialize(newFolder, false);
                PluginLog.Debug("Folder serialized successfully.");
                status = PaissaStatus.Success;
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Exception in import task: {ex.Message}");
                folderText = $"Error: {ex.Message}";
                status = PaissaStatus.Error;
            }
        }
    }
}
