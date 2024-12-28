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
                PluginLog.Debug("Retrieving house listings from PaissaDB...");

                Task.Run(async () => {
                    var responseData = await PaissaUtils.GetListingsAsync((int)Player.HomeWorldId);
                    PaissaResponse responseObject = EzConfig.DefaultSerializationFactory.Deserialize<PaissaResponse>(responseData);
                    AddressBookFolder newFolder = PaissaUtils.GetAddressBookFolderFromPaissaResponse(responseObject);

                    /* 

                    ADD NEW FOLDER TO LIST HERE

                    */

                    folderText = EzConfig.DefaultSerializationFactory.Serialize(newFolder, false);
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
