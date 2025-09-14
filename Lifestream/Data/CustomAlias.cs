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

    public void Enqueue(bool force = false, int? inclusiveStart = null, int? exclusiveEnd = null)
    {
        foreach(var x in Commands)
        {
            if(!x.CanExecute(out var e))
            {
                DuoLog.Error($"{e}");
                return;
            }
        }
        if(force || !Utils.IsBusy())
        {
            var cmds = Commands;
            if(inclusiveStart.HasValue && inclusiveStart.Value > 0 && inclusiveStart.Value < cmds.Count)
            {
                cmds = [.. cmds.Skip(inclusiveStart.Value)];
            }

            if(exclusiveEnd.HasValue && exclusiveEnd.Value > 0 && exclusiveEnd.Value < cmds.Count)
            {
                cmds = [.. cmds.Take(exclusiveEnd.Value - (inclusiveStart ?? 0))];
            }
            for(var i = 0; i < cmds.Count; i++)
            {
                List<Vector3> append = [];
                var cmd = cmds[i];
                if(cmd.Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point, CustomAliasKind.Circular_movement) == true)
                {
                    while(cmds.SafeSelect(i + 1)?.Kind == CustomAliasKind.Move_to_point)
                    {
                        if(this.IsChainedWithNext(i + (inclusiveStart ?? 0)))
                        {
                            var c = cmds[i + 1];
                            append.Add(c.Point.Scatter(c.Scatter));
                            i++;
                            PluginLog.Information($"Appending command {i}");
                        }
                        else
                        {
                            break;
                        }
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
