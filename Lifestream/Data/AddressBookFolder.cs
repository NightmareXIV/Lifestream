using NightmareUI.OtterGuiWrapper.FileSystems.Generic;

namespace Lifestream.Data;
[Serializable]
public class AddressBookFolder : IFileSystemStorage
{
    internal bool IsCopy = false;
    public string ExportedName = "";
    public Guid GUID { get; set; } = Guid.NewGuid();
    public List<AddressBookEntry> Entries = [];
    public bool IsDefault = false;
    public SortMode SortMode = SortMode.Manual;

    public bool ShouldSerializeGUID() => !IsCopy;
    public bool ShouldSerializeIsDefault() => !IsCopy;
    public bool ShouldSerializeExportedName() => IsCopy;

    public string GetCustomName() => null;
    public void SetCustomName(string s) { }
}
