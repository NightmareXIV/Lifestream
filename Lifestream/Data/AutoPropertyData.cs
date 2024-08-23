using Lifestream.Tasks.Shortcuts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
