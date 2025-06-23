using NightmareUI.OtterGuiWrapper.FileSystems.Generic;

namespace Lifestream.Data;
[Serializable]
public class CustomAlias : IFileSystemStorage
{
    public string ExportedName;
    public Guid GUID { get; set; } = Guid.NewGuid();
    public string Alias = "";
    public bool Enabled = true;
    public List<CustomAliasCommand> Commands = [];

    public bool ShouldSerializeAlias() => Alias.Length > 0;
    public bool ShouldSerializeEnabled() => Enabled != true;
    public bool ShouldSerializeGUID() => GUID != Guid.Empty;

    public string GetCustomName() => null;
    public void SetCustomName(string s) { }

    public void Enqueue(bool force = false)
    {
        if(force || !Utils.IsBusy())
        {
            for(var i = 0; i < Commands.Count; i++)
            {
                List<Vector3> append = [];
                var cmd = Commands[i];
                if(cmd.Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point, CustomAliasKind.Circular_movement) == true)
                {
                    while(Commands.SafeSelect(i + 1)?.Kind == CustomAliasKind.Move_to_point)
                    {
                        var c = Commands[i + 1];
                        append.Add(c.Point.Scatter(c.Scatter));
                        i++;
                    }
                }
                cmd.Enqueue(append);
            }
        }
        else
        {
            Notify.Error("Lifestream is busy!");
        }
    }
}
