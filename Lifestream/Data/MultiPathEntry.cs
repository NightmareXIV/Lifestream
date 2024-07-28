using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
[Serializable]
public class MultiPathEntry
{
    internal Guid GUID = Guid.NewGuid();
    public uint Territory;
    public bool Sprint = false;
    public List<Vector3> Points = [];
}
