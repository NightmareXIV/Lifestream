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
        private short* HoveredCoordsPtr => (short*)((nint)(&addon) + 1968);
        private float HoveredX => (float)addon.HoveredCoordsPtr[0] + (float)addon.HoveredCoordsPtr[1] / 10f;
        private float HoveredY => (float)addon.HoveredCoordsPtr[2] + (float)addon.HoveredCoordsPtr[3] / 10f;
        public Vector2 HoveredCoords => new(addon.HoveredX, addon.HoveredY);
    }
}
