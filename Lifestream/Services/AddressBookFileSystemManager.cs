using ECommons.Configuration;
using Lifestream.Data;
using NightmareUI.OtterGuiWrapper.FileSystems.Generic;

namespace Lifestream.Services;
public class AddressBookFileSystemManager
{
    public GenericFileSystem<AddressBookFolder> FileSystem;
    private AddressBookFileSystemManager()
    {
        FileSystem = new(C.AddressBookFolders, "AddressBook");
        FileSystem.Selector.OnAfterDrawLeafName += Selector_OnAfterDrawLeafName;
        FileSystem.Selector.OnBeforeItemCreation += Selector_OnBeforeItemCreation;
        FileSystem.Selector.OnBeforeCopy += Selector_OnBeforeCopy;
        FileSystem.Selector.OnImportPopupOpen += Selector_OnImportPopupOpen;
    }
    private void Selector_OnBeforeCopy(AddressBookFolder original, ref AddressBookFolder copy)
    {
        copy.IsCopy = true;
        if(FileSystem.FindLeaf(original, out var leaf))
        {
            copy.ExportedName = leaf.Name;
        }
    }

    private void Selector_OnBeforeItemCreation(ref AddressBookFolder item)
    {
        if(item.Entries == null)
        {
            item = null;
            Notify.Error($"Item contains invalid data");
        }
        item.IsDefault = false;
        item.GUID = Guid.NewGuid();
    }

    private void Selector_OnImportPopupOpen(string clipboardText, ref string newName)
    {
        try
        {
            var item = EzConfig.DefaultSerializationFactory.Deserialize<AddressBookFolder>(clipboardText);
            if(item != null && item.ExportedName != null && !item.ExportedName.EqualsIgnoreCase("Default Book"))
            {
                newName = item.ExportedName;
            }
        }
        catch(Exception e) { }
    }

    private void Selector_OnAfterDrawLeafName(AddressBookFS.Leaf leaf, GenericFileSystem<AddressBookFolder>.FileSystemSelector.State arg2, bool arg3)
    {
        if(ImGui.BeginDragDropTarget())
        {
            if(ImGuiDragDrop.AcceptDragDropPayload("MoveRule", out Guid payload))
            {
                AddressBookEntry entry = null;
                AddressBookFolder folder = null;
                foreach(var f in C.AddressBookFolders)
                {
                    foreach(var e in f.Entries)
                    {
                        if(e.GUID == payload)
                        {
                            entry = e;
                            folder = f;
                            break;
                        }
                    }
                }
                if(entry == null)
                {
                    Notify.Error("Could not move");
                }
                else if(folder == leaf.Value)
                {
                    Notify.Error($"Could not move to the same folder");
                }
                else
                {
                    folder.Entries.Remove(entry);
                    leaf.Value.Entries.Add(entry);
                }
            }
            ImGui.EndDragDropTarget();
        }
    }
}
