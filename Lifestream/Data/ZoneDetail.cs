using Lifestream.Systems.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
public unsafe class ZoneDetail
{
    public List<CustomAetheryte> Aetherytes = [];
    public float MaxInteractionDistance = 4.6f;
    public List<string> GenericAetheryteNames = [];

    public ZoneDetail(List<CustomAetheryte> aetherytes)
    {
        Aetherytes = aetherytes;
    }

    public ZoneDetail(List<CustomAetheryte> aetherytes, float maxInteractionDistance)
    {
        Aetherytes = aetherytes;
        MaxInteractionDistance = maxInteractionDistance;
    }

    public ZoneDetail(List<CustomAetheryte> aetherytes, List<string> genericAetheryteNames) : this(aetherytes)
    {
        GenericAetheryteNames = genericAetheryteNames;
    }

    public ZoneDetail(List<CustomAetheryte> aetherytes, float maxInteractionDistance, List<string> genericAetheryteNames)
    {
        Aetherytes = aetherytes;
        MaxInteractionDistance = maxInteractionDistance;
        GenericAetheryteNames = genericAetheryteNames;
    }
}
