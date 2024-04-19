using ECommons.GameHelpers;
using NightmareUI.OtterGuiWrapper.FileSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
[Serializable]
public class AddressBookFolder : IFileSystemStorage
{
    public Guid GUID { get; set; } = Guid.NewGuid();
    public List<AddressBookEntry> Entries = [];
    public bool IsDefault = false;
    public string GetCustomName() => null;
    public void SetCustomName(string s) {  }
}
