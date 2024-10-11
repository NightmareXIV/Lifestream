using ECommons.Configuration;
using Lifestream.Data;
using NightmareUI.OtterGuiWrapper.FileSystems.Generic;

namespace Lifestream.Services;
public class CustomAliasFileSystemManager
{
    public GenericFileSystem<CustomAlias> FileSystem;
    private CustomAliasFileSystemManager()
    {
        FileSystem = new(P.Config.CustomAliases, "CustomAlias");
        FileSystem.Selector.OnBeforeItemCreation += Selector_OnBeforeItemCreation;
        FileSystem.Selector.OnBeforeCopy += Selector_OnBeforeCopy;
        FileSystem.Selector.OnImportPopupOpen += Selector_OnImportPopupOpen;
    }
    private void Selector_OnBeforeCopy(CustomAlias original, ref CustomAlias copy)
    {
        if(FileSystem.FindLeaf(original, out var leaf))
        {
            copy.ExportedName = leaf.Name;
        }
    }

    private void Selector_OnBeforeItemCreation(ref CustomAlias item)
    {
        item.GUID = Guid.NewGuid();
    }

    private void Selector_OnImportPopupOpen(string clipboardText, ref string newName)
    {
        try
        {
            var item = EzConfig.DefaultSerializationFactory.Deserialize<CustomAlias>(clipboardText);
            if(item != null && item.ExportedName != null)
            {
                newName = item.ExportedName;
            }
        }
        catch(Exception e) { }
    }
}
