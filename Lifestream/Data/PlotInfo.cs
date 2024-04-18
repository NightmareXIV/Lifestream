using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
[Serializable]
public class PlotInfo
{
    public uint AethernetID;
    public Vector3 Front;
    public List<Vector3> Path = [];
}
