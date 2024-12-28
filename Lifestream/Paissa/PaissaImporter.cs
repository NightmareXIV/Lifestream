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

        public PaissaImporter(string id = "##paissa")
        {
            ID = id;
        }

        public void Draw()
        {
            ImGui.PushID(ID);

            if (ImGui.Button("Import from PaissaDB"))
            {
                PluginLog.Debug("PaissaDB import process initiated!");

                Task.Run(async () =>
                {
                    try
                    {
                        var responseData = await PaissaUtils.GetListingsForHomeWorldAsync((int)Player.HomeWorldId);

                        if (responseData.StartsWith("Error") || responseData.StartsWith("Exception"))
                        {
                            PluginLog.Error($"Error retrieving data: {responseData}");
                            folderText = "Error: Unable to retrieve listings. See log for details.";
                            return;
                        }

                        PaissaResponse responseObject = EzConfig.DefaultSerializationFactory.Deserialize<PaissaResponse>(responseData);
                        if (responseObject == null)
                        {
                            PluginLog.Error("Failed to deserialize PaissaResponse.");
                            folderText = "Error: Invalid response format.";
                            return;
                        }

                        AddressBookFolder newFolder = PaissaUtils.GetAddressBookFolderFromPaissaResponse(responseObject);

                        /*
                         
                         ADD NEW FOLDER TO LIST HERE
                         
                         */

                        folderText = EzConfig.DefaultSerializationFactory.Serialize(newFolder, false);
                        PluginLog.Debug("Folder serialized successfully.");
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error($"Exception in import task: {ex.Message}");
                        folderText = $"Error: {ex.Message}";
                    }
                });
            }

            // Display folder object in text field to copy and import for testing
            byte[] textBuffer = new byte[2048];
            Encoding.UTF8.GetBytes(folderText, 0, folderText.Length, textBuffer, 0);
            ImGui.InputText("", textBuffer, (uint)textBuffer.Length);

            ImGui.PopID();
        }
    }
}
