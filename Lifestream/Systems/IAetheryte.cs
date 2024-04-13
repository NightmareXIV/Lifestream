using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Systems;
public interface IAetheryte
{
    public Vector2 Position { get; set; }
    public uint TerritoryType { get; set; }
    public uint ID { get; set; }
    public string Name { get; set; }
}
