using NightmareUI.OtterGuiWrapper.FileSystems.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
[Serializable]
public class CustomAlias : IFileSystemStorage
{
    public string ExportedName;
    public Guid GUID { get; set; } = Guid.NewGuid();
    public string Alias = "";
    public List<CustomAliasCommand> Commands = [];

    public string GetCustomName() => null;
    public void SetCustomName(string s) { }

    public void Enqueue()
    {
        if(!Utils.IsBusy())
        {
            foreach(var x in this.Commands)
            {
                x.Enqueue();
            }
        }
        else
        {
            Notify.Error("Lifestream is busy!");
        }
    }
}
