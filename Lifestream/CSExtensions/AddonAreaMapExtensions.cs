using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.CSExtensions;
public static unsafe class AddonAreaMapExtensions
{
    extension(AddonAreaMap addon)
    {
        private short HoveredCoordsX => *(short*)((nint)(&addon) + 1968);
        private short HoveredCoordsXFraction => *(short*)((nint)(&addon) + 1970);
        private short HoveredCoordsY => *(short*)((nint)(&addon) + 1972);
        private short HoveredCoordsYFraction => *(short*)((nint)(&addon) + 1974);
        private float HoveredX => (float)addon.HoveredCoordsX + (float)addon.HoveredCoordsXFraction / 10f;
        private float HoveredY => (float)addon.HoveredCoordsY + (float)addon.HoveredCoordsYFraction / 10f;
        public Vector2 HoveredCoords => new(addon.HoveredX, addon.HoveredY);
    }
}
