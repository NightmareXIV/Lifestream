using ECommons.SplatoonAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.IPC;
public class SplatoonCache
{
    public List<Element> WaymarkLineCache = [];
    public int WaymarkLinePos = 0;
    public List<Element> WaymarkPointCache = [];
    public int WaymarkPointPos = 0;
}
