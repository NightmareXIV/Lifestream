using Lifestream.Tasks.Shortcuts;

namespace Lifestream.Data;
public class AutoPropertyData
{
    public bool Enabled = true;
    public TaskPropertyShortcut.PropertyType Type;

    public AutoPropertyData() { }

    public AutoPropertyData(bool enabled, TaskPropertyShortcut.PropertyType type)
    {
        Enabled = enabled;
        Type = type;
    }
}
